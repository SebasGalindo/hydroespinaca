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
from FuzzyService.Domain.Entities.fuzzy_routine import FuzzyRoutine, RoutineStep
from FuzzyService.Domain.ValueObjects.MembershipFunction import MembershipFunction
from FuzzyService.Domain.Enums.MembershipFunctionType import MembershipFunctionType
from FuzzyService.Domain.Errors.DomainErrors import ValidationError, EntityNotFoundError
from FuzzyService.Domain.Interfaces.IFuzzyEngine import IFuzzyEngine
from FuzzyService.Infrastructure.ExternalServices.FuzzyEngine.FuzzyResultTypes import (
    FuzzificationResult,
    BatchRuleEvaluationResult
)
from FuzzyService.Infrastructure.ExternalServices.FuzzyEngine.RuleEvaluationEngine import RuleEvaluationEngine
from FuzzyService.Domain.Interfaces.IFuzzyRoutineRepository import IFuzzyRoutineRepository
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyTermId
from medyator import Medyator
from kink import inject, di


# FuzzificationResult ahora se importa desde FuzzificationTypes


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
                    # Obtener el valor crisp del sensor
                    sensor_value = sensor_readings[variable.device_id]
                    
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
            
            processing_time = (datetime.now() - start_time).total_seconds() * 1000
            self.logger.info(
                f"Fuzzificación completada: {len(results)} variables procesadas en {processing_time:.2f}ms"
            )
            
            return results
            
        except Exception as e:
            self.logger.error(f"Error en fuzzificación: {e}")
            raise ValidationError(f"Error en proceso de fuzzificación: {str(e)}")
    
    def _identify_input_variables(
        self, 
        variables: List[FuzzyVariable], 
        sensor_readings: Dict[str, float]
    ) -> List[FuzzyVariable]:
        """Paso 3.1: Identifica variables de entrada vinculadas a sensor IDs."""
        input_variables = []
        sensor_ids = set(sensor_readings.keys())
        
        for variable in variables:
            # Solo variables de entrada
            if variable.variable_type != "input":
                continue
                
            # Que tengan device_id vinculado a un sensor
            if variable.device_id and variable.device_id in sensor_ids:
                input_variables.append(variable)
                self.logger.debug(
                    f"Variable de entrada identificada: {variable.name} -> sensor {variable.device_id}"
                )
        
        return input_variables
    
    def _get_variable_terms(self, variable: FuzzyVariable, all_terms: List[FuzzyTerm]) -> List[FuzzyTerm]:
        """Paso 3.2: Obtiene los términos asociados a una variable."""
        variable_terms = [
            term for term in all_terms 
            if term.variable_id == variable.id
        ]
        
        self.logger.debug(
            f"Términos encontrados para {variable.name}: {[t.label for t in variable_terms]}"
        )
        
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
                sensor_id=variable.device_id,
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
                        self.logger.debug(
                            f"Término activo: {term.label} = {membership_degree:.3f} "
                            f"para valor {crisp_value}"
                        )
                        
                except Exception as e:
                    self.logger.error(f"Error calculando membresía para término {term.label}: {e}")
                    continue
            
            processing_time = (datetime.now() - start_time).total_seconds() * 1000
            result.processing_time_ms = processing_time
            
            if result.activated_terms:
                dominant_term, max_degree = result.get_dominant_term()
                self.logger.info(
                    f"Variable {variable.name}: valor {crisp_value} -> "
                    f"término dominante '{dominant_term}' ({max_degree:.3f})"
                )
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
            
            # Evaluar reglas usando el motor de evaluación
            rule_evaluation_result = await self.rule_evaluation_engine.evaluate_rules(
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
                "rule_evaluation": self._serialize_rule_evaluation_result(rule_evaluation_result),
                "processing_time_ms": (datetime.now() - start_time).total_seconds() * 1000
            }
            
            self.logger.info(
                f"Evaluación fuzzy completada: {len(fuzzification_results)} variables fuzzificadas, "
                f"{rule_evaluation_result.rules_activated} reglas activadas"
            )
            
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
    
    def _serialize_rule_evaluation_result(self, result: BatchRuleEvaluationResult, routines_payload: List[Dict[str, Any]] = None) -> Dict[str, Any]:
         """Serializa el resultado de evaluación de reglas para la respuesta."""
         if routines_payload is None:
             routines_payload = []
         
         # Crear un mapeo de rule_id a output_values desde routines_payload
         rule_output_map = {}
         for rule_result in result.get_activated_rules():
             if rule_result.consequent:
                 # Buscar la rutina correspondiente en routines_payload
                 for routine in routines_payload:
                     if routine.get("routineId") == str(rule_result.consequent):
                         # Mapear los pasos con el formato correcto
                         mapped_steps = []
                         for step in routine.get("steps", []):
                             mapped_steps.append({
                                 "actuator_id": step.get("actuator", ""),
                                 "power": step.get("power", 0),
                                 "duration": step.get("duration", 0)
                             })
                         rule_output_map[rule_result.rule_id] = mapped_steps
                         break
                 
                 # Si no se encuentra la rutina, usar lista vacía
                 if rule_result.rule_id not in rule_output_map:
                     rule_output_map[rule_result.rule_id] = []
         
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

    async def evaluate_rules(
        self,
        system: FuzzySystem,
        rules: List[FuzzyRule],
        fuzzification_results: List[FuzzificationResult],
        variables: List[FuzzyVariable]
    ) -> BatchRuleEvaluationResult:
        """Evalúa las reglas fuzzy usando los resultados de fuzzificación.
        
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
            # Usar el motor de evaluación de reglas existente con el sistema proporcionado
            return await self.rule_evaluation_engine.evaluate_rules(
                system=system,
                rules=rules,
                fuzzification_results=fuzzification_results
            )
            
        except Exception as e:
            self.logger.error(f"Error en evaluación de reglas: {e}")
            raise ValidationError(f"Error evaluando reglas: {str(e)}")

    async def defuzzify_routines(
        self,
        rule_evaluation_result: BatchRuleEvaluationResult,
        all_terms: List[FuzzyTerm]
    ) -> List[Dict[str, Any]]:
        """Realiza la defuzzificación basada en rutinas para obtener el payload de actuadores.
        
        Args:
            rule_evaluation_result: Resultado de la evaluación de reglas
            all_terms: Todos los términos fuzzy del sistema
            
        Returns:
            Lista de rutinas defuzzificadas en formato payload para actuadores
            
        Raises:
            ValidationError: Si hay problemas en la defuzzificación
        """
        try:
            # Obtener repositorio de rutinas a través del contenedor de dependencias
            routine_repository: IFuzzyRoutineRepository = di[IFuzzyRoutineRepository]
            
            routines_payload = []
            processed_routines = set()  # Para evitar duplicados
            
            # Para cada regla activada, obtener su rutina consecuente
            for rule_result in rule_evaluation_result.get_activated_rules():
                if rule_result.firing_strength > 0 and rule_result.consequent:
                    routine_id = rule_result.consequent
                    
                    # Evitar procesar la misma rutina múltiples veces
                    if str(routine_id) in processed_routines:
                        continue
                    
                    processed_routines.add(str(routine_id))
                    
                    # Obtener la rutina desde el repositorio
                    routine = await routine_repository.get_by_id(routine_id)
                    if not routine:
                        self.logger.warning(f"Rutina {routine_id} no encontrada")
                        continue
                    
                    # Defuzzificar cada paso de la rutina
                    defuzzified_steps = []
                    for step in routine.steps:
                        defuzzified_step = await self._defuzzify_routine_step(
                            step, rule_result.firing_strength, all_terms
                        )
                        if defuzzified_step:
                            defuzzified_steps.append(defuzzified_step)
                    
                    if defuzzified_steps:
                        routine_payload = {
                            "routineId": str(routine.id),
                            "steps": defuzzified_steps
                        }
                        routines_payload.append(routine_payload)
                        
                        self.logger.info(
                            f"Rutina {routine.routine_name} defuzzificada con {len(defuzzified_steps)} pasos"
                        )
            
            return routines_payload
            
        except Exception as e:
            self.logger.error(f"Error en defuzzificación de rutinas: {e}")
            raise ValidationError(f"Error en defuzzificación de rutinas: {str(e)}")

    async def _defuzzify_routine_step(
        self,
        step: RoutineStep,
        firing_strength: float,
        all_terms: List[FuzzyTerm]
    ) -> Optional[Dict[str, Any]]:
        """Defuzzifica un paso de rutina usando los términos de power y duration.
        
        Args:
            step: Paso de la rutina a defuzzificar
            firing_strength: Fuerza de activación de la regla
            all_terms: Todos los términos fuzzy disponibles
            
        Returns:
            Diccionario con el paso defuzzificado o None si hay error
        """
        try:
            # Obtener repositorio de términos
            term_repository: IFuzzyTermRepository = di[IFuzzyTermRepository]
            
            # Buscar los términos de power y duration por ID
            power_term = await term_repository.get_by_id(FuzzyTermId(step.power_term_id))
            duration_term = await term_repository.get_by_id(FuzzyTermId(step.duration_term_id))
            
            if not power_term:
                self.logger.warning(f"Término de power {step.power_term_id} no encontrado")
                return None
                
            if not duration_term:
                self.logger.warning(f"Término de duration {step.duration_term_id} no encontrado")
                return None
            
            # Defuzzificar power y duration independientemente
            power_value = await self._defuzzify_term_with_firing_strength(
                power_term, firing_strength
            )
            
            duration_value = await self._defuzzify_term_with_firing_strength(
                duration_term, firing_strength
            )
            
            if power_value is None or duration_value is None:
                self.logger.warning(f"Error defuzzificando paso {step.step_id}")
                return None
            
            # Obtener el actuator_id real desde el repositorio de variables
            variable_repository: IFuzzyVariableRepository = di[IFuzzyVariableRepository]
            
            # Buscar la variable usando el variable_id del power_term para obtener el device_id (actuator_id)
            actuator_id = None
            try:
                # Usar el variable_id del power_term para buscar directamente la variable
                if power_term.variable_id:
                    variable = await variable_repository.get_by_id(power_term.variable_id)
                    if variable and variable.device_id:
                        actuator_id = variable.device_id
                    else:
                        self.logger.warning(f"Variable {power_term.variable_id} no encontrada o sin device_id")
                else:
                    self.logger.warning(f"Power term {step.power_term_id} no tiene variable_id")
                        
                if not actuator_id:
                    self.logger.warning(f"No se encontró actuator_id para power_term_id {step.power_term_id}")
                    actuator_id = f"actuator_{step.step_id}"  # Fallback
                    
            except Exception as e:
                self.logger.error(f"Error obteniendo actuator_id: {e}")
                actuator_id = f"actuator_{step.step_id}"  # Fallback
            
            return {
                "actuator": {"$oid": str(actuator_id)},
                "power": round(power_value, 2),
                "duration": round(duration_value, 2)
            }
            
        except Exception as e:
            self.logger.error(f"Error defuzzificando paso de rutina: {e}")
            return None

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
                # Si no hay área, usar el centro del universo
                return float((mf.universe_min + mf.universe_max) / 2)
                
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
            self.logger.info(f"Iniciando evaluación completa del sistema {system.name}")
            
            fuzzification_results = await self.fuzzify_sensor_readings(
                variables=variables,
                terms=terms,
                sensor_readings=sensor_readings
            )
            
            # Paso 2: Evaluación de reglas
            rule_evaluation_result = await self.evaluate_rules(
                system=system,
                rules=rules,
                fuzzification_results=fuzzification_results,
                variables=variables
            )
            
            # Paso 3: Defuzzificación basada en rutinas
            routines_payload = await self.defuzzify_routines(
                rule_evaluation_result=rule_evaluation_result,
                all_terms=terms
            )
            
            # Construir respuesta completa
            total_time = (datetime.now() - start_time).total_seconds() * 1000
            
            result = {
                "system_id": system.id,
                "system_name": system.name,
                "evaluation_timestamp": start_time.isoformat(),
                "sensor_readings": sensor_readings,
                "fuzzification_results": self._serialize_fuzzification_results(fuzzification_results),
                "rule_evaluation": self._serialize_rule_evaluation_result(rule_evaluation_result, routines_payload),
                "routines_payload": routines_payload,
                "total_processing_time_ms": total_time,
                "status": "completed"
            }
            
            self.logger.info(
                f"Evaluación completa finalizada: {len(fuzzification_results)} variables fuzzificadas, "
                f"{rule_evaluation_result.rules_activated} reglas activadas, "
                f"{len(routines_payload)} rutinas defuzzificadas en {total_time:.2f}ms"
            )
            
            return result
            
        except Exception as e:
            self.logger.error(f"Error en evaluación fuzzy completa: {e}")
            raise ValidationError(f"Error en evaluación fuzzy completa: {str(e)}")