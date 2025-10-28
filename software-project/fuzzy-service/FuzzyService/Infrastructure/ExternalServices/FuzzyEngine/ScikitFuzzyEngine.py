from __future__ import annotations

import logging
import numpy as np
from typing import Dict, List, Any, Optional, Tuple
from datetime import datetime, timezone

try:
    import skfuzzy as fuzz
except ImportError:
    fuzz = None
    logging.warning("scikit-fuzzy no está instalado. Instalar con: pip install scikit-fuzzy")

from FuzzyService.Domain.Entities.fuzzy_variable import FuzzyVariable
from FuzzyService.Domain.Entities.fuzzy_term import FuzzyTerm
from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule
from FuzzyService.Domain.Entities.fuzzy_system import FuzzySystem
from FuzzyService.Domain.ValueObjects.MembershipFunction import MembershipFunction
from FuzzyService.Domain.Enums.MembershipFunctionType import MembershipFunctionType
from FuzzyService.Domain.Errors.DomainErrors import ValidationError, EntityNotFoundError
from FuzzyService.Domain.Interfaces.IFuzzyEngine import IFuzzyEngine
from FuzzyService.Infrastructure.ExternalServices.FuzzyEngine.FuzzyResultTypes import (
    FuzzificationResult,
    BatchRuleEvaluationResult
)
from FuzzyService.Infrastructure.ExternalServices.FuzzyEngine.RuleEvaluationEngine import RuleEvaluationEngine
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyTermId
from medyator import Medyator
from kink import inject, di


# FuzzificationResult ahora se importa desde FuzzyResultTypes


