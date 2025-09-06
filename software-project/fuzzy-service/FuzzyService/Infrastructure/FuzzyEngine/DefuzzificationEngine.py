from dataclasses import dataclass, field
from typing import Dict, List, Optional, Tuple, Any, Union, Callable
from enum import Enum
import logging
import time
import numpy as np
from scipy import integrate
import threading
from concurrent.futures import ThreadPoolExecutor, as_completed

from .FuzzyEngineExceptions import (
    DefuzzificationException,
    ValidationException,
    PerformanceException
)
from .FuzzyEngineConfiguration import (
    FuzzyEngineConfiguration,
    DefuzzificationMethod
)
from .FuzzyEngineMetrics import FuzzyEngineMetrics, PerformanceMonitor
from .AggregationEngine import AggregationResult, VariableAggregation, AggregatedTerm
from .MembershipFunctionConverter import MembershipFunctionConverter


@dataclass
class DefuzzificationInput:
    """Entrada para el proceso de defuzzificación."""
    variable_name: str
    routine_id: str
    step_number: int
    aggregated_terms: Dict[str, float]  # term_name -> aggregated_value
    universe_range: Tuple[float, float]
    resolution: int = 1000
    
    def __post_init__(self):
        """Validación post-inicialización."""
        if not self.variable_name:
            raise ValidationException("variable_name no puede estar vacío")
        if not self.aggregated_terms:
            raise ValidationException("aggregated_terms no puede estar vacío")
        if self.universe_range[0] >= self.universe_range[1]:
            raise ValidationException("universe_range debe ser (min, max) con min < max")
        if self.resolution < 100:
            raise ValidationException("resolution debe ser al menos 100")


@dataclass
class DefuzzificationResult:
    """Resultado del proceso de defuzzificación."""
    variable_name: str
    routine_id: str
    step_number: int
    crisp_value: float
    defuzzification_method: DefuzzificationMethod
    computation_time_ms: float
    universe_range: Tuple[float, float]
    aggregated_area: float
    confidence_score: float  # Basado en la calidad de la defuzzificación
    metadata: Dict[str, Any] = field(default_factory=dict)
    
    def __post_init__(self):
        """Validación post-inicialización."""
        if not isinstance(self.crisp_value, (int, float)):
            raise ValidationException("crisp_value debe ser un número")
        if not (0 <= self.confidence_score <= 1):
            raise ValidationException("confidence_score debe estar entre 0 y 1")
        if self.computation_time_ms < 0:
            raise ValidationException("computation_time_ms no puede ser negativo")


@dataclass
class BatchDefuzzificationResult:
    """Resultado de defuzzificación de múltiples variables."""
    defuzzification_results: List[DefuzzificationResult]
    total_computation_time_ms: float
    variables_processed: int
    successful_defuzzifications: int
    performance_metrics: Dict[str, Any] = field(default_factory=dict)
    
    def get_result_by_context(self, 
                             variable_name: str, 
                             routine_id: str, 
                             step_number: int) -> Optional[DefuzzificationResult]:
        """Obtiene el resultado para un contexto específico."""
        for result in self.defuzzification_results:
            if (result.variable_name == variable_name and 
                result.routine_id == routine_id and 
                result.step_number == step_number):
                return result
        return None
    
    def get_results_by_routine(self, routine_id: str) -> List[DefuzzificationResult]:
        """Obtiene todos los resultados para una rutina específica."""
        return [r for r in self.defuzzification_results if r.routine_id == routine_id]


