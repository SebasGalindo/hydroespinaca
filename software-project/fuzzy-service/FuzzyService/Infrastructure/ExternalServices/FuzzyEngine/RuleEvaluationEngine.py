from __future__ import annotations

import logging
from typing import Dict, List, Any, Optional, Tuple
from datetime import datetime, timezone

from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule
from FuzzyService.Domain.Entities.fuzzy_system import FuzzySystem
from FuzzyService.Domain.ValueObjects.RuleCondition import RuleCondition
from FuzzyService.Domain.Enums.LogicalOperator import LogicalOperator
from FuzzyService.Domain.Enums.OperatorMethods import AndOperatorMethod, OrOperatorMethod
from FuzzyService.Domain.Errors.DomainErrors import ValidationError
from FuzzyService.Infrastructure.ExternalServices.FuzzyEngine.FuzzyResultTypes import (
    FuzzificationResult,
    RuleActivationResult,
    BatchRuleEvaluationResult
)


class RuleEvaluationEngine:
    """Motor de evaluación de reglas fuzzy.
    
    Este motor se encarga del paso 4 del flujo:
    4. Evaluación de reglas - Con las reglas cargadas del sistema se hace la evaluación,
       teniendo en cuenta:
       - Solo reglas del sistema activo
       - Construcción correcta de reglas con condiciones y conectores
       - Configuración de operadores AND/OR del sistema fuzzy
       - Cálculo correcto del firing strength
    """
    
    def __init__(self):
        self.logger = logging.getLogger(f"{__name__}.{self.__class__.__name__}")
        
    def evaluate_rules(
        self,
        system: FuzzySystem,
        rules: List[FuzzyRule],
        fuzzification_results: List[FuzzificationResult],
        is_simulation: bool = False
    ) -> BatchRuleEvaluationResult:
        """Evalúa todas las reglas del sistema con los resultados de fuzzificación.

        Convertido a síncrono: No realiza operaciones I/O ni llamadas asíncronas.
        Solo ejecuta cálculos computacionales para evaluación de reglas fuzzy.

        Args:
            system: Sistema fuzzy con configuración de operadores
            rules: Lista de reglas del sistema activo
            fuzzification_results: Resultados de la fuzzificación de variables
            is_simulation: Si es True, se omite la validación de estado operacional

        Returns:
            Resultado de la evaluación de todas las reglas

        Raises:
            ValidationError: Si hay problemas con la configuración o datos
        """
        start_time = datetime.now()
        
        try:
            self.logger.info(
                f"Iniciando evaluación de {len(rules)} reglas para sistema {system.id}"
            )
            
            # Validar que el sistema esté activo (omitir en simulación)
            if not is_simulation and not self._is_system_operational(system):
                raise ValidationError(
                    f"Sistema {system.id} no está operativo para evaluación de reglas"
                )
            
            # Crear índice de resultados de fuzzificación por variable
            fuzz_index = self._create_fuzzification_index(fuzzification_results)
            
            # Resultado del lote
            batch_result = BatchRuleEvaluationResult()
            
            # Evaluar cada regla individualmente
            for rule in rules:
                try:
                    rule_result = self._evaluate_single_rule(
                        rule, system, fuzz_index
                    )
                    batch_result.add_rule_result(rule_result)
                    
                    if rule_result.is_activated:
                        self.logger.debug(
                            f"Regla activada: {rule.name} (ID: {rule.id}) "
                            f"con firing strength {rule_result.firing_strength:.3f}"
                        )
                    
                except Exception as e:
                    self.logger.error(f"Error evaluando regla {rule.id}: {e}")
                    # Crear resultado de error para la regla
                    error_result = RuleActivationResult(
                        rule_id=str(rule.id) if rule.id else "unknown",
                        rule_name=rule.name or "unnamed",
                        consequent=None  # No usado en modelo Mamdani (consecuentes en rule.consequents)
                    )
                    error_result.error_message = str(e)
                    batch_result.add_rule_result(error_result)
            
            # Calcular tiempo total de procesamiento
            processing_time = (datetime.now() - start_time).total_seconds() * 1000
            batch_result.total_processing_time_ms = processing_time
            
            self.logger.info(
                f"Evaluación completada: {batch_result.rules_activated}/{batch_result.rules_evaluated} "
                f"reglas activadas en {processing_time:.2f}ms"
            )
            
            return batch_result
            
        except Exception as e:
            self.logger.error(f"Error en evaluación de reglas: {e}")
            raise ValidationError(f"Error en proceso de evaluación de reglas: {str(e)}")
    
    def _is_system_operational(self, system: FuzzySystem) -> bool:
        """Valida que el sistema esté en estado operativo."""
        # Verificar que el sistema tenga estado activo
        status = getattr(system, 'status', None)
        if status is None:
            self.logger.warning(f"Sistema {system.id} no tiene estado definido")
            return False
            
        # Solo sistemas activos o en testing pueden ser evaluados
        operational_statuses = ['active', 'in_use', 'testing']
        is_operational = str(status).lower() in operational_statuses
        
        if not is_operational:
            self.logger.warning(
                f"Sistema {system.id} no está operativo (status: {status})"
            )
            
        return is_operational
    
    def _create_fuzzification_index(
        self, 
        fuzzification_results: List[FuzzificationResult]
    ) -> Dict[str, FuzzificationResult]:
        """Crea un índice de resultados de fuzzificación por variable_id.
        Indexa usando variable_id como clave principal.
        """
        index = {}
        for result in fuzzification_results:
            # Indexar por variable_id (clave principal)
            if result.variable_id:
                index[result.variable_id] = result
                self.logger.debug(
                    f"Variable indexada: {result.variable_id} ({result.variable_name}) con {len(result.activated_terms)} términos activos"
                )
            else:
                self.logger.warning(
                    f"FuzzificationResult sin variable_id: {result.variable_name}"
                )
        
        return index
    
    def _evaluate_single_rule(
        self,
        rule: FuzzyRule,
        system: FuzzySystem,
        fuzz_index: Dict[str, FuzzificationResult]
    ) -> RuleActivationResult:
        """Evalúa una regla individual.
        
        Args:
            rule: Regla a evaluar
            system: Sistema fuzzy con configuración de operadores
            fuzz_index: Índice de resultados de fuzzificación
            
        Returns:
            Resultado de la evaluación de la regla
        """
        start_time = datetime.now()
        
        result = RuleActivationResult(
            rule_id=str(rule.id) if rule.id else "unknown",
            rule_name=rule.name or "unnamed",
            consequent=None  # No usado en modelo Mamdani (consecuentes en rule.consequents)
        )
        
        try:
            # Validar que la regla tenga condiciones
            if not rule.conditions or len(rule.conditions) == 0:
                self.logger.warning(f"Regla {rule.id} no tiene condiciones")
                return result
            
            # Evaluar cada condición de la regla
            condition_values = []
            for i, condition_data in enumerate(rule.conditions):
                try:
                    condition_value = self._evaluate_condition(
                        condition_data, fuzz_index, result
                    )
                    condition_values.append(condition_value)
                    
                except Exception as e:
                    self.logger.error(
                        f"Error evaluando condición {i} de regla {rule.id}: {e}"
                    )
                    # Si una condición falla, la regla no se activa
                    condition_values.append(0.0)
            
            # Calcular firing strength usando conectores
            if len(condition_values) == 1:
                # Una sola condición
                firing_strength = condition_values[0]
            else:
                # Múltiples condiciones - usar conectores
                firing_strength = self._calculate_firing_strength(
                    condition_values, rule.connectors, system
                )
            
            result.set_firing_strength(firing_strength)
            
            # Calcular tiempo de evaluación
            evaluation_time = (datetime.now() - start_time).total_seconds() * 1000
            result.evaluation_time_ms = evaluation_time
            
            return result
            
        except Exception as e:
            self.logger.error(f"Error evaluando regla {rule.id}: {e}")
            result.error_message = str(e)
            return result
    
    def _evaluate_condition(
        self,
        condition_data: Dict[str, Any],
        fuzz_index: Dict[str, FuzzificationResult],
        result: RuleActivationResult
    ) -> float:
        """Evalúa una condición individual de una regla.
        
        Args:
            condition_data: Datos de la condición (variable_id, term_id, etc.)
            fuzz_index: Índice de resultados de fuzzificación
            result: Resultado donde registrar la evaluación
            
        Returns:
            Grado de pertenencia de la condición (0.0 - 1.0)
        """
        try:
            # Extraer información de la condición
            variable_id = condition_data.get('variableId')
            term_label = condition_data.get('value')

            if not variable_id or not term_label:
                self.logger.warning(
                    f"Condición incompleta: variableId={variable_id}, value={term_label}"
                )
                return 0.0
            
            # Buscar resultado de fuzzificación para la variable
            fuzz_result = fuzz_index.get(variable_id)
            if not fuzz_result:
                self.logger.warning(
                    f"No se encontró resultado de fuzzificación para variable {variable_id}"
                )
                return 0.0
            
            # Obtener grado de pertenencia del término
            membership_degree = fuzz_result.activated_terms.get(term_label, 0.0)
            
            # Registrar evaluación de la condición
            result.add_condition_evaluation(
                variable_name=variable_id,
                term_label=term_label,
                membership_degree=membership_degree,
                sensor_value=fuzz_result.crisp_value
            )
            
            self.logger.debug(
                f"Condición evaluada: {variable_id}.{term_label} = {membership_degree:.3f} "
                f"(valor sensor: {fuzz_result.crisp_value})"
            )
            
            return membership_degree
            
        except Exception as e:
            self.logger.error(f"Error evaluando condición: {e}")
            return 0.0
    
    def _calculate_firing_strength(
        self,
        condition_values: List[float],
        connectors: List[str],
        system: FuzzySystem
    ) -> float:
        """Calcula el firing strength usando los conectores de la regla.
        
        Args:
            condition_values: Lista de valores de las condiciones
            connectors: Lista de conectores entre condiciones
            system: Sistema fuzzy con configuración de operadores
            
        Returns:
            Firing strength calculado (0.0 - 1.0)
        """
        try:
            if len(condition_values) <= 1:
                return condition_values[0] if condition_values else 0.0
            
            # Obtener configuración de operadores del sistema
            operators_config = getattr(system, 'operators', None)
            if not operators_config:
                self.logger.warning(
                    f"Sistema {system.id} no tiene configuración de operadores, usando defaults"
                )
                # Usar operadores por defecto
                and_method = AndOperatorMethod.MIN
                or_method = OrOperatorMethod.MAX
            else:
                and_method = getattr(operators_config, 'and_method', AndOperatorMethod.MIN)
                or_method = getattr(operators_config, 'or_method', OrOperatorMethod.MAX)
            
            # Si no hay conectores explícitos, asumir AND
            if not connectors or len(connectors) == 0:
                self.logger.debug("No hay conectores explícitos, usando AND por defecto")
                return self._apply_and_operator(condition_values, and_method)
            
            # Procesar conectores secuencialmente
            result = condition_values[0]
            
            for i, connector in enumerate(connectors):
                if i + 1 >= len(condition_values):
                    break
                    
                next_value = condition_values[i + 1]
                
                if connector.upper() == 'AND':
                    result = self._apply_and_operation(result, next_value, and_method)
                elif connector.upper() == 'OR':
                    result = self._apply_or_operation(result, next_value, or_method)
                else:
                    self.logger.warning(f"Conector desconocido: {connector}, usando AND")
                    result = self._apply_and_operation(result, next_value, and_method)
            
            self.logger.debug(
                f"Firing strength calculado: {result:.3f} "
                f"(valores: {condition_values}, conectores: {connectors})"
            )
            
            return result
            
        except Exception as e:
            self.logger.error(f"Error calculando firing strength: {e}")
            return 0.0
    
    def _apply_and_operator(self, values: List[float], method: AndOperatorMethod) -> float:
        """Aplica operador AND a una lista de valores."""
        if not values:
            return 0.0
        
        # Convertir a string para comparar con valores del enum
        method_str = method.value if hasattr(method, 'value') else str(method)
            
        if method_str == "min":
            return min(values)
        elif method_str == "prod":
            result = 1.0
            for value in values:
                result *= value
            return result
        else:
            self.logger.warning(f"Método AND desconocido: {method}, usando MIN")
            return min(values)
    
    def _apply_and_operation(self, a: float, b: float, method: AndOperatorMethod) -> float:
        """Aplica operador AND entre dos valores."""
        # Convertir a string para comparar con valores del enum
        method_str = method.value if hasattr(method, 'value') else str(method)
        
        if method_str == "min":
            return min(a, b)
        elif method_str == "prod":
            return a * b
        else:
            self.logger.warning(f"Método AND desconocido: {method}, usando MIN")
            return min(a, b)
    
    def _apply_or_operation(self, a: float, b: float, method: OrOperatorMethod) -> float:
        """Aplica operador OR entre dos valores."""
        # Convertir a string para comparar con valores del enum
        method_str = method.value if hasattr(method, 'value') else str(method)
        
        if method_str == "max":
            return max(a, b)
        elif method_str == "sum":
            return min(1.0, a + b)  # Clamp a 1.0
        elif method_str == "probor":
            return a + b - (a * b)
        else:
            self.logger.warning(f"Método OR desconocido: {method}, usando MAX")
            return max(a, b)