class ScikitFuzzyEngine(IFuzzyEngine):
    """Motor fuzzy basado en scikit-fuzzy para implementar la fuzzificación.
    
    Este motor se encarga específicamente del paso 3 del flujo:
    3.1 - Identificar variables de entrada vinculadas a sensor IDs
    3.2 - Cargar términos con sus funciones de membresía
    3.3 - Calcular grados de pertenencia usando scikit-fuzzy
    """
    
    @inject
    def __init__(self, mediator: Medyator):
        self.logger = logging.getLogger(f"{__name__}.{self.__class__.__name__}")
        if fuzz is None:
            raise ImportError("scikit-fuzzy es requerido para ScikitFuzzyEngine")
        
        # Inicializar motor de evaluación de reglas
        self.rule_evaluation_engine = RuleEvaluationEngine()
        
        # Mediador para acceder a repositorios
        self.mediator = mediator
    
    async def fuzzify_sensor_readings(
        self, 
        variables: List[FuzzyVariable], 
        terms: List[FuzzyTerm], 
        sensor_readings: Dict[str, float]
    ) -> List[FuzzificationResult]:
        """Realiza la fuzzificación de las lecturas de sensores.
        
        Args:
            variables: Lista de variables fuzzy del sistema
            terms: Lista de términos fuzzy asociados a las variables
            sensor_readings: Diccionario {sensor_id: valor_crisp}
            
        Returns:
            Lista de resultados de fuzzificación por variable
            
        Raises:
            ValidationError: Si hay problemas con los datos de entrada
        """
        start_time = datetime.now()
        results = []
        
        try:
            # Paso 3.1: Identificar variables de entrada vinculadas a sensor IDs
            input_variables = self._identify_input_variables(variables, sensor_readings)
            
            if not input_variables:
                self.logger.warning("No se encontraron variables de entrada vinculadas a los sensores")
                return results
            
            # Procesar cada variable de entrada
            for variable in input_variables:
                try:
                    # Obtener el valor crisp del sensor usando reference_code
                    sensor_value = sensor_readings[variable.reference_code]
                    
                    # Paso 3.2: Cargar términos con sus funciones de membresía
                    variable_terms = self._get_variable_terms(variable, terms)
                    
                    if not variable_terms:
                        self.logger.warning(f"No se encontraron términos para la variable {variable.name}")
                        continue
                    
                    # Paso 3.3: Calcular grados de pertenencia
                    fuzz_result = await self._calculate_membership_degrees(
                        variable, variable_terms, sensor_value
                    )
                    
                    if fuzz_result:
                        results.append(fuzz_result)
                        
                except Exception as e:
                    self.logger.error(f"Error procesando variable {variable.name}: {e}")
                    continue

            return results
            
        except Exception as e:
            self.logger.error(f"Error en fuzzificación: {e}")
            raise ValidationError(f"Error en proceso de fuzzificación: {str(e)}")
    
    def _identify_input_variables(
        self,
        variables: List[FuzzyVariable],
        sensor_readings: Dict[str, float]
    ) -> List[FuzzyVariable]:
        """Paso 3.1: Identifica variables de entrada vinculadas a sensor codes."""
        input_variables = []
        sensor_codes = set(sensor_readings.keys())

        for variable in variables:
            # Solo variables de entrada
            if variable.variable_type != "input":
                continue

            # Que tengan reference_code vinculado a un sensor
            if variable.reference_code and variable.reference_code in sensor_codes:
                input_variables.append(variable)

        return input_variables
    
    def _get_variable_terms(self, variable: FuzzyVariable, all_terms: List[FuzzyTerm]) -> List[FuzzyTerm]:
        """Paso 3.2: Obtiene los términos asociados a una variable."""
        variable_terms = [
            term for term in all_terms
            if term.variable_id == variable.id
        ]

        return variable_terms
    
    async def _calculate_membership_degrees(
        self, 
        variable: FuzzyVariable, 
        terms: List[FuzzyTerm], 
        crisp_value: float
    ) -> Optional[FuzzificationResult]:
        """Paso 3.3: Calcula grados de pertenencia usando scikit-fuzzy."""
        start_time = datetime.now()
        
        try:
            result = FuzzificationResult(
                variable_name=variable.name,
                sensor_id=variable.reference_code,  # Usar reference_code (código estable del sensor)
                crisp_value=crisp_value,
                variable_id=str(variable.id)
            )
            
            # Determinar el universo de discurso
            universe_min, universe_max = self._determine_universe(terms)
            
            # Crear el universo de discurso
            universe = np.linspace(universe_min, universe_max, 1000)
            
            # Calcular grado de pertenencia para cada término
            for term in terms:
                try:
                    membership_degree = self._calculate_term_membership(
                        term, crisp_value, universe
                    )

                    if membership_degree > 0.0:  # Solo términos activos
                        result.add_term_activation(term.label, membership_degree)

                except Exception as e:
                    self.logger.error(f"Error calculando membresía para término {term.label}: {e}")
                    continue

            processing_time = (datetime.now() - start_time).total_seconds() * 1000
            result.processing_time_ms = processing_time

            if result.activated_terms:
                return result
            else:
                self.logger.warning(
                    f"Ningún término activo para {variable.name} con valor {crisp_value}"
                )
                return None
                
        except Exception as e:
            self.logger.error(f"Error calculando grados de pertenencia: {e}")
            return None
    
    def _determine_universe(self, terms: List[FuzzyTerm]) -> Tuple[float, float]:
        """Determina el universo de discurso basado en los términos."""
        if not terms:
            return 0.0, 100.0
        
        # Usar el universo definido en las funciones de membresía
        min_vals = [term.membership_function.universe_min for term in terms]
        max_vals = [term.membership_function.universe_max for term in terms]
        
        universe_min = min(min_vals)
        universe_max = max(max_vals)
        
        return universe_min, universe_max
    
    def _calculate_term_membership(
        self, 
        term: FuzzyTerm, 
        crisp_value: float, 
        universe: np.ndarray
    ) -> float:
        """Calcula el grado de pertenencia de un valor crisp a un término."""
        mf = term.membership_function
        
        # Crear la función de membresía usando scikit-fuzzy
        membership_values = self._create_membership_function(mf, universe)
        
        # Interpolar para obtener el grado de pertenencia del valor crisp
        membership_degree = np.interp(crisp_value, universe, membership_values)
        
        return float(membership_degree)
    
    def _create_membership_function(
        self, 
        mf: MembershipFunction, 
        universe: np.ndarray
    ) -> np.ndarray:
        """Crea una función de membresía usando scikit-fuzzy."""
        params = mf.parameters
        
        # Debug logging para identificar el problema
        self.logger.debug(
            f"Procesando función de membresía: tipo={mf.function_type}, "
            f"tipo_python={type(mf.function_type)}, valor={mf.function_type.value if hasattr(mf.function_type, 'value') else 'N/A'}"
        )
        
        if mf.function_type == MembershipFunctionType.TRIANGULAR:
            if len(params) != 3:
                raise ValidationError(f"Función triangular requiere 3 parámetros, recibidos: {len(params)}")
            return fuzz.trimf(universe, params)
            
        elif mf.function_type == MembershipFunctionType.TRAPEZOIDAL:
            if len(params) != 4:
                raise ValidationError(f"Función trapezoidal requiere 4 parámetros, recibidos: {len(params)}")
            return fuzz.trapmf(universe, params)
            
        elif mf.function_type == MembershipFunctionType.GAUSSIAN:
            if len(params) != 2:
                raise ValidationError(f"Función gaussiana requiere 2 parámetros, recibidos: {len(params)}")
            mean, sigma = params
            return fuzz.gaussmf(universe, mean, sigma)
            
        else:
            # Para otros tipos, usar triangular como fallback
            self.logger.warning(
                f"Tipo de función {mf.function_type} (tipo: {type(mf.function_type)}) no soportado, usando triangular como fallback"
            )
            if len(params) >= 3:
                return fuzz.trimf(universe, params[:3])
            else:
                # Crear una función triangular básica
                mid = (mf.universe_min + mf.universe_max) / 2
                return fuzz.trimf(universe, [mf.universe_min, mid, mf.universe_max])
    
    async def evaluate_fuzzy_logic(self, request: Dict[str, Any]) -> Dict[str, Any]:
        """Método principal para evaluación fuzzy.
        
        Implementa fuzzificación (paso 3) y evaluación de reglas (paso 4).
        """
        start_time = datetime.now()
        
        # Extraer datos del request
        sensor_data = request.get("sensor_data", {})
        variables = request.get("variables", [])
        terms = request.get("terms", [])
        
        # Paso 3: Fuzzificación
        fuzzification_results = await self.fuzzify_sensor_readings(
            variables=variables,
            terms=terms,
            sensor_readings=sensor_data
        )
        
        # Paso 4: Evaluación de reglas
        try:
            # Extraer sistema fuzzy y reglas del request
            system = request.get("fuzzy_system")
            rules = request.get("rules", [])
            
            if not system:
                raise ValidationError("Sistema fuzzy no proporcionado en el request")
            
            if not rules:
                self.logger.warning("No se proporcionaron reglas para evaluar")
                rules = []
            
            # Evaluar reglas usando el motor de evaluación (síncrono)
            rule_evaluation_result = self.rule_evaluation_engine.evaluate_rules(
                system=system,
                rules=rules,
                fuzzification_results=fuzzification_results
            )
            
            # Construir respuesta con resultados de fuzzificación y evaluación de reglas
            response = {
                "request_id": request.get("request_id", "unknown"),
                "status": "rules_evaluated",
                "message": f"Fuzzificación y evaluación de reglas completadas. {rule_evaluation_result.rules_activated}/{rule_evaluation_result.rules_evaluated} reglas activadas",
                "sensor_data_received": sensor_data,
                "fuzzification_results": self._serialize_fuzzification_results(fuzzification_results),
                "rule_evaluation": self._serialize_rule_evaluation_result(rule_evaluation_result, None, rules, variables),
                "processing_time_ms": (datetime.now() - start_time).total_seconds() * 1000
            }

            return response
            
        except Exception as e:
            self.logger.error(f"Error en evaluación de reglas: {e}")
            # Retornar solo resultados de fuzzificación si falla la evaluación de reglas
            return {
                "request_id": request.get("request_id", "unknown"),
                "status": "fuzzification_only",
                "message": f"Solo fuzzificación completada. Error en evaluación de reglas: {str(e)}",
                "sensor_data_received": sensor_data,
                "fuzzification_results": self._serialize_fuzzification_results(fuzzification_results),
                "processing_time_ms": (datetime.now() - start_time).total_seconds() * 1000
            }
    
    def _serialize_fuzzification_results(self, results: List[FuzzificationResult]) -> List[Dict[str, Any]]:
        """Serializa los resultados de fuzzificación para la respuesta."""
        return [
            {
                "variable_name": result.variable_name,
                "sensor_id": result.sensor_id,
                "crisp_value": result.crisp_value,
                "activated_terms": result.activated_terms,
                "dominant_term": result.get_dominant_term()[0],
                "processing_time_ms": result.processing_time_ms
            }
            for result in results
        ]
    
    def _find_duration_variable(
        self,
        power_variable_name: str,
        routines_payload: List[Dict[str, Any]]
    ) -> Optional[Dict[str, Any]]:
        """Busca la variable de duración asociada a una variable de potencia.

        Convenciones de nombres:
        - "Potencia X" → "Duración de X"
        - "Control X" → "Duración de X"

        Args:
            power_variable_name: Nombre de la variable de potencia/control
            routines_payload: Lista de variables defuzzificadas

        Returns:
            Dict con la variable de duración encontrada, o None
        """
        # Extraer el nombre base del actuador
        if "Potencia" in power_variable_name:
            # "Potencia del Ventilador" → "Ventilador" → "Duración de Ventilación"
            base_name = power_variable_name.replace("Potencia del ", "").replace("Potencia de ", "").replace("Potencia ", "")
        elif "Control" in power_variable_name:
            # "Control Calefactor Aire" → "Calefactor Aire" → "Duración de Calefacción de Aire"
            base_name = power_variable_name.replace("Control ", "")
        else:
            return None

        # Mapeo de nombres de actuadores a nombres de duración
        # Estos mapeos se infieren del seed data
        duration_mappings = {
            "Ventilador": "Duración de Ventilación",
            "Calefactor Aire": "Duración de Calefacción de Aire",
            "Calefactor Agua": "Duración de Calefacción de Agua",
            "Bomba Riego": "Duración de Riego",
            "Bomba Aireación": "Duración de Aireación",
            "Humidificador": "Duración de Humidificación",
            "Luz": "Duración de Luz"
        }

        # Buscar el nombre de duración esperado
        duration_name = duration_mappings.get(base_name)
        if not duration_name:
            self.logger.warning(
                f"No se encontró mapeo de duración para '{power_variable_name}' (base: '{base_name}')"
            )
            return None

        # Buscar en routines_payload
        for routine in routines_payload:
            if routine.get("variable_name") == duration_name:
                return routine

        self.logger.warning(
            f"No se encontró variable de duración '{duration_name}' en routines_payload"
        )
        return None

    def _serialize_rule_evaluation_result(
        self,
        result: BatchRuleEvaluationResult,
        routines_payload: List[Dict[str, Any]] = None,
        fuzzy_rules: List["FuzzyRule"] = None,
        fuzzy_variables: List["FuzzyVariable"] = None
    ) -> Dict[str, Any]:
         """Serializa el resultado de evaluación de reglas para la respuesta (modelo Mamdani)."""
         if routines_payload is None:
             routines_payload = []
         if fuzzy_rules is None:
             fuzzy_rules = []
         if fuzzy_variables is None:
             fuzzy_variables = []

         # Crear índice de routines por variable_id para búsqueda rápida
         routines_by_variable = {}
         for r in routines_payload:
             if "variable_id" in r:
                 routines_by_variable[r["variable_id"]] = r

         # Crear índice de variables por ID para obtener reference_code
         variables_by_id = {str(var.id): var for var in fuzzy_variables}

         # Crear índice de reglas por ID
         rules_by_id = {str(rule.id): rule for rule in fuzzy_rules}

         # Crear mapeo de rule_id a output_values
         rule_output_map = {}

         # Crear índice de routines por _routine_id para formato antiguo
         routines_by_routine_id = {}
         for r in routines_payload:
             if "_routine_id" in r:
                 routines_by_routine_id[r["_routine_id"]] = r

         for rule_result in result.get_activated_rules():
             rule = rules_by_id.get(str(rule_result.rule_id))
             mapped_outputs = []

             # Intentar con formato nuevo (consecuentes)
             if rule and rule.has_consequents():
                 # Para cada consecuente de la regla, buscar el output correspondiente
                 for consequent in rule.consequents:
                     variable_id = str(consequent.variable_id)
                     routine = routines_by_variable.get(variable_id)

                     if not routine:
                         continue

                     variable = variables_by_id.get(variable_id)
                     if not variable:
                         self.logger.warning(f"Variable not found for ID {variable_id}")
                         continue

                     command = routine.get("command", {})
                     variable_name = routine.get("variable_name", "")

                     if "Duración" in variable_name or "DURACION" in variable_name.upper():
                         continue

                     output_dict = {
                         "reference_code": variable.reference_code,
                         "duration": 0.5
                     }

                     duration_routine = self._find_duration_variable(variable_name, routines_payload)
                     if duration_routine:
                         duration_value = duration_routine.get("crisp_value", 0.5)
                         output_dict["duration"] = max(0.5, min(10000.0, float(duration_value)))
                         self.logger.debug(
                             f"Duración encontrada para {variable_name}: {output_dict['duration']:.1f}s"
                         )
                     else:
                         self.logger.warning(
                             f"No se encontró duración para {variable_name}, usando valor mínimo 0.5s"
                         )

                     if "power" in command:
                         output_dict["power"] = command["power"]
                     elif "dutyCycle" in command:
                         output_dict["dutyCycle"] = command["dutyCycle"]

                     mapped_outputs.append(output_dict)

             # Fallback: formato antiguo con rutinas basadas en consequent string
             elif rule_result.consequent and routines_by_routine_id:
                 routine = routines_by_routine_id.get(rule_result.consequent)
                 if routine and "steps" in routine:
                     # Formato antiguo: extraer de steps
                     for step in routine["steps"]:
                         output_dict = {
                             "reference_code": step.get("outputVariable", "unknown"),
                             "duration": step.get("duration", 0.5)
                         }
                         if "power" in step:
                             output_dict["power"] = step["power"]
                         elif "dutyCycle" in step:
                             output_dict["dutyCycle"] = step["dutyCycle"]

                         mapped_outputs.append(output_dict)

             rule_output_map[rule_result.rule_id] = mapped_outputs
         
         return {
             "rules_evaluated": result.rules_evaluated,
             "rules_activated": result.rules_activated,
             "total_processing_time_ms": result.total_processing_time_ms,
             "evaluation_timestamp": result.evaluation_timestamp.isoformat(),
             "rule_results": [
                 {
                     "rule_id": rule_result.rule_id,
                     "rule_name": rule_result.rule_name,
                     "firing_strength": rule_result.firing_strength,
                     "is_activated": rule_result.is_activated,
                     "condition_evaluations": rule_result.condition_evaluations,
                     "evaluation_time_ms": rule_result.evaluation_time_ms,
                     "error_message": rule_result.error_message
                 }
                 for rule_result in result.rule_results
             ],
             "activated_rules": [
                 {
                     "rule_id": rule_result.rule_id,
                     "rule_name": rule_result.rule_name,
                     "firing_strength": rule_result.firing_strength,
                     "output_values": rule_output_map.get(rule_result.rule_id, [])
                 }
                 for rule_result in result.get_activated_rules()
             ]
         }

    def evaluate_rules(
        self,
        system: FuzzySystem,
        rules: List[FuzzyRule],
        fuzzification_results: List[FuzzificationResult],
        variables: List[FuzzyVariable]
    ) -> BatchRuleEvaluationResult:
        """Evalúa las reglas fuzzy usando los resultados de fuzzificación.

        Convertido a síncrono: Delega a RuleEvaluationEngine que es síncrono.

        Args:
            system: Sistema fuzzy que contiene las reglas
            rules: Lista de reglas fuzzy a evaluar
            fuzzification_results: Resultados de la fuzzificación
            variables: Variables del sistema fuzzy

        Returns:
            Resultado de la evaluación de reglas con firing strengths

        Raises:
            ValidationError: Si hay problemas con las reglas o datos
        """
        try:
            # Usar el motor de evaluación de reglas existente con el sistema proporcionado (síncrono)
            return self.rule_evaluation_engine.evaluate_rules(
                system=system,
                rules=rules,
                fuzzification_results=fuzzification_results
            )
            
        except Exception as e:
            self.logger.error(f"Error en evaluación de reglas: {e}")
            raise ValidationError(f"Error evaluando reglas: {str(e)}")


    async def _defuzzify_term_with_firing_strength(
        self,
        term: FuzzyTerm,
        firing_strength: float
    ) -> Optional[float]:
        """Defuzzifica un término individual usando la fuerza de activación.
        
        Args:
            term: Término fuzzy a defuzzificar
            firing_strength: Fuerza de activación de la regla
            
        Returns:
            Valor crisp defuzzificado o None si hay error
        """
        try:
            # Determinar universo de discurso del término
            mf = term.membership_function
            universe = np.linspace(mf.universe_min, mf.universe_max, 1000)
            
            # Crear función de membresía
            membership_values = self._create_membership_function(mf, universe)
            
            # Aplicar firing strength (método de implicación mínimo)
            clipped_membership = np.minimum(membership_values, firing_strength)
            
            # Defuzzificar usando centroide
            if np.sum(clipped_membership) > 0:
                centroid = fuzz.defuzz(universe, clipped_membership, 'centroid')
                return float(centroid)
            else:
                # Si no hay área tras clipear con firing_strength, retornar 0.0
                self.logger.warning(
                    f"Término {term.label}: clipped_membership vacío (firing_strength={firing_strength}), retornando 0.0"
                )
                return 0.0
                
        except Exception as e:
            self.logger.error(f"Error defuzzificando término {term.name}: {e}")
            return None

    async def _defuzzify_variable(
        self,
        variable: FuzzyVariable,
        terms: List[FuzzyTerm],
        rule_evaluation_result: BatchRuleEvaluationResult
    ) -> Optional[float]:
        """Defuzzifica una variable de salida usando el método del centroide."""
        try:
            # Determinar universo de discurso
            universe_min, universe_max = self._determine_universe(terms)
            universe = np.linspace(universe_min, universe_max, 1000)
            
            # Inicializar función de salida agregada
            aggregated_output = np.zeros_like(universe)
            
            # Para cada regla activada, agregar su contribución
            for rule_result in rule_evaluation_result.get_activated_rules():
                if rule_result.firing_strength > 0:
                    # Buscar el término de salida de esta regla para esta variable
                    output_term = self._find_output_term_for_rule(rule_result, variable, terms)
                    
                    if output_term:
                        # Crear función de membresía del término
                        term_membership = self._create_membership_function(
                            output_term.membership_function, universe
                        )
                        
                        # Aplicar firing strength (método de implicación mínimo)
                        clipped_membership = np.minimum(
                            term_membership, rule_result.firing_strength
                        )
                        
                        # Agregar a la salida (método de agregación máximo)
                        aggregated_output = np.maximum(aggregated_output, clipped_membership)
            
            # Defuzzificar usando centroide
            if np.sum(aggregated_output) > 0:
                centroid = fuzz.defuzz(universe, aggregated_output, 'centroid')
                return float(centroid)
            else:
                self.logger.warning(f"No hay salida agregada para variable {variable.name}")
                return None
                
        except Exception as e:
            self.logger.error(f"Error defuzzificando variable {variable.name}: {e}")
            return None

    def _find_output_term_for_rule(
        self,
        rule_result,
        variable: FuzzyVariable,
        terms: List[FuzzyTerm]
    ) -> Optional[FuzzyTerm]:
        """Encuentra el término de salida correspondiente a una regla para una variable específica."""
        # Esta es una implementación simplificada
        # En un sistema real, necesitarías parsear las consecuencias de las reglas
        # Por ahora, retornamos el primer término de la variable
        var_terms = [term for term in terms if term.variable_id == variable.id]
        return var_terms[0] if var_terms else None

    def _infer_universe_from_terms(
        self,
        variable: FuzzyVariable,
        all_terms: List[FuzzyTerm]
    ) -> Tuple[float, float]:
        """Infiere el universo de discurso de una variable desde sus términos."""
        var_terms = [t for t in all_terms if t.variable_id == variable.id]

        if not var_terms:
            # Fallback: universo por defecto para cualquier tipo de actuador
            return 0.0, 100.0

        # Obtener min/max de todos los términos
        min_vals = [t.membership_function.universe_min for t in var_terms]
        max_vals = [t.membership_function.universe_max for t in var_terms]

        return min(min_vals), max(max_vals)

    async def defuzzify_variables_mamdani(
        self,
        output_variables: List[FuzzyVariable],
        rule_evaluation_result: BatchRuleEvaluationResult,
        all_terms: List[FuzzyTerm]
    ) -> Dict[str, float]:
        """Defuzzifica variables de salida usando agregación Mamdani completa.

        Para cada variable de salida:
        1. Identificar todas las reglas que contribuyen a esa variable
        2. Aplicar implicación (min) con firing_strength a cada término
        3. Agregar funciones (max)
        4. Defuzzificar con centroide

        Args:
            output_variables: Lista de variables de salida del sistema
            rule_evaluation_result: Resultado de evaluación de reglas con firing strengths
            all_terms: Todos los términos fuzzy del sistema

        Returns:
            Dict[variable_id, crisp_value] - Valores defuzzificados por variable
        """
        try:
            defuzzified_values = {}

            for variable in output_variables:
                # Determinar universo de discurso
                if variable.universe_min is not None and variable.universe_max is not None:
                    universe_min = variable.universe_min
                    universe_max = variable.universe_max
                else:
                    # Inferir desde términos
                    universe_min, universe_max = self._infer_universe_from_terms(variable, all_terms)

                universe = np.linspace(universe_min, universe_max, 1000)

                # Inicializar función agregada
                aggregated_output = np.zeros_like(universe)

                # Para cada regla activada
                rules_contributing = 0
                for rule_result in rule_evaluation_result.get_activated_rules():
                    if rule_result.firing_strength <= 0:
                        continue

                    # Obtener término de salida para esta variable
                    # NOTA: En el flujo actual basado en rutinas, esto requiere extensión
                    # Por ahora, usar el método existente
                    output_term = self._find_output_term_for_rule(
                        rule_result, variable, all_terms
                    )

                    if not output_term:
                        continue

                    # Crear función de membresía
                    term_membership = self._create_membership_function(
                        output_term.membership_function, universe
                    )

                    # Implicación (min con firing strength)
                    clipped = np.minimum(term_membership, rule_result.firing_strength)

                    # Agregación (max)
                    aggregated_output = np.maximum(aggregated_output, clipped)
                    rules_contributing += 1

                # Defuzzificar
                if np.sum(aggregated_output) > 0:
                    crisp_value = fuzz.defuzz(universe, aggregated_output, 'centroid')
                    self.logger.debug(
                        f"Variable {variable.name}: {rules_contributing} reglas → centroide = {crisp_value:.2f}"
                    )
                else:
                    # Sin agregación: retornar 0.0
                    crisp_value = 0.0
                    self.logger.warning(
                        f"Variable {variable.name}: sin agregación, retornando 0.0 (sin acción)"
                    )

                defuzzified_values[str(variable.id)] = float(crisp_value)

            return defuzzified_values

        except Exception as e:
            self.logger.error(f"Error en defuzzificación Mamdani: {e}")
            raise ValidationError(f"Error en defuzzificación Mamdani: {str(e)}")

    async def defuzzify_from_consequents(
        self,
        fuzzy_rules: List["FuzzyRule"],
        output_variables: List[FuzzyVariable],
        rule_evaluation_result: BatchRuleEvaluationResult,
        all_terms: List[FuzzyTerm]
    ) -> Dict[str, float]:
        """Defuzzifica usando consecuentes directos (modelo Mamdani completo).

        Este método implementa el flujo de inferencia Mamdani completo:
        1. Agrupar contribuciones de múltiples reglas por variable de salida
        2. Para cada variable:
           - Agregar las funciones de membresía de todos los términos activados
           - Aplicar implicación (min) con firing_strength
           - Agregar funciones (método configurable: max, sum, probabilistic_or)
           - Defuzzificar con centroide
        3. Aplicar lógica de actuador (PWM continuo o DIGITAL con threshold)

        Args:
            fuzzy_rules: Lista de reglas con consecuentes directos
            output_variables: Variables de salida del sistema
            rule_evaluation_result: Resultado de evaluación con firing strengths
            all_terms: Todos los términos fuzzy del sistema

        Returns:
            Dict[variable_id, crisp_value] - Valores defuzzificados
        """
        try:
            defuzzified_values = {}

            # Crear índice de términos por ID para búsqueda rápida
            terms_by_id = {str(term.id): term for term in all_terms}

            # Crear índice de reglas por ID
            rules_by_id = {str(rule.id): rule for rule in fuzzy_rules}

            # Agrupar contribuciones por variable de salida
            contributions_by_variable = {}  # variable_id -> List[(term, firing_strength, aggregation_method)]

            for rule_result in rule_evaluation_result.get_activated_rules():
                if rule_result.firing_strength <= 0:
                    continue

                rule = rules_by_id.get(str(rule_result.rule_id))
                if not rule:
                    self.logger.warning(f"Regla {rule_result.rule_id} no encontrada en rules_by_id")
                    continue

                if not rule.has_consequents():
                    self.logger.warning(
                        f"Regla {rule.name} (ID: {rule.id}) no tiene consecuentes. "
                        f"consequents={rule.consequents}"
                    )
                    continue

                # Para cada consecuente de esta regla
                for consequent in rule.consequents:
                    variable_id = str(consequent.variable_id)

                    if variable_id not in contributions_by_variable:
                        contributions_by_variable[variable_id] = []

                    # Agregar todos los términos de este consecuente
                    for term_id in consequent.terms:
                        term = terms_by_id.get(str(term_id))
                        if term:
                            contributions_by_variable[variable_id].append({
                                "term": term,
                                "firing_strength": rule_result.firing_strength,
                                "aggregation_method": consequent.aggregation_method
                            })

            # Defuzzificar cada variable de salida
            for variable in output_variables:
                variable_id = str(variable.id)
                contributions = contributions_by_variable.get(variable_id, [])

                if not contributions:
                    # Sin contribuciones: retornar 0.0 (sin acción)
                    crisp_value = 0.0
                    self.logger.warning(
                        f"Variable {variable.name}: sin contribuciones de reglas activadas, retornando 0.0 (sin acción)"
                    )
                    defuzzified_values[variable_id] = float(crisp_value)
                    continue

                # Determinar universo de discurso
                if variable.universe_min is not None and variable.universe_max is not None:
                    universe_min = variable.universe_min
                    universe_max = variable.universe_max
                else:
                    universe_min, universe_max = self._infer_universe_from_terms(variable, all_terms)

                universe = np.linspace(universe_min, universe_max, 1000)

                # Inicializar función agregada
                aggregated_output = np.zeros_like(universe)

                # Método de agregación (usar el primer consecuente, todos deberían ser iguales)
                aggregation_method = contributions[0]["aggregation_method"]

                # Procesar cada contribución
                for contrib in contributions:
                    term = contrib["term"]
                    firing_strength = contrib["firing_strength"]

                    # Crear función de membresía
                    term_membership = self._create_membership_function(
                        term.membership_function, universe
                    )

                    # Implicación (min con firing strength)
                    clipped = np.minimum(term_membership, firing_strength)

                    # Agregación según método
                    if aggregation_method == "max":
                        aggregated_output = np.maximum(aggregated_output, clipped)
                    elif aggregation_method == "sum":
                        # Suma limitada (bounded sum)
                        aggregated_output = np.minimum(aggregated_output + clipped, 1.0)
                    elif aggregation_method == "probabilistic_or":
                        # OR probabilístico: a + b - a*b
                        aggregated_output = aggregated_output + clipped - (aggregated_output * clipped)
                    else:
                        # Fallback a max
                        aggregated_output = np.maximum(aggregated_output, clipped)

                # Defuzzificar con centroide
                if np.sum(aggregated_output) > 0:
                    crisp_value = fuzz.defuzz(universe, aggregated_output, 'centroid')
                    self.logger.debug(
                        f"Variable {variable.name}: {len(contributions)} contribuciones → centroide = {crisp_value:.2f}"
                    )
                else:
                    # Agregación vacía (no debería ocurrir si hay contributions): retornar 0.0
                    crisp_value = 0.0
                    self.logger.warning(
                        f"Variable {variable.name}: agregación vacía tras procesar {len(contributions)} contribuciones, retornando 0.0"
                    )

                defuzzified_values[variable_id] = float(crisp_value)

            return defuzzified_values

        except Exception as e:
            self.logger.error(f"Error en defuzzify_from_consequents: {e}")
            raise ValidationError(f"Error en defuzzify_from_consequents: {str(e)}")

    async def apply_actuator_logic(
        self,
        variable: FuzzyVariable,
        crisp_value: float
    ) -> Dict[str, Any]:
        """Convierte valor crisp en comando de actuador según tipo.

        Args:
            variable: Variable fuzzy con configuración de actuador
            crisp_value: Valor defuzzificado

        Returns:
            Dict con "power" (DIGITAL) o "dutyCycle" (PWM)

        Raises:
            ValidationError: Si la variable no tiene actuator_type definido
        """
        if not variable.actuator_type:
            raise ValidationError(
                f"Variable output '{variable.name}' debe tener actuator_type definido (PWM o DIGITAL)"
            )

        try:
            if variable.actuator_type == "DIGITAL":
                # Aplicar threshold configurable
                threshold = variable.defuzzification_threshold
                if crisp_value >= threshold:
                    command = {"power": "ON"}
                else:
                    command = {"power": "OFF"}
                return command

            elif variable.actuator_type == "PWM":
                # Valor continuo (clamped 0-100)
                duty_cycle = max(0.0, min(100.0, crisp_value))
                command = {"dutyCycle": round(duty_cycle, 2)}
                return command

            else:
                raise ValidationError(
                    f"actuator_type inválido '{variable.actuator_type}' para variable '{variable.name}'. "
                    f"Debe ser 'PWM' o 'DIGITAL'"
                )

        except ValidationError:
            raise
        except Exception as e:
            self.logger.error(f"Error aplicando lógica de actuador: {e}")
            raise ValidationError(f"Error aplicando lógica de actuador: {str(e)}")

    async def complete_fuzzy_evaluation(
        self,
        system: FuzzySystem,
        variables: List[FuzzyVariable],
        terms: List[FuzzyTerm],
        rules: List[FuzzyRule],
        sensor_readings: Dict[str, float]
    ) -> Dict[str, Any]:
        """Realiza una evaluación fuzzy completa del sistema.
        
        Este método orquesta todo el flujo:
        1. Fuzzificación de entradas
        2. Evaluación de reglas
        3. Defuzzificación de salidas
        
        Args:
            system: Sistema fuzzy a evaluar
            variables: Variables del sistema (entrada y salida)
            terms: Términos fuzzy de todas las variables
            rules: Reglas del sistema
            sensor_readings: Lecturas de sensores {sensor_id: value}
            
        Returns:
            Diccionario con resultados completos de la evaluación
            
        Raises:
            ValidationError: Si hay problemas en cualquier paso
        """
        start_time = datetime.now()

        try:
            # Paso 1: Fuzzificación de entradas
            fuzzification_results = await self.fuzzify_sensor_readings(
                variables=variables,
                terms=terms,
                sensor_readings=sensor_readings
            )
            
            # Paso 2: Evaluación de reglas (síncrono)
            rule_evaluation_result = self.evaluate_rules(
                system=system,
                rules=rules,
                fuzzification_results=fuzzification_results,
                variables=variables
            )

            # Paso 3: Defuzzificación Mamdani con consecuentes directos
            self.logger.info("Defuzzificando con agregación Mamdani")

            # Obtener variables de salida
            output_variables = [v for v in variables if v.is_output()]

            self.logger.info(
                f"Variables totales: {len(variables)}, "
                f"Variables de salida: {len(output_variables)}"
            )
            for v in variables:
                self.logger.debug(f"Variable: {v.name}, tipo={v.variable_type}, is_output={v.is_output()}")

            # Defuzzificar con agregación multi-regla
            defuzzified_values = await self.defuzzify_from_consequents(
                fuzzy_rules=rules,
                output_variables=output_variables,
                rule_evaluation_result=rule_evaluation_result,
                all_terms=terms
            )

            # Convertir valores defuzzificados en comandos de actuador
            routines_payload = []
            for variable in output_variables:
                variable_id = str(variable.id)
                crisp_value = defuzzified_values.get(variable_id)

                if crisp_value is not None:
                    # Las variables de duración no necesitan command, solo el crisp_value
                    is_duration = "DURACION" in (variable.reference_code or "").upper() or "Duración" in variable.name

                    if is_duration:
                        # Variable de duración: solo guardar el valor en segundos
                        routines_payload.append({
                            "variable_id": variable_id,
                            "variable_name": variable.name,
                            "crisp_value": crisp_value,
                            "command": {}  # Sin command para duraciones
                        })
                    else:
                        # Variable de control: generar command (power/dutyCycle)
                        command = await self.apply_actuator_logic(variable, crisp_value)
                        routines_payload.append({
                            "variable_id": variable_id,
                            "variable_name": variable.name,
                            "crisp_value": crisp_value,
                            "command": command
                        })
            
            # Construir respuesta completa
            total_time = (datetime.now() - start_time).total_seconds() * 1000

            result = {
                "system_id": system.id,
                "system_name": system.name,
                "evaluation_timestamp": start_time.isoformat(),
                "sensor_readings": sensor_readings,
                "fuzzification_results": self._serialize_fuzzification_results(fuzzification_results),
                "rule_evaluation": self._serialize_rule_evaluation_result(rule_evaluation_result, routines_payload, rules, variables),
                "routines_payload": routines_payload,
                "total_processing_time_ms": total_time,
                "status": "completed"
            }

            return result
            
        except Exception as e:
            self.logger.error(f"Error en evaluación fuzzy completa: {e}")
            raise ValidationError(f"Error en evaluación fuzzy completa: {str(e)}")