class DefuzzificationEngine:
    """Motor de defuzzificación con múltiples métodos y optimizaciones."""
    
    def __init__(self, 
                 config: FuzzyEngineConfiguration,
                 metrics: FuzzyEngineMetrics,
                 membership_converter: MembershipFunctionConverter):
        """Inicializa el motor de defuzzificación."""
        self.config = config
        self.metrics = metrics
        self.membership_converter = membership_converter
        self.logger = logging.getLogger(f"{__name__}.{self.__class__.__name__}")
        
        # Cache para optimización
        self._universe_cache: Dict[str, np.ndarray] = {}
        self._membership_cache: Dict[str, np.ndarray] = {}
        self._cache_lock = threading.RLock()
        
        # Estadísticas de rendimiento
        self._defuzz_stats = {
            'total_defuzzifications': 0,
            'total_time_ms': 0.0,
            'method_usage': {},
            'cache_hits': 0,
            'cache_misses': 0
        }
        
        # Mapeo de métodos de defuzzificación
        self._defuzz_methods: Dict[DefuzzificationMethod, Callable] = {
            DefuzzificationMethod.CENTROID: self._centroid_defuzzification,
            DefuzzificationMethod.BISECTOR: self._bisector_defuzzification,
            DefuzzificationMethod.MOM: self._mom_defuzzification,
            DefuzzificationMethod.SOM: self._som_defuzzification,
            DefuzzificationMethod.LOM: self._lom_defuzzification
        }
        
        self.logger.info("DefuzzificationEngine inicializado correctamente")
    
    def defuzzify_aggregation_result(self, 
                                   aggregation_result: AggregationResult,
                                   membership_functions: Dict[str, Dict[str, Any]],
                                   defuzzification_method: Optional[DefuzzificationMethod] = None) -> BatchDefuzzificationResult:
        """Defuzzifica el resultado de agregación completo."""
        start_time = time.perf_counter()
        
        try:
            # Usar método por defecto si no se especifica
            if defuzzification_method is None:
                defuzzification_method = self.config.default_defuzzification_method
            
            # Validaciones iniciales
            self._validate_aggregation_result(aggregation_result)
            self._validate_membership_functions(membership_functions)
            
            with PerformanceMonitor(self.metrics, "batch_defuzzification") as monitor:
                # Preparar entradas de defuzzificación
                defuzz_inputs = self._prepare_defuzzification_inputs(
                    aggregation_result, 
                    membership_functions
                )
                
                # Procesar defuzzificaciones
                if len(defuzz_inputs) > self.config.performance_limits.parallel_threshold:
                    results = self._defuzzify_parallel(defuzz_inputs, defuzzification_method)
                else:
                    results = self._defuzzify_sequential(defuzz_inputs, defuzzification_method)
                
                total_time = (time.perf_counter() - start_time) * 1000
                successful_count = len([r for r in results if r is not None])
                
                # Filtrar resultados nulos
                valid_results = [r for r in results if r is not None]
                
                # Crear resultado final
                batch_result = BatchDefuzzificationResult(
                    defuzzification_results=valid_results,
                    total_computation_time_ms=total_time,
                    variables_processed=len(defuzz_inputs),
                    successful_defuzzifications=successful_count,
                    performance_metrics={
                        'defuzzification_method': defuzzification_method.value,
                        'avg_time_per_variable': (
                            total_time / len(defuzz_inputs) if defuzz_inputs else 0
                        ),
                        'success_rate': successful_count / len(defuzz_inputs) if defuzz_inputs else 0,
                        'parallel_execution': len(defuzz_inputs) > self.config.performance_limits.parallel_threshold
                    }
                )
                
                # Actualizar métricas
                self.metrics.record_operation(
                    "batch_defuzzification",
                    total_time,
                    success=True
                )
                
                self._update_stats(defuzzification_method, total_time, len(defuzz_inputs))
                
                self.logger.info(
                    f"Defuzzificación completada: {len(defuzz_inputs)} variables, "
                    f"{successful_count} exitosas, {total_time:.2f}ms"
                )
                
                return batch_result
                
        except Exception as e:
            self.metrics.record_operation(
                "batch_defuzzification",
                (time.perf_counter() - start_time) * 1000,
                0,
                success=False
            )
            self.logger.error(f"Error en defuzzificación batch: {e}")
            raise DefuzzificationException(f"Error en defuzzificación batch: {e}") from e
    
    def defuzzify_single_variable(self, 
                                 defuzz_input: DefuzzificationInput,
                                 membership_functions: Dict[str, Any],
                                 defuzzification_method: DefuzzificationMethod) -> Optional[DefuzzificationResult]:
        """Defuzzifica una sola variable."""
        start_time = time.perf_counter()
        
        try:
            # Validaciones
            self._validate_defuzzification_input(defuzz_input)
            
            with PerformanceMonitor(self.metrics, "single_defuzzification") as monitor:
                # Crear universo de discurso
                universe = self._get_or_create_universe(
                    defuzz_input.universe_range, 
                    defuzz_input.resolution
                )
                
                # Construir función de membresía agregada
                aggregated_membership = self._build_aggregated_membership(
                    defuzz_input, 
                    membership_functions, 
                    universe
                )
                
                # Verificar si hay área para defuzzificar
                total_area = np.trapz(aggregated_membership, universe)
                if total_area <= 1e-10:  # Área prácticamente cero
                    self.logger.warning(
                        f"Área de membresía muy pequeña para {defuzz_input.variable_name}: {total_area}"
                    )
                    return None
                
                # Aplicar método de defuzzificación
                defuzz_method = self._defuzz_methods.get(defuzzification_method)
                if not defuzz_method:
                    raise DefuzzificationException(f"Método no soportado: {defuzzification_method}")
                
                crisp_value = defuzz_method(universe, aggregated_membership)
                
                # Calcular confianza basada en la calidad de la defuzzificación
                confidence = self._calculate_confidence(universe, aggregated_membership, crisp_value)
                
                computation_time = (time.perf_counter() - start_time) * 1000
                
                result = DefuzzificationResult(
                    variable_name=defuzz_input.variable_name,
                    routine_id=defuzz_input.routine_id,
                    step_number=defuzz_input.step_number,
                    crisp_value=crisp_value,
                    defuzzification_method=defuzzification_method,
                    computation_time_ms=computation_time,
                    universe_range=defuzz_input.universe_range,
                    aggregated_area=total_area,
                    confidence_score=confidence,
                    metadata={
                        'resolution': defuzz_input.resolution,
                        'terms_count': len(defuzz_input.aggregated_terms),
                        'max_membership': float(np.max(aggregated_membership))
                    }
                )
                
                self.logger.debug(
                    f"Variable {defuzz_input.variable_name} defuzzificada: "
                    f"{crisp_value:.3f} (confianza: {confidence:.3f})"
                )
                
                return result
                
        except Exception as e:
            self.logger.error(f"Error defuzzificando {defuzz_input.variable_name}: {e}")
            return None
    
    def _prepare_defuzzification_inputs(self, 
                                       aggregation_result: AggregationResult,
                                       membership_functions: Dict[str, Dict[str, Any]]) -> List[DefuzzificationInput]:
        """Prepara las entradas para defuzzificación."""
        inputs = []
        
        for var_agg in aggregation_result.variable_aggregations:
            try:
                # Obtener funciones de membresía para esta variable
                var_functions = membership_functions.get(var_agg.variable_name, {})
                if not var_functions:
                    self.logger.warning(f"No hay funciones de membresía para {var_agg.variable_name}")
                    continue
                
                # Determinar rango del universo
                universe_range = self._determine_universe_range(var_functions)
                
                # Crear diccionario de términos agregados
                aggregated_terms = var_agg.get_all_terms_dict()
                
                defuzz_input = DefuzzificationInput(
                    variable_name=var_agg.variable_name,
                    routine_id=var_agg.routine_id,
                    step_number=var_agg.step_number,
                    aggregated_terms=aggregated_terms,
                    universe_range=universe_range,
                    resolution=self.config.performance_limits.defuzzification_resolution
                )
                
                inputs.append(defuzz_input)
                
            except Exception as e:
                self.logger.warning(f"Error preparando entrada para {var_agg.variable_name}: {e}")
                continue
        
        return inputs
    
    def _defuzzify_sequential(self, 
                            inputs: List[DefuzzificationInput],
                            method: DefuzzificationMethod) -> List[Optional[DefuzzificationResult]]:
        """Defuzzifica secuencialmente."""
        results = []
        
        for defuzz_input in inputs:
            try:
                # Obtener funciones de membresía para esta variable
                var_functions = {}  # Se debería pasar como parámetro
                result = self.defuzzify_single_variable(defuzz_input, var_functions, method)
                results.append(result)
            except Exception as e:
                self.logger.warning(f"Error en defuzzificación secuencial: {e}")
                results.append(None)
        
        return results
    
    def _defuzzify_parallel(self, 
                          inputs: List[DefuzzificationInput],
                          method: DefuzzificationMethod) -> List[Optional[DefuzzificationResult]]:
        """Defuzzifica en paralelo."""
        results = [None] * len(inputs)
        
        with ThreadPoolExecutor(max_workers=self.config.performance_limits.max_workers) as executor:
            # Enviar tareas
            future_to_index = {
                executor.submit(self.defuzzify_single_variable, inp, {}, method): i
                for i, inp in enumerate(inputs)
            }
            
            # Recoger resultados
            for future in as_completed(future_to_index):
                index = future_to_index[future]
                try:
                    results[index] = future.result()
                except Exception as e:
                    self.logger.warning(f"Error en defuzzificación paralela: {e}")
                    results[index] = None
        
        return results
    
    def _get_or_create_universe(self, 
                               universe_range: Tuple[float, float], 
                               resolution: int) -> np.ndarray:
        """Obtiene o crea un universo de discurso."""
        cache_key = f"{universe_range[0]}_{universe_range[1]}_{resolution}"
        
        with self._cache_lock:
            if cache_key in self._universe_cache:
                self._defuzz_stats['cache_hits'] += 1
                return self._universe_cache[cache_key]
            
            self._defuzz_stats['cache_misses'] += 1
            universe = np.linspace(universe_range[0], universe_range[1], resolution)
            
            # Limitar tamaño del cache
            if len(self._universe_cache) < self.config.cache.max_cache_size:
                self._universe_cache[cache_key] = universe
            
            return universe
    
    def _build_aggregated_membership(self, 
                                   defuzz_input: DefuzzificationInput,
                                   membership_functions: Dict[str, Any],
                                   universe: np.ndarray) -> np.ndarray:
        """Construye la función de membresía agregada."""
        aggregated = np.zeros_like(universe)
        
        for term_name, aggregated_value in defuzz_input.aggregated_terms.items():
            if aggregated_value <= 0:
                continue
            
            # Obtener función de membresía para este término
            term_function = membership_functions.get(term_name)
            if term_function is None:
                self.logger.warning(f"Función de membresía no encontrada para término {term_name}")
                continue
            
            try:
                # Convertir función de membresía
                membership_values = self.membership_converter.convert_to_membership_values(
                    term_function, universe
                )
                
                # Aplicar el valor agregado (clipping)
                clipped_membership = np.minimum(membership_values, aggregated_value)
                
                # Combinar con agregación MAX
                aggregated = np.maximum(aggregated, clipped_membership)
                
            except Exception as e:
                self.logger.warning(f"Error procesando término {term_name}: {e}")
                continue
        
        return aggregated
    
    def _centroid_defuzzification(self, universe: np.ndarray, membership: np.ndarray) -> float:
        """Método de defuzzificación por centroide."""
        try:
            numerator = np.trapz(universe * membership, universe)
            denominator = np.trapz(membership, universe)
            
            if denominator == 0:
                return float(np.mean(universe))  # Fallback al centro del universo
            
            return float(numerator / denominator)
        except Exception as e:
            self.logger.error(f"Error en centroide: {e}")
            return float(np.mean(universe))
    
    def _bisector_defuzzification(self, universe: np.ndarray, membership: np.ndarray) -> float:
        """Método de defuzzificación por bisector."""
        try:
            total_area = np.trapz(membership, universe)
            if total_area == 0:
                return float(np.mean(universe))
            
            target_area = total_area / 2
            cumulative_area = 0
            
            for i in range(len(universe) - 1):
                segment_area = (membership[i] + membership[i + 1]) * (universe[i + 1] - universe[i]) / 2
                cumulative_area += segment_area
                
                if cumulative_area >= target_area:
                    return float(universe[i])
            
            return float(universe[-1])
        except Exception as e:
            self.logger.error(f"Error en bisector: {e}")
            return float(np.mean(universe))
    
    def _mom_defuzzification(self, universe: np.ndarray, membership: np.ndarray) -> float:
        """Método Mean of Maximum (MOM)."""
        try:
            max_membership = np.max(membership)
            if max_membership == 0:
                return float(np.mean(universe))
            
            max_indices = np.where(membership == max_membership)[0]
            return float(np.mean(universe[max_indices]))
        except Exception as e:
            self.logger.error(f"Error en MOM: {e}")
            return float(np.mean(universe))
    
    def _som_defuzzification(self, universe: np.ndarray, membership: np.ndarray) -> float:
        """Método Smallest of Maximum (SOM)."""
        try:
            max_membership = np.max(membership)
            if max_membership == 0:
                return float(np.mean(universe))
            
            max_indices = np.where(membership == max_membership)[0]
            return float(universe[max_indices[0]])
        except Exception as e:
            self.logger.error(f"Error en SOM: {e}")
            return float(np.mean(universe))
    
    def _lom_defuzzification(self, universe: np.ndarray, membership: np.ndarray) -> float:
        """Método Largest of Maximum (LOM)."""
        try:
            max_membership = np.max(membership)
            if max_membership == 0:
                return float(np.mean(universe))
            
            max_indices = np.where(membership == max_membership)[0]
            return float(universe[max_indices[-1]])
        except Exception as e:
            self.logger.error(f"Error en LOM: {e}")
            return float(np.mean(universe))
    
    def _calculate_confidence(self, 
                            universe: np.ndarray, 
                            membership: np.ndarray, 
                            crisp_value: float) -> float:
        """Calcula la confianza de la defuzzificación."""
        try:
            # Factores que afectan la confianza:
            # 1. Área total de la función de membresía
            total_area = np.trapz(membership, universe)
            area_factor = min(1.0, total_area / (np.max(universe) - np.min(universe)))
            
            # 2. Altura máxima de la función
            max_height = np.max(membership)
            height_factor = max_height
            
            # 3. Concentración alrededor del valor crisp
            crisp_idx = np.argmin(np.abs(universe - crisp_value))
            window_size = max(1, len(universe) // 20)  # 5% del universo
            start_idx = max(0, crisp_idx - window_size)
            end_idx = min(len(universe), crisp_idx + window_size)
            
            local_area = np.trapz(membership[start_idx:end_idx], universe[start_idx:end_idx])
            concentration_factor = local_area / total_area if total_area > 0 else 0
            
            # Combinar factores
            confidence = (area_factor * 0.3 + height_factor * 0.4 + concentration_factor * 0.3)
            return min(1.0, max(0.0, confidence))
            
        except Exception as e:
            self.logger.warning(f"Error calculando confianza: {e}")
            return 0.5  # Confianza media por defecto
    
    def _determine_universe_range(self, membership_functions: Dict[str, Any]) -> Tuple[float, float]:
        """Determina el rango del universo basado en las funciones de membresía."""
        try:
            min_val = float('inf')
            max_val = float('-inf')
            
            for term_name, func_def in membership_functions.items():
                # Extraer rango de la definición de la función
                if 'parameters' in func_def:
                    params = func_def['parameters']
                    if isinstance(params, list) and len(params) >= 2:
                        min_val = min(min_val, min(params))
                        max_val = max(max_val, max(params))
            
            if min_val == float('inf') or max_val == float('-inf'):
                # Valores por defecto si no se puede determinar
                return (0.0, 100.0)
            
            # Expandir ligeramente el rango
            range_expansion = (max_val - min_val) * 0.1
            return (min_val - range_expansion, max_val + range_expansion)
            
        except Exception as e:
            self.logger.warning(f"Error determinando rango del universo: {e}")
            return (0.0, 100.0)
    
    def _validate_aggregation_result(self, aggregation_result: AggregationResult):
        """Valida el resultado de agregación."""
        if not isinstance(aggregation_result, AggregationResult):
            raise ValidationException("aggregation_result debe ser una instancia de AggregationResult")
        
        if not aggregation_result.variable_aggregations:
            raise ValidationException("No hay variables para defuzzificar")
    
    def _validate_membership_functions(self, membership_functions: Dict[str, Dict[str, Any]]):
        """Valida las funciones de membresía."""
        if not isinstance(membership_functions, dict):
            raise ValidationException("membership_functions debe ser un diccionario")
    
    def _validate_defuzzification_input(self, defuzz_input: DefuzzificationInput):
        """Valida la entrada de defuzzificación."""
        if not isinstance(defuzz_input, DefuzzificationInput):
            raise ValidationException("defuzz_input debe ser una instancia de DefuzzificationInput")
    
    def _update_stats(self, method: DefuzzificationMethod, time_ms: float, count: int):
        """Actualiza estadísticas de rendimiento."""
        with self._cache_lock:
            self._defuzz_stats['total_defuzzifications'] += count
            self._defuzz_stats['total_time_ms'] += time_ms
            
            method_name = method.value
            if method_name not in self._defuzz_stats['method_usage']:
                self._defuzz_stats['method_usage'][method_name] = 0
            self._defuzz_stats['method_usage'][method_name] += count
    
    def get_performance_stats(self) -> Dict[str, Any]:
        """Obtiene estadísticas de rendimiento."""
        with self._cache_lock:
            stats = self._defuzz_stats.copy()
            stats.update({
                'universe_cache_size': len(self._universe_cache),
                'membership_cache_size': len(self._membership_cache),
                'cache_hit_rate': (
                    stats['cache_hits'] / (stats['cache_hits'] + stats['cache_misses'])
                    if (stats['cache_hits'] + stats['cache_misses']) > 0 else 0
                ),
                'avg_defuzzification_time_ms': (
                    stats['total_time_ms'] / stats['total_defuzzifications']
                    if stats['total_defuzzifications'] > 0 else 0
                )
            })
        return stats
    
    def clear_cache(self):
        """Limpia todos los caches."""
        with self._cache_lock:
            self._universe_cache.clear()
            self._membership_cache.clear()
            self.logger.info("Caches de defuzzificación limpiados")
    
    def health_check(self) -> Dict[str, Any]:
        """Verifica el estado de salud del motor."""
        try:
            stats = self.get_performance_stats()
            
            avg_time = stats.get('avg_defuzzification_time_ms', 0)
            max_time = self.config.performance_limits.max_defuzzification_time_ms
            
            health_status = {
                'status': 'healthy',
                'avg_defuzzification_time_ms': avg_time,
                'max_allowed_time_ms': max_time,
                'cache_hit_rate': stats.get('cache_hit_rate', 0),
                'total_defuzzifications': stats.get('total_defuzzifications', 0),
                'issues': []
            }
            
            if avg_time > max_time:
                health_status['status'] = 'degraded'
                health_status['issues'].append(
                    f"Tiempo promedio de defuzzificación ({avg_time:.2f}ms) excede el límite ({max_time}ms)"
                )
            
            return health_status
            
        except Exception as e:
            return {
                'status': 'unhealthy',
                'error': str(e),
                'issues': [f"Error en health check: {e}"]
            }