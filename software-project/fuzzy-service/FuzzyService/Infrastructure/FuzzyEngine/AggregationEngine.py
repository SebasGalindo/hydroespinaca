from dataclasses import dataclass, field
from typing import Dict, List, Optional, Tuple, Any, Union
from enum import Enum
import logging
import time
import numpy as np
from collections import defaultdict
import threading
from concurrent.futures import ThreadPoolExecutor, as_completed

from .FuzzyEngineExceptions import (
    AggregationException,
    ValidationException,
    PerformanceException
)
from .FuzzyEngineConfiguration import (
    FuzzyEngineConfiguration,
    AggregationMethod
)
from .FuzzyEngineMetrics import FuzzyEngineMetrics, PerformanceMonitor
from .RuleEvaluationEngine import RuleEvaluationResult, BatchEvaluationResult, RuleConsequent


@dataclass
class AggregatedTerm:
    """Representa un término agregado con su valor final."""
    variable_name: str
    term_name: str
    aggregated_value: float
    contributing_rules: List[str]
    routine_id: str
    step_number: int
    aggregation_method: AggregationMethod
    
    def __post_init__(self):
        """Validación post-inicialización."""
        if not isinstance(self.aggregated_value, (int, float)) or not (0 <= self.aggregated_value <= 1):
            raise ValidationException("aggregated_value debe estar entre 0 y 1")
        if not self.contributing_rules:
            raise ValidationException("contributing_rules no puede estar vacío")


@dataclass
class VariableAggregation:
    """Agregación completa para una variable de salida."""
    variable_name: str
    routine_id: str
    step_number: int
    aggregated_terms: List[AggregatedTerm]
    total_rules_contributing: int
    aggregation_time_ms: float
    
    def get_term_value(self, term_name: str) -> Optional[float]:
        """Obtiene el valor agregado de un término específico."""
        for term in self.aggregated_terms:
            if term.term_name == term_name:
                return term.aggregated_value
        return None
    
    def get_all_terms_dict(self) -> Dict[str, float]:
        """Retorna todos los términos como diccionario."""
        return {term.term_name: term.aggregated_value for term in self.aggregated_terms}


@dataclass
class AggregationResult:
    """Resultado completo de la agregación."""
    variable_aggregations: List[VariableAggregation]
    total_aggregation_time_ms: float
    variables_processed: int
    terms_aggregated: int
    performance_metrics: Dict[str, Any] = field(default_factory=dict)
    
    def get_aggregation_by_variable_and_routine(self, 
                                               variable_name: str, 
                                               routine_id: str, 
                                               step_number: int) -> Optional[VariableAggregation]:
        """Obtiene la agregación para una variable, rutina y paso específicos."""
        for agg in self.variable_aggregations:
            if (agg.variable_name == variable_name and 
                agg.routine_id == routine_id and 
                agg.step_number == step_number):
                return agg
        return None
    
    def get_all_aggregations_by_routine(self, routine_id: str) -> List[VariableAggregation]:
        """Obtiene todas las agregaciones para una rutina específica."""
        return [agg for agg in self.variable_aggregations if agg.routine_id == routine_id]


