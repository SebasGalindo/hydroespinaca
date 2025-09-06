from dataclasses import dataclass, field
from typing import Dict, List, Optional, Tuple, Any, Union
from enum import Enum
import logging
import time
import numpy as np
from concurrent.futures import ThreadPoolExecutor, as_completed
import threading

from .FuzzyEngineExceptions import (
    RuleEvaluationException,
    ValidationException,
    PerformanceException
)
from .FuzzyEngineConfiguration import (
    FuzzyEngineConfiguration,
    LogicalOperator
)
from .FuzzyEngineMetrics import FuzzyEngineMetrics, PerformanceMonitor
from .FuzzificationEngine import FuzzificationResult

# Remove Domain imports that caused conflicts
# from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule
# from FuzzyService.Domain.ValueObjects.RuleCondition import RuleCondition
# from FuzzyService.Domain.Enums.LogicalOperator import LogicalOperator as DomainLogicalOperator


class RuleOperatorType(Enum):
    """Tipos de operadores en reglas fuzzy."""
    AND = "AND"
    OR = "OR"
    NOT = "NOT"


@dataclass
class RuleConsequent:
    """Representa la consecuencia de una regla fuzzy.
    
    Esta clase se mantiene en Infrastructure porque es específica
    del motor de evaluación y no es parte del modelo de dominio.
    """
    variable_name: str
    term_name: str
    routine_id: str
    step_number: int
    
    def __post_init__(self):
        """Validación post-inicialización."""
        if not self.variable_name or not isinstance(self.variable_name, str):
            raise ValidationException("variable_name debe ser un string no vacío")
        if not self.term_name or not isinstance(self.term_name, str):
            raise ValidationException("term_name debe ser un string no vacío")
        if not self.routine_id or not isinstance(self.routine_id, str):
            raise ValidationException("routine_id debe ser un string no vacío")
        if not isinstance(self.step_number, int) or self.step_number < 1:
            raise ValidationException("step_number debe ser un entero positivo")


@dataclass
class RuleCondition:
    """Condición para evaluación de reglas en el motor Infrastructure.
    
    Los tests y el motor esperan estos campos: sensor_id, variable_name, term_name y un operador opcional.
    """
    sensor_id: str
    variable_name: str
    term_name: str
    operator: Optional[LogicalOperator] = None

    def __post_init__(self):
        if not isinstance(self.sensor_id, str) or not self.sensor_id.strip():
            raise ValidationException("sensor_id debe ser un string no vacío")
        if not isinstance(self.variable_name, str) or not self.variable_name.strip():
            raise ValidationException("variable_name debe ser un string no vacío")
        if not isinstance(self.term_name, str) or not self.term_name.strip():
            raise ValidationException("term_name debe ser un string no vacío")
        # operator es opcional; si viene, validar tipo
        if self.operator is not None and not isinstance(self.operator, LogicalOperator):
            raise ValidationException("operator debe ser LogicalOperator o None")


@dataclass
class InfraFuzzyRule:
    """Representa una regla fuzzy para el motor de evaluación.
    
    Esta es una versión simplificada para uso interno del motor.
    Las reglas del dominio se convierten a esta estructura.
    """
    rule_id: str
    conditions: List[RuleCondition]
    consequents: List[RuleConsequent]
    logical_operator: LogicalOperator
    priority: int = 1
    description: Optional[str] = None

    def __post_init__(self):
        """Validación post-inicialización."""
        if not self.rule_id or not isinstance(self.rule_id, str):
            raise ValidationException("rule_id debe ser un string no vacío")
        if not self.conditions or not isinstance(self.conditions, list):
            raise ValidationException("conditions debe ser una lista no vacía")
        if not self.consequents or not isinstance(self.consequents, list):
            raise ValidationException("consequents debe ser una lista no vacía")
        if not isinstance(self.logical_operator, LogicalOperator):
            raise ValidationException("logical_operator debe ser un LogicalOperator válido")
        if not isinstance(self.priority, int) or self.priority < 1:
            raise ValidationException("priority debe ser un entero positivo")


# Alias para compatibilidad con código existente
FuzzyRule = InfraFuzzyRule


@dataclass
class RuleEvaluationResult:
    """Resultado de la evaluación de una regla."""
    rule_id: str
    firing_strength: float
    activated_consequents: List[Tuple[RuleConsequent, float]]
    evaluation_time_ms: float
    conditions_evaluated: int
    
    def __post_init__(self):
        """Validación post-inicialización."""
        if not isinstance(self.firing_strength, (int, float)) or not (0 <= self.firing_strength <= 1):
            raise ValidationException("firing_strength debe estar entre 0 y 1")
        if self.evaluation_time_ms < 0:
            raise ValidationException("evaluation_time_ms no puede ser negativo")