class AggregationEngine:
    """Motor de agregación para combinar múltiples reglas fuzzy."""
    
    def __init__(self, 
                 config: FuzzyEngineConfiguration,
                 metrics: FuzzyEngineMetrics):
        """Inicializa el motor de agregación."""
        self.config = config
        self.metrics = metrics
        self.logger = logging.getLogger(f"{__name__}.{self.__class__.__name__}")
        
        # Cache para optimización
        self._aggregation_cache: Dict[str, AggregationResult] = {}
        self._cache_lock = threading.RLock()
        
        # Estadísticas de rendimiento
        self._aggregation_stats = {
            'total_aggregations': 0,
            'total_time_ms': 0.0,
            'cache_hits': 0,
            'cache_misses': 0,
            'memory_peak_mb': 0.0
        }
        
        self.logger.info("AggregationEngine inicializado correctamente")
    
    def aggregate_rules(self, 
                       batch_result: BatchEvaluationResult,
                       aggregation_method: Optional[AggregationMethod] = None) -> AggregationResult:
        """Agrega los resultados de múltiples reglas evaluadas."""
        start_time = time.perf_counter()
        
        try:
            # Usar método de agregación por defecto si no se especifica
            if aggregation_method is None:
                aggregation_method = self.config.default_aggregation_method
            
            # Validaciones iniciales
            self._validate_batch_result(batch_result)
            
            with PerformanceMonitor(self.metrics, "rule_aggregation") as monitor:
                # Agrupar consecuentes por variable, rutina y paso
                grouped_consequents = self._group_consequents_by_context(batch_result)
                
                # Procesar cada grupo
                variable_aggregations = []
                total_terms = 0
                
                for context_key, consequent_data in grouped_consequents.items():
                    variable_name, routine_id, step_number = context_key
                    
                    agg_start = time.perf_counter()
                    
                    # Agregar términos para esta variable/rutina/paso
                    aggregated_terms = self._aggregate_terms_for_context(
                        consequent_data, 
                        aggregation_method
                    )
                    
                    agg_time = (time.perf_counter() - agg_start) * 1000
                    
                    if aggregated_terms:  # Solo agregar si hay términos
                        variable_agg = VariableAggregation(
                            variable_name=variable_name,
                            routine_id=routine_id,
                            step_number=step_number,
                            aggregated_terms=aggregated_terms,
                            total_rules_contributing=len(set(
                                rule_id for _, _, rule_id in consequent_data
                            )),
                            aggregation_time_ms=agg_time
                        )
                        
                        variable_aggregations.append(variable_agg)
                        total_terms += len(aggregated_terms)
                
                total_time = (time.perf_counter() - start_time) * 1000
                
                # Crear resultado final
                result = AggregationResult(
                    variable_aggregations=variable_aggregations,
                    total_aggregation_time_ms=total_time,
                    variables_processed=len(variable_aggregations),
                    terms_aggregated=total_terms,
                    performance_metrics={
                        'aggregation_method': aggregation_method.value,
                        'avg_time_per_variable': (
                            total_time / len(variable_aggregations) 
                            if variable_aggregations else 0
                        ),
                        'rules_with_consequents': len([
                            r for r in batch_result.rule_results 
                            if r.activated_consequents
                        ]),
                        'total_input_rules': len(batch_result.rule_results)
                    }
                )
                
                # Actualizar métricas
                self.metrics.record_operation(
                    "rule_aggregation",
                    total_time,
                    success=True
                )
                
                self._update_stats(total_time, len(variable_aggregations))
                
                self.logger.info(
                    f"Agregación completada: {len(variable_aggregations)} variables, "
                    f"{total_terms} términos, {total_time:.2f}ms"
                )
                
                return result
                
        except Exception as e:
            self.metrics.record_operation(
                "rule_aggregation",
                (time.perf_counter() - start_time) * 1000,
                success=False
            )
            self.logger.error(f"Error en agregación de reglas: {e}")
            raise AggregationException(f"Error en agregación de reglas: {e}") from e
    
    def aggregate_single_variable(self, 
                                 consequent_data: List[Tuple[RuleConsequent, float, str]],
                                 aggregation_method: AggregationMethod) -> List[AggregatedTerm]:
        """Agrega términos para una sola variable."""
        try:
            return self._aggregate_terms_for_context(consequent_data, aggregation_method)
        except Exception as e:
            self.logger.error(f"Error agregando variable individual: {e}")
            raise AggregationException(f"Error agregando variable individual: {e}") from e
    
    def _group_consequents_by_context(self, 
                                     batch_result: BatchEvaluationResult) -> Dict[Tuple[str, str, int], List[Tuple[RuleConsequent, float, str]]]:
        """Agrupa consecuentes por variable, rutina y paso."""
        grouped = defaultdict(list)
        
        for rule_result in batch_result.rule_results:
            if rule_result.firing_strength > 0:  # Solo reglas activadas
                for consequent, strength in rule_result.activated_consequents:
                    context_key = (
                        consequent.variable_name,
                        consequent.routine_id,
                        consequent.step_number
                    )
                    grouped[context_key].append((
                        consequent, 
                        strength, 
                        rule_result.rule_id
                    ))
        
        return dict(grouped)
    
    def _aggregate_terms_for_context(self, 
                                   consequent_data: List[Tuple[RuleConsequent, float, str]],
                                   aggregation_method: AggregationMethod) -> List[AggregatedTerm]:
        """Agrega términos para un contexto específico (variable/rutina/paso)."""
        if not consequent_data:
            return []
        
        # Agrupar por término
        terms_by_name = defaultdict(list)
        for consequent, strength, rule_id in consequent_data:
            terms_by_name[consequent.term_name].append((strength, rule_id, consequent))
        
        aggregated_terms = []
        
        for term_name, term_data in terms_by_name.items():
            try:
                # Extraer valores y reglas contribuyentes
                values = [strength for strength, _, _ in term_data]
                contributing_rules = [rule_id for _, rule_id, _ in term_data]
                sample_consequent = term_data[0][2]  # Usar el primer consecuente como muestra
                
                # Aplicar método de agregación
                aggregated_value = self._apply_aggregation_method(values, aggregation_method)
                
                # Crear término agregado
                aggregated_term = AggregatedTerm(
                    variable_name=sample_consequent.variable_name,
                    term_name=term_name,
                    aggregated_value=aggregated_value,
                    contributing_rules=contributing_rules,
                    routine_id=sample_consequent.routine_id,
                    step_number=sample_consequent.step_number,
                    aggregation_method=aggregation_method
                )
                
                aggregated_terms.append(aggregated_term)
                
                self.logger.debug(
                    f"Término {term_name} agregado: {len(values)} valores -> {aggregated_value:.3f}"
                )
                
            except Exception as e:
                self.logger.warning(f"Error agregando término {term_name}: {e}")
                continue
        
        return aggregated_terms
    
    def _apply_aggregation_method(self, 
                                values: List[float], 
                                method: AggregationMethod) -> float:
        """Aplica el método de agregación especificado."""
        if not values:
            return 0.0
        
        try:
            values_array = np.array(values, dtype=np.float64)
            
            if method == AggregationMethod.MAX:
                return float(np.max(values_array))
            elif method == AggregationMethod.SUM:
                # Suma limitada a 1.0
                return min(1.0, float(np.sum(values_array)))
            elif method == AggregationMethod.AVERAGE:
                return float(np.mean(values_array))
            elif method == AggregationMethod.WEIGHTED_AVERAGE:
                # Para weighted average, usamos los valores como pesos
                # (en un caso más complejo, los pesos vendrían por separado)
                weights = values_array
                if np.sum(weights) > 0:
                    return float(np.average(values_array, weights=weights))
                else:
                    return 0.0
            elif method == AggregationMethod.PROBABILISTIC_OR:
                # OR probabilístico: 1 - ∏(1 - xi)
                complement_product = np.prod(1.0 - values_array)
                return float(1.0 - complement_product)
            else:
                self.logger.warning(f"Método de agregación no soportado: {method}")
                return float(np.max(values_array))  # Default a MAX
                
        except Exception as e:
            self.logger.error(f"Error aplicando método de agregación {method}: {e}")
            return 0.0
    
    def _validate_batch_result(self, batch_result: BatchEvaluationResult):
        """Valida el resultado de evaluación de reglas."""
        if not isinstance(batch_result, BatchEvaluationResult):
            raise ValidationException("batch_result debe ser una instancia de BatchEvaluationResult")
        
        if not batch_result.rule_results:
            raise ValidationException("batch_result no puede tener rule_results vacío")
        
        # Verificar límites de memoria
        total_consequents = sum(
            len(result.activated_consequents) 
            for result in batch_result.rule_results
        )
        
        if total_consequents > self.config.performance_limits.max_consequents_per_aggregation:
            raise PerformanceException(
                f"Demasiados consecuentes para agregar: {total_consequents} > "
                f"{self.config.performance_limits.max_consequents_per_aggregation}"
            )
    
    def _update_stats(self, execution_time_ms: float, variables_processed: int):
        """Actualiza estadísticas de rendimiento."""
        with self._cache_lock:
            self._aggregation_stats['total_aggregations'] += 1
            self._aggregation_stats['total_time_ms'] += execution_time_ms
    
    def optimize_memory_usage(self):
        """Optimiza el uso de memoria limpiando caches."""
        with self._cache_lock:
            cache_size_before = len(self._aggregation_cache)
            
            # Limpiar cache si es muy grande
            if cache_size_before > self.config.cache.max_cache_size:
                self._aggregation_cache.clear()
                self.logger.info(f"Cache de agregación limpiado: {cache_size_before} entradas removidas")
    
    def get_performance_stats(self) -> Dict[str, Any]:
        """Obtiene estadísticas de rendimiento del motor."""
        with self._cache_lock:
            stats = self._aggregation_stats.copy()
            stats.update({
                'cache_size': len(self._aggregation_cache),
                'avg_aggregation_time_ms': (
                    stats['total_time_ms'] / stats['total_aggregations']
                    if stats['total_aggregations'] > 0 else 0
                )
            })
        return stats
    
    def clear_cache(self):
        """Limpia el cache de agregación."""
        with self._cache_lock:
            self._aggregation_cache.clear()
            self.logger.info("Cache de agregación limpiado")
    
    def health_check(self) -> Dict[str, Any]:
        """Verifica el estado de salud del motor de agregación."""
        try:
            stats = self.get_performance_stats()
            
            # Verificar límites de rendimiento
            avg_time = stats.get('avg_aggregation_time_ms', 0)
            max_time = self.config.performance_limits.max_aggregation_time_ms
            
            health_status = {
                'status': 'healthy',
                'avg_aggregation_time_ms': avg_time,
                'max_allowed_time_ms': max_time,
                'total_aggregations': stats.get('total_aggregations', 0),
                'cache_size': stats.get('cache_size', 0),
                'issues': []
            }
            
            if avg_time > max_time:
                health_status['status'] = 'degraded'
                health_status['issues'].append(
                    f"Tiempo promedio de agregación ({avg_time:.2f}ms) excede el límite ({max_time}ms)"
                )
            
            cache_size = stats.get('cache_size', 0)
            if cache_size > self.config.cache.max_cache_size * 0.9:
                health_status['issues'].append(
                    f"Cache de agregación cerca del límite: {cache_size}/{self.config.cache.max_cache_size}"
                )
            
            return health_status
            
        except Exception as e:
            return {
                'status': 'unhealthy',
                'error': str(e),
                'issues': [f"Error en health check: {e}"]
            }
    
    def get_aggregation_summary(self, result: AggregationResult) -> Dict[str, Any]:
        """Genera un resumen de la agregación para logging/debugging."""
        try:
            summary = {
                'total_variables': result.variables_processed,
                'total_terms': result.terms_aggregated,
                'total_time_ms': result.total_aggregation_time_ms,
                'variables_by_routine': defaultdict(int),
                'terms_by_variable': defaultdict(int),
                'aggregation_methods_used': set()
            }
            
            for var_agg in result.variable_aggregations:
                summary['variables_by_routine'][var_agg.routine_id] += 1
                summary['terms_by_variable'][var_agg.variable_name] += len(var_agg.aggregated_terms)
                
                for term in var_agg.aggregated_terms:
                    summary['aggregation_methods_used'].add(term.aggregation_method.value)
            
            # Convertir defaultdict y set a tipos serializables
            summary['variables_by_routine'] = dict(summary['variables_by_routine'])
            summary['terms_by_variable'] = dict(summary['terms_by_variable'])
            summary['aggregation_methods_used'] = list(summary['aggregation_methods_used'])
            
            return summary
            
        except Exception as e:
            self.logger.error(f"Error generando resumen de agregación: {e}")
            return {'error': str(e)}