@dataclass
class BatchEvaluationResult:
    """Resultado de evaluación de múltiples reglas."""
    rule_results: List[RuleEvaluationResult]
    total_evaluation_time_ms: float
    rules_processed: int
    rules_activated: int
    performance_metrics: Dict[str, Any] = field(default_factory=dict)
    
    def get_activated_consequents_by_variable(self) -> Dict[str, List[Tuple[str, float, RuleConsequent]]]:
        """Agrupa consecuentes activados por variable de salida."""
        grouped = {}
        for result in self.rule_results:
            for consequent, strength in result.activated_consequents:
                var_name = consequent.variable_name
                if var_name not in grouped:
                    grouped[var_name] = []
                grouped[var_name].append((consequent.term_name, strength, consequent))
        return grouped


class RuleEvaluationEngine:
    """Motor de evaluación de reglas fuzzy con optimizaciones y monitoreo."""
    
    def __init__(self, 
                 config: FuzzyEngineConfiguration,
                 metrics: FuzzyEngineMetrics):
        """Inicializa el motor de evaluación de reglas."""
        self.config = config
        self.metrics = metrics
        self.logger = logging.getLogger(f"{__name__}.{self.__class__.__name__}")
        
        # Cache para optimización
        self._rule_cache: Dict[str, FuzzyRule] = {}
        self._cache_lock = threading.RLock()
        
        # Estadísticas de rendimiento
        self._evaluation_stats = {
            'total_evaluations': 0,
            'total_time_ms': 0.0,
            'cache_hits': 0,
            'cache_misses': 0
        }
        
        self.logger.info("RuleEvaluationEngine inicializado correctamente")
    
    def evaluate_rules(self, 
                      rules: List[FuzzyRule],
                      fuzzification_results: Dict[str, FuzzificationResult],
                      parallel: bool = True) -> BatchEvaluationResult:
        """Evalúa múltiples reglas fuzzy."""
        start_time = time.perf_counter()
        
        try:
            # Validaciones iniciales
            self._validate_evaluation_inputs(rules, fuzzification_results)
            
            with PerformanceMonitor(self.metrics, "rule_evaluation_batch") as monitor:
                if parallel and len(rules) > self.config.performance_limits.parallel_threshold:
                    results = self._evaluate_rules_parallel(rules, fuzzification_results)
                else:
                    results = self._evaluate_rules_sequential(rules, fuzzification_results)
                
                total_time = (time.perf_counter() - start_time) * 1000
                activated_count = sum(1 for r in results if r.firing_strength > 0)
                
                # Actualizar métricas
                self.metrics.record_operation(
                    "rule_evaluation_batch",
                    total_time,
                    success=True
                )
                
                batch_result = BatchEvaluationResult(
                    rule_results=results,
                    total_evaluation_time_ms=total_time,
                    rules_processed=len(rules),
                    rules_activated=activated_count,
                    performance_metrics={
                        'avg_time_per_rule': total_time / len(rules) if rules else 0,
                        'activation_rate': activated_count / len(rules) if rules else 0,
                        'parallel_execution': parallel and len(rules) > self.config.performance_limits.parallel_threshold
                    }
                )
                
                self.logger.info(
                    f"Evaluación completada: {len(rules)} reglas, {activated_count} activadas, "
                    f"{total_time:.2f}ms total"
                )
                
                return batch_result
                
        except Exception as e:
            self.metrics.record_operation(
                "rule_evaluation_batch",
                (time.perf_counter() - start_time) * 1000,
                success=False
            )
            self.logger.error(f"Error en evaluación de reglas: {e}")
            raise RuleEvaluationException(f"Error en evaluación de reglas: {e}") from e
    
    def evaluate_single_rule(self, 
                            rule: FuzzyRule,
                            fuzzification_results: Dict[str, FuzzificationResult]) -> RuleEvaluationResult:
        """Evalúa una sola regla fuzzy."""
        start_time = time.perf_counter()
        
        try:
            # Validaciones
            if not isinstance(rule, FuzzyRule):
                raise ValidationException("rule debe ser una instancia de FuzzyRule")
            
            self._validate_fuzzification_results(fuzzification_results)
            
            with PerformanceMonitor(self.metrics, "rule_evaluation_single") as monitor:
                # Evaluar condiciones
                condition_values = self._evaluate_conditions(rule.conditions, fuzzification_results)
                
                # Calcular firing strength
                firing_strength = self._calculate_firing_strength(
                    condition_values, 
                    rule.logical_operator
                )
                
                # Activar consecuentes si hay firing strength
                activated_consequents = []
                if firing_strength > 0:
                    for consequent in rule.consequents:
                        activated_consequents.append((consequent, firing_strength))
                
                evaluation_time = (time.perf_counter() - start_time) * 1000
                
                result = RuleEvaluationResult(
                    rule_id=rule.rule_id,
                    firing_strength=firing_strength,
                    activated_consequents=activated_consequents,
                    evaluation_time_ms=evaluation_time,
                    conditions_evaluated=len(rule.conditions)
                )
                
                self.logger.debug(
                    f"Regla {rule.rule_id}: firing_strength={firing_strength:.3f}, "
                    f"tiempo={evaluation_time:.2f}ms"
                )
                
                return result
                
        except Exception as e:
            self.logger.error(f"Error evaluando regla {rule.rule_id}: {e}")
            raise RuleEvaluationException(f"Error evaluando regla {rule.rule_id}: {e}") from e
    
    def _evaluate_rules_sequential(self, 
                                  rules: List[FuzzyRule],
                                  fuzzification_results: Dict[str, FuzzificationResult]) -> List[RuleEvaluationResult]:
        """Evalúa reglas secuencialmente."""
        results = []
        for rule in rules:
            try:
                result = self.evaluate_single_rule(rule, fuzzification_results)
                results.append(result)
            except Exception as e:
                self.logger.warning(f"Error evaluando regla {rule.rule_id}: {e}")
                # Crear resultado con firing strength 0 para mantener consistencia
                results.append(RuleEvaluationResult(
                    rule_id=rule.rule_id,
                    firing_strength=0.0,
                    activated_consequents=[],
                    evaluation_time_ms=0.0,
                    conditions_evaluated=len(rule.conditions)
                ))
        return results
    
    def _evaluate_rules_parallel(self, 
                                rules: List[FuzzyRule],
                                fuzzification_results: Dict[str, FuzzificationResult]) -> List[RuleEvaluationResult]:
        """Evalúa reglas en paralelo."""
        results = [None] * len(rules)
        
        with ThreadPoolExecutor(max_workers=self.config.performance_limits.max_workers) as executor:
            # Enviar tareas
            future_to_index = {
                executor.submit(self.evaluate_single_rule, rule, fuzzification_results): i
                for i, rule in enumerate(rules)
            }
            
            # Recoger resultados
            for future in as_completed(future_to_index):
                index = future_to_index[future]
                try:
                    results[index] = future.result()
                except Exception as e:
                    rule = rules[index]
                    self.logger.warning(f"Error evaluando regla {rule.rule_id} en paralelo: {e}")
                    results[index] = RuleEvaluationResult(
                        rule_id=rule.rule_id,
                        firing_strength=0.0,
                        activated_consequents=[],
                        evaluation_time_ms=0.0,
                        conditions_evaluated=len(rule.conditions)
                    )
        
        return results
    
    def _evaluate_conditions(self, 
                           conditions: List[RuleCondition],
                           fuzzification_results: Dict[str, FuzzificationResult]) -> List[float]:
        """Evalúa las condiciones de una regla."""
        condition_values = []
        
        for condition in conditions:
            try:
                # Buscar el resultado de fuzzificación para el sensor
                if condition.sensor_id not in fuzzification_results:
                    self.logger.warning(
                        f"Sensor {condition.sensor_id} no encontrado en resultados de fuzzificación"
                    )
                    condition_values.append(0.0)
                    continue
                
                fuzz_result = fuzzification_results[condition.sensor_id]
                
                # Buscar el término específico
                term_value = None
                for var_name, terms in fuzz_result.membership_values.items():
                    if var_name == condition.variable_name:
                        if condition.term_name in terms:
                            term_value = terms[condition.term_name]
                            break
                
                if term_value is None:
                    self.logger.warning(
                        f"Término {condition.term_name} no encontrado para variable "
                        f"{condition.variable_name} del sensor {condition.sensor_id}"
                    )
                    condition_values.append(0.0)
                else:
                    condition_values.append(float(term_value))
                    
            except Exception as e:
                self.logger.error(f"Error evaluando condición: {e}")
                condition_values.append(0.0)
        
        return condition_values
    
    def _calculate_firing_strength(self, 
                                 condition_values: List[float],
                                 logical_operator: LogicalOperator) -> float:
        """Calcula el firing strength basado en los valores de condiciones."""
        if not condition_values:
            return 0.0
        
        try:
            if logical_operator == LogicalOperator.AND:
                return float(np.min(condition_values))
            elif logical_operator == LogicalOperator.OR:
                return float(np.max(condition_values))
            elif logical_operator == LogicalOperator.PRODUCT:
                return float(np.prod(condition_values))
            elif logical_operator == LogicalOperator.PROBABILISTIC_OR:
                # OR probabilístico: a + b - a*b
                result = condition_values[0]
                for val in condition_values[1:]:
                    result = result + val - (result * val)
                return float(result)
            else:
                self.logger.warning(f"Operador lógico no soportado: {logical_operator}")
                return float(np.min(condition_values))  # Default a AND
                
        except Exception as e:
            self.logger.error(f"Error calculando firing strength: {e}")
            return 0.0
    
    def _validate_evaluation_inputs(self, 
                                  rules: List[FuzzyRule],
                                  fuzzification_results: Dict[str, FuzzificationResult]):
        """Valida las entradas para evaluación de reglas."""
        if not isinstance(rules, list):
            raise ValidationException("rules debe ser una lista")
        
        if not rules:
            raise ValidationException("La lista de reglas no puede estar vacía")
        
        if len(rules) > self.config.performance_limits.max_rules_per_evaluation:
            raise PerformanceException(
                f"Demasiadas reglas para evaluar: {len(rules)} > "
                f"{self.config.performance_limits.max_rules_per_evaluation}"
            )
        
        self._validate_fuzzification_results(fuzzification_results)
        
        # Validar cada regla
        for i, rule in enumerate(rules):
            if not isinstance(rule, FuzzyRule):
                raise ValidationException(f"Regla en índice {i} no es una instancia de FuzzyRule")
    
    def _validate_fuzzification_results(self, fuzzification_results: Dict[str, FuzzificationResult]):
        """Valida los resultados de fuzzificación."""
        if not isinstance(fuzzification_results, dict):
            raise ValidationException("fuzzification_results debe ser un diccionario")
        
        if not fuzzification_results:
            raise ValidationException("fuzzification_results no puede estar vacío")
        
        for sensor_id, result in fuzzification_results.items():
            if not isinstance(result, FuzzificationResult):
                raise ValidationException(
                    f"Resultado para sensor {sensor_id} no es una instancia de FuzzificationResult"
                )
    
    def get_performance_stats(self) -> Dict[str, Any]:
        """Obtiene estadísticas de rendimiento del motor."""
        with self._cache_lock:
            stats = self._evaluation_stats.copy()
            stats.update({
                'cache_size': len(self._rule_cache),
                'cache_hit_rate': (
                    stats['cache_hits'] / (stats['cache_hits'] + stats['cache_misses'])
                    if (stats['cache_hits'] + stats['cache_misses']) > 0 else 0
                ),
                'avg_evaluation_time_ms': (
                    stats['total_time_ms'] / stats['total_evaluations']
                    if stats['total_evaluations'] > 0 else 0
                )
            })
        return stats
    
    def clear_cache(self):
        """Limpia el cache de reglas."""
        with self._cache_lock:
            self._rule_cache.clear()
            self.logger.info("Cache de reglas limpiado")
    
    def health_check(self) -> Dict[str, Any]:
        """Verifica el estado de salud del motor de evaluación."""
        try:
            stats = self.get_performance_stats()
            
            # Verificar límites de rendimiento
            avg_time = stats.get('avg_evaluation_time_ms', 0)
            max_time = self.config.performance_limits.max_evaluation_time_ms
            
            health_status = {
                'status': 'healthy',
                'avg_evaluation_time_ms': avg_time,
                'max_allowed_time_ms': max_time,
                'cache_hit_rate': stats.get('cache_hit_rate', 0),
                'total_evaluations': stats.get('total_evaluations', 0),
                'issues': []
            }
            
            if avg_time > max_time:
                health_status['status'] = 'degraded'
                health_status['issues'].append(
                    f"Tiempo promedio de evaluación ({avg_time:.2f}ms) excede el límite ({max_time}ms)"
                )
            
            cache_hit_rate = stats.get('cache_hit_rate', 0)
            if cache_hit_rate < 0.5 and stats.get('total_evaluations', 0) > 100:
                health_status['issues'].append(
                    f"Tasa de aciertos de cache baja: {cache_hit_rate:.2%}"
                )
            
            return health_status
            
        except Exception as e:
            return {
                'status': 'unhealthy',
                'error': str(e),
                'issues': [f"Error en health check: {e}"]
            }