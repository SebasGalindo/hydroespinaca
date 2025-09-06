from dataclasses import dataclass, field
from typing import Dict, List, Optional, Tuple, Any, Union, Callable
from enum import Enum
import logging
import time
import asyncio
import threading
from contextlib import asynccontextmanager
import uuid
from datetime import datetime, timezone

from .FuzzyEngineExceptions import (
    FuzzyEngineException,
    ValidationException,
    PerformanceException,
    FuzzificationException,
    RuleEvaluationException,
    AggregationException,
    DefuzzificationException
)
from .FuzzyEngineConfiguration import FuzzyEngineConfiguration
from .FuzzyEngineMetrics import FuzzyEngineMetrics, PerformanceMonitor
from .MembershipFunctionConverter import MembershipFunctionConverter
from .FuzzificationEngine import FuzzificationEngine, FuzzificationResult
from .RuleEvaluationEngine import (
    RuleEvaluationEngine, 
    InfraFuzzyRule as FuzzyRule, 
    RuleConsequent,
    BatchEvaluationResult
)
from FuzzyService.Domain.ValueObjects.RuleCondition import RuleCondition
from .AggregationEngine import AggregationEngine, AggregationResult
from .DefuzzificationEngine import (
    DefuzzificationEngine, 
    BatchDefuzzificationResult,
    DefuzzificationResult
)
from .AsyncTaskQueue import (
    AsyncTaskManager, 
    TaskPriority, 
    TaskResult, 
    TaskStatus
)
from .AsyncLoadBalancer import (
    AsyncTaskManagerWithLoadBalancer,
    LoadBalancerConfig,
    LoadBalancingStrategy
)
from .ConcurrencyOptimizer import ConcurrencyOptimizer, OptimizationStrategy
from .ResourcePool import ThreadPoolManager


class FuzzyEngineState(Enum):
    """Estados del motor fuzzy."""
    INITIALIZING = "initializing"
    READY = "ready"
    PROCESSING = "processing"
    ERROR = "error"
    SHUTDOWN = "shutdown"


@dataclass
class FuzzyEvaluationRequest:
    """Solicitud de evaluación fuzzy."""
    request_id: str
    system_id: str
    sensor_data: Dict[str, float]  # sensor_id -> value
    timestamp: datetime
    rules: List[FuzzyRule]
    membership_functions: Dict[str, Dict[str, Any]]  # variable_name -> {term_name -> function_def}
    context: Dict[str, Any] = field(default_factory=dict)
    
    def __post_init__(self):
        """Validación post-inicialización."""
        if not self.request_id:
            self.request_id = str(uuid.uuid4())
        if not self.system_id:
            raise ValidationException("system_id es requerido")
        if not self.sensor_data:
            raise ValidationException("sensor_data no puede estar vacío")
        if not self.rules:
            raise ValidationException("rules no puede estar vacío")
        if not self.membership_functions:
            raise ValidationException("membership_functions no puede estar vacío")


@dataclass
class FuzzyEvaluationResponse:
    """Respuesta de evaluación fuzzy."""
    request_id: str
    system_id: str
    success: bool
    crisp_outputs: Dict[str, Dict[str, Dict[int, float]]]  # routine_id -> variable_name -> step_number -> value
    processing_time_ms: float
    timestamp: datetime
    confidence_scores: Dict[str, Dict[str, Dict[int, float]]] = field(default_factory=dict)
    performance_metrics: Dict[str, Any] = field(default_factory=dict)
    error_details: Optional[str] = None
    warnings: List[str] = field(default_factory=list)
    
    def get_output_value(self, routine_id: str, variable_name: str, step_number: int) -> Optional[float]:
        """Obtiene un valor de salida específico."""
        try:
            return self.crisp_outputs[routine_id][variable_name][step_number]
        except KeyError:
            return None
    
    def get_confidence_score(self, routine_id: str, variable_name: str, step_number: int) -> Optional[float]:
        """Obtiene el score de confianza para un valor específico."""
        try:
            return self.confidence_scores[routine_id][variable_name][step_number]
        except KeyError:
            return None


class ScikitFuzzyEngine:
    """Motor principal de lógica fuzzy con orquestación completa."""
    
    def __init__(self, config: FuzzyEngineConfiguration):
        """Inicializa el motor fuzzy principal."""
        self.config = config
        self.logger = logging.getLogger(f"{__name__}.{self.__class__.__name__}")
        
        # Estado del motor
        self._state = FuzzyEngineState.INITIALIZING
        self._state_lock = threading.RLock()
        
        # Métricas y monitoreo
        self.metrics = FuzzyEngineMetrics(config)
        
        # Componentes del motor
        self.membership_converter = None
        self.fuzzification_engine = None
        self.rule_evaluation_engine = None
        self.aggregation_engine = None
        self.defuzzification_engine = None
        
        # Estadísticas de procesamiento
        self._processing_stats = {
            'total_requests': 0,
            'successful_requests': 0,
            'failed_requests': 0,
            'total_processing_time_ms': 0.0,
            'last_request_time': None,
            'current_concurrent_requests': 0
        }
        self._stats_lock = threading.RLock()
        
        # Configurar async task manager con load balancer
        self.async_task_manager: Optional[AsyncTaskManagerWithLoadBalancer] = None
        self._async_enabled = config.performance_limits.enable_async_processing
        
        # Configurar optimizador de concurrencia
        self.concurrency_optimizer: Optional[ConcurrencyOptimizer] = None
        
        # Configurar gestor de pools de recursos
        self.thread_pool_manager: Optional[ThreadPoolManager] = None
        
        if self._async_enabled:
            load_balancer_config = LoadBalancerConfig(
                strategy=LoadBalancingStrategy.ADAPTIVE,
                health_check_interval_seconds=30.0,
                load_update_interval_seconds=5.0
            )
            self.async_task_manager = AsyncTaskManagerWithLoadBalancer(
                config=config,
                load_balancer_config=load_balancer_config,
                metrics=self.metrics,
                logger=self.logger.getChild("AsyncTaskManager")
            )
            
            # Inicializar optimizador de concurrencia
            self.concurrency_optimizer = ConcurrencyOptimizer(
                config=config,
                metrics=self.metrics,
                task_manager=self.async_task_manager.task_manager if self.async_task_manager else None,
                load_balancer=self.async_task_manager.load_balancer if self.async_task_manager else None,
                logger=self.logger.getChild("ConcurrencyOptimizer")
            )
            
            # Inicializar gestor de thread pools
            self.thread_pool_manager = ThreadPoolManager(
                config=config,
                metrics=self.metrics,
                logger=self.logger.getChild("ThreadPoolManager")
            )
        
        # Inicializar componentes
        self._initialize_components()
        
        self.logger.info("ScikitFuzzyEngine inicializado correctamente")
    
    def _initialize_components(self):
        """Inicializa todos los componentes del motor."""
        try:
            with self._state_lock:
                self._state = FuzzyEngineState.INITIALIZING
                
                # Inicializar componentes en orden de dependencia
                self.membership_converter = MembershipFunctionConverter(
                    self.config, self.metrics
                )
                
                self.fuzzification_engine = FuzzificationEngine(
                    self.config, self.metrics, self.membership_converter
                )
                
                self.rule_evaluation_engine = RuleEvaluationEngine(
                    self.config, self.metrics
                )
                
                self.aggregation_engine = AggregationEngine(
                    self.config, self.metrics
                )
                
                self.defuzzification_engine = DefuzzificationEngine(
                    self.config, self.metrics, self.membership_converter
                )
                
                self._state = FuzzyEngineState.READY
                
                self.logger.info("Todos los componentes del motor fuzzy inicializados")
                
        except Exception as e:
            with self._state_lock:
                self._state = FuzzyEngineState.ERROR
            self.logger.error(f"Error inicializando componentes: {e}")
            raise FuzzyEngineException(f"Error inicializando motor fuzzy: {e}") from e
    
    async def evaluate_fuzzy_logic(self, request: FuzzyEvaluationRequest) -> FuzzyEvaluationResponse:
        """Evalúa la lógica fuzzy de forma asíncrona."""
        # Verificar estado del motor
        if not self._is_ready():
            raise FuzzyEngineException(f"Motor fuzzy no está listo. Estado actual: {self._state.value}")
        
        # Si async task manager está habilitado, usar el sistema de colas
        if self.async_task_manager and self._async_enabled:
            return await self._evaluate_with_task_queue(request)
        else:
            return await self._evaluate_direct_async(request)
    
    async def _evaluate_with_task_queue(self, request: FuzzyEvaluationRequest) -> FuzzyEvaluationResponse:
        """Evalúa usando el sistema de colas asíncronas."""
        start_time = time.perf_counter()
        request_id = request.request_id
        
        try:
            self.logger.info(f"Enviando request {request_id} al task queue")
            
            # Crear tarea para el pipeline fuzzy
            task_result = await self.async_task_manager.submit_task(
                task_func=self._execute_fuzzy_pipeline,
                args=(request,),
                priority=TaskPriority.HIGH,
                timeout_seconds=getattr(self.config.performance_limits, 'max_processing_time_seconds', 30)
            )
            
            if task_result.status == TaskStatus.COMPLETED:
                response = task_result.result
                self.logger.info(
                    f"Task queue evaluación completada para request {request_id} "
                    f"en {task_result.execution_time_seconds * 1000:.2f}ms"
                )
                return response
            else:
                error_msg = f"Task failed: {task_result.error_message}"
                self.logger.error(f"Error en task queue para request {request_id}: {error_msg}")
                
                return FuzzyEvaluationResponse(
                    request_id=request_id,
                    system_id=request.system_id,
                    success=False,
                    crisp_outputs={},
                    processing_time_ms=(time.perf_counter() - start_time) * 1000,
                    timestamp=datetime.now(timezone.utc),
                    error_details=error_msg
                )
                
        except Exception as e:
            self.logger.error(f"Error en task queue para request {request_id}: {str(e)}")
            return FuzzyEvaluationResponse(
                request_id=request_id,
                system_id=request.system_id,
                success=False,
                crisp_outputs={},
                processing_time_ms=(time.perf_counter() - start_time) * 1000,
                timestamp=datetime.now(timezone.utc),
                error_details=str(e)
            )
    
    async def _evaluate_direct_async(self, request: FuzzyEvaluationRequest) -> FuzzyEvaluationResponse:
        """Evalúa directamente de forma asíncrona (método original)."""
        start_time = time.perf_counter()
        
        # Incrementar contador de requests concurrentes
        with self._stats_lock:
            self._processing_stats['current_concurrent_requests'] += 1
            self._processing_stats['total_requests'] += 1
            self._processing_stats['last_request_time'] = datetime.now(timezone.utc)
        
        try:
            with self._state_lock:
                self._state = FuzzyEngineState.PROCESSING
            
            # Validar request
            self._validate_evaluation_request(request)
            
            with PerformanceMonitor(self.metrics, "fuzzy_evaluation_complete") as monitor:
                # Ejecutar pipeline de evaluación fuzzy
                response = await self._execute_fuzzy_pipeline(request)
                
                processing_time = (time.perf_counter() - start_time) * 1000
                response.processing_time_ms = processing_time
                
                # Actualizar estadísticas
                with self._stats_lock:
                    self._processing_stats['successful_requests'] += 1
                    self._processing_stats['total_processing_time_ms'] += processing_time
                
                # Registrar métricas
                self.metrics.record_operation(
                    "fuzzy_evaluation_complete",
                    processing_time,
                    success=True
                )
                
                self.logger.info(
                    f"Evaluación fuzzy completada para request {request.request_id}: "
                    f"{processing_time:.2f}ms"
                )
                
                return response
                
        except Exception as e:
            processing_time = (time.perf_counter() - start_time) * 1000
            
            # Actualizar estadísticas de error
            with self._stats_lock:
                self._processing_stats['failed_requests'] += 1
            
            # Registrar métricas de error
            self.metrics.record_operation(
                "fuzzy_evaluation_complete",
                processing_time,
                success=False
            )
            
            self.logger.error(f"Error en evaluación fuzzy para request {request.request_id}: {e}")
            
            # Crear respuesta de error
            error_response = FuzzyEvaluationResponse(
                request_id=request.request_id,
                system_id=request.system_id,
                success=False,
                crisp_outputs={},
                processing_time_ms=processing_time,
                timestamp=datetime.now(timezone.utc),
                error_details=str(e)
            )
            
            return error_response
            
        finally:
            # Decrementar contador de requests concurrentes
            with self._stats_lock:
                self._processing_stats['current_concurrent_requests'] -= 1
            
            with self._state_lock:
                if self._state == FuzzyEngineState.PROCESSING:
                    self._state = FuzzyEngineState.READY
    
    def evaluate_fuzzy_logic_sync(self, request: FuzzyEvaluationRequest) -> FuzzyEvaluationResponse:
        """Versión síncrona de la evaluación fuzzy."""
        try:
            # Crear un nuevo loop de eventos si no existe
            loop = asyncio.new_event_loop()
            asyncio.set_event_loop(loop)
            try:
                return loop.run_until_complete(self.evaluate_fuzzy_logic(request))
            finally:
                loop.close()
        except Exception as e:
            self.logger.error(f"Error en evaluación fuzzy síncrona: {e}")
            raise
    
    async def _execute_fuzzy_pipeline(self, request: FuzzyEvaluationRequest) -> FuzzyEvaluationResponse:
        """Ejecuta el pipeline completo de evaluación fuzzy."""
        warnings = []
        performance_metrics = {}
        
        try:
            # 1. Fuzzificación
            self.logger.debug(f"Iniciando fuzzificación para request {request.request_id}")
            fuzzification_start = time.perf_counter()
            
            # Convert membership_functions to fuzzy_variables format
            fuzzy_variables = self._convert_membership_functions_to_variables(request.membership_functions)
            
            fuzzification_result = await self.execute_with_optimized_resources(
                self.fuzzification_engine.fuzzify_sensor_readings,
                request.sensor_data,
                fuzzy_variables,
                request.system_id,
                use_thread_pool=True,
                timeout_seconds=10.0
            )
            
            fuzzification_time = (time.perf_counter() - fuzzification_start) * 1000
            performance_metrics['fuzzification_time_ms'] = fuzzification_time
            
            # 2. Evaluación de reglas
            self.logger.debug(f"Iniciando evaluación de reglas para request {request.request_id}")
            rule_eval_start = time.perf_counter()
            
            rule_evaluation_result = await self.execute_with_optimized_resources(
                self.rule_evaluation_engine.evaluate_rules,
                request.rules,
                fuzzification_result,
                parallel=len(request.rules) > self.config.performance_limits.parallel_threshold,
                use_thread_pool=True,
                timeout_seconds=30.0
            )
            
            rule_eval_time = (time.perf_counter() - rule_eval_start) * 1000
            performance_metrics['rule_evaluation_time_ms'] = rule_eval_time
            
            # 3. Agregación
            self.logger.debug(f"Iniciando agregación para request {request.request_id}")
            aggregation_start = time.perf_counter()
            
            aggregation_result = await self.execute_with_optimized_resources(
                self.aggregation_engine.aggregate_rules,
                rule_evaluation_result,
                use_thread_pool=True,
                timeout_seconds=15.0
            )
            
            aggregation_time = (time.perf_counter() - aggregation_start) * 1000
            performance_metrics['aggregation_time_ms'] = aggregation_time
            
            # 4. Defuzzificación
            self.logger.debug(f"Iniciando defuzzificación para request {request.request_id}")
            defuzz_start = time.perf_counter()
            
            defuzzification_result = await self.execute_with_optimized_resources(
                self.defuzzification_engine.defuzzify_aggregation_result,
                aggregation_result,
                request.membership_functions,
                use_thread_pool=True,
                timeout_seconds=20.0
            )
            
            defuzz_time = (time.perf_counter() - defuzz_start) * 1000
            performance_metrics['defuzzification_time_ms'] = defuzz_time
            
            # 5. Construir respuesta
            crisp_outputs, confidence_scores = self._build_output_structure(defuzzification_result)
            
            # Agregar métricas adicionales
            performance_metrics.update({
                'total_sensors_processed': len(request.sensor_data),
                'total_rules_evaluated': len(request.rules),
                'rules_activated': rule_evaluation_result.rules_activated,
                'variables_defuzzified': defuzzification_result.variables_processed,
                'successful_defuzzifications': defuzzification_result.successful_defuzzifications
            })
            
            # Verificar calidad de resultados
            quality_warnings = self._assess_result_quality(
                fuzzification_result,
                rule_evaluation_result,
                aggregation_result,
                defuzzification_result
            )
            warnings.extend(quality_warnings)
            
            response = FuzzyEvaluationResponse(
                request_id=request.request_id,
                system_id=request.system_id,
                success=True,
                crisp_outputs=crisp_outputs,
                processing_time_ms=0,  # Se establecerá en el método principal
                timestamp=datetime.now(timezone.utc),
                confidence_scores=confidence_scores,
                performance_metrics=performance_metrics,
                warnings=warnings
            )
            
            return response
            
        except Exception as e:
            self.logger.error(f"Error en pipeline fuzzy: {e}")
            raise FuzzyEngineException(f"Error en pipeline de evaluación fuzzy: {e}") from e
    
    def _build_output_structure(self, 
                               defuzz_result: BatchDefuzzificationResult) -> Tuple[Dict[str, Dict[str, Dict[int, float]]], Dict[str, Dict[str, Dict[int, float]]]]:
        """Construye la estructura de salida organizada por rutina, variable y paso."""
        crisp_outputs = {}
        confidence_scores = {}
        
        for result in defuzz_result.defuzzification_results:
            routine_id = result.routine_id
            variable_name = result.variable_name
            step_number = result.step_number
            
            # Inicializar estructuras anidadas si no existen
            if routine_id not in crisp_outputs:
                crisp_outputs[routine_id] = {}
                confidence_scores[routine_id] = {}
            
            if variable_name not in crisp_outputs[routine_id]:
                crisp_outputs[routine_id][variable_name] = {}
                confidence_scores[routine_id][variable_name] = {}
            
            # Asignar valores
            crisp_outputs[routine_id][variable_name][step_number] = result.crisp_value
            confidence_scores[routine_id][variable_name][step_number] = result.confidence_score
        
        return crisp_outputs, confidence_scores
    
    def _assess_result_quality(self, 
                              fuzz_result: Dict[str, FuzzificationResult],
                              rule_result: BatchEvaluationResult,
                              agg_result: AggregationResult,
                              defuzz_result: BatchDefuzzificationResult) -> List[str]:
        """Evalúa la calidad de los resultados y genera advertencias."""
        warnings = []
        
        try:
            # Verificar tasa de activación de reglas
            activation_rate = rule_result.rules_activated / rule_result.rules_processed if rule_result.rules_processed > 0 else 0
            if activation_rate < 0.1:  # Menos del 10% de reglas activadas
                warnings.append(f"Baja tasa de activación de reglas: {activation_rate:.1%}")
            
            # Verificar éxito en defuzzificación
            defuzz_success_rate = defuzz_result.successful_defuzzifications / defuzz_result.variables_processed if defuzz_result.variables_processed > 0 else 0
            if defuzz_success_rate < 0.9:  # Menos del 90% de éxito
                warnings.append(f"Baja tasa de éxito en defuzzificación: {defuzz_success_rate:.1%}")
            
            # Verificar confianza promedio
            confidence_scores = [r.confidence_score for r in defuzz_result.defuzzification_results]
            if confidence_scores:
                avg_confidence = sum(confidence_scores) / len(confidence_scores)
                if avg_confidence < 0.5:
                    warnings.append(f"Baja confianza promedio en resultados: {avg_confidence:.2f}")
            
            # Verificar tiempos de procesamiento
            total_time = defuzz_result.total_computation_time_ms
            if total_time > self.config.performance_limits.max_total_processing_time_ms:
                warnings.append(f"Tiempo de procesamiento elevado: {total_time:.2f}ms")
            
        except Exception as e:
            self.logger.warning(f"Error evaluando calidad de resultados: {e}")
            warnings.append("No se pudo evaluar completamente la calidad de los resultados")
        
        return warnings
    
    def _convert_membership_functions_to_variables(self, membership_functions: Dict[str, Dict[str, Any]]) -> List[Dict[str, Any]]:
        """Convert membership_functions format to fuzzy_variables format.
        
        Args:
            membership_functions: Dict[variable_name -> {term_name -> function_def}]
            
        Returns:
            List of fuzzy variable definitions
        """
        fuzzy_variables = []
        
        for variable_name, terms in membership_functions.items():
            # Create variable definition
            variable_def = {
                'id': variable_name,
                'name': variable_name,
                'min_value': 0.0,  # Default range, should be configured properly
                'max_value': 100.0,
                'sensor_id': variable_name,  # Assume variable name maps to sensor
                'terms': []
            }
            
            # Convert terms
            for term_name, function_def in terms.items():
                term_def = {
                    'id': term_name,
                    'name': term_name,
                    'function_type': function_def.get('type', 'triangular'),
                    'parameters': function_def.get('params', {})
                }
                variable_def['terms'].append(term_def)
            
            fuzzy_variables.append(variable_def)
        
        return fuzzy_variables

    def _validate_evaluation_request(self, request: FuzzyEvaluationRequest):
        """Valida la solicitud de evaluación."""
        if not isinstance(request, FuzzyEvaluationRequest):
            raise ValidationException("request debe ser una instancia de FuzzyEvaluationRequest")
        
        # Verificar límites de rendimiento
        if len(request.sensor_data) > self.config.performance_limits.max_sensors_per_evaluation:
            raise PerformanceException(
                f"Demasiados sensores: {len(request.sensor_data)} > {self.config.performance_limits.max_sensors_per_evaluation}"
            )
        
        if len(request.rules) > self.config.performance_limits.max_rules_per_evaluation:
            raise PerformanceException(
                f"Demasiadas reglas: {len(request.rules)} > {self.config.performance_limits.max_rules_per_evaluation}"
            )
        
        # Verificar concurrencia
        with self._stats_lock:
            current_concurrent = self._processing_stats['current_concurrent_requests']
            if current_concurrent >= self.config.performance_limits.max_concurrent_evaluations:
                raise PerformanceException(
                    f"Demasiadas evaluaciones concurrentes: {current_concurrent} >= {self.config.performance_limits.max_concurrent_evaluations}"
                )
    
    def _is_ready(self) -> bool:
        """Verifica si el motor está listo para procesar."""
        with self._state_lock:
            return self._state in [FuzzyEngineState.READY, FuzzyEngineState.PROCESSING]
    
    def get_engine_status(self) -> Dict[str, Any]:
        """Obtiene el estado completo del motor."""
        with self._state_lock, self._stats_lock:
            status = {
                'state': self._state.value,
                'is_ready': self._is_ready(),
                'processing_stats': self._processing_stats.copy(),
                'performance_stats': {},
                'component_health': {},
                'async_processing': {},
                'configuration': {
                    'defuzzification_method': self.config.default_defuzzification_method.value,
                    'aggregation_method': self.config.default_aggregation_method.value,
                    'max_concurrent_evaluations': self.config.performance_limits.max_concurrent_evaluations,
                    'async_processing_enabled': self._async_enabled
                }
            }
            
            # Agregar información del async task manager
            if self.async_task_manager and self._async_enabled:
                try:
                    worker_count = len(self.async_task_manager.task_manager.workers)
                    active_workers = len([w for w in self.async_task_manager.task_manager.workers if w._current_task is not None])
                    status['async_processing'] = {
                        'enabled': True,
                        'worker_count': worker_count,
                        'active_workers': active_workers,
                        'queue_size': self.async_task_manager.task_manager.task_queue.qsize(),
                        'load_balancer': {
                            'strategy': self.async_task_manager.load_balancer.config.strategy.value,
                            'total_workers': worker_count
                        }
                    }
                except Exception as e:
                    status['async_processing'] = {'enabled': True, 'error': str(e)}
            else:
                status['async_processing'] = {'enabled': False}
            
            # Agregar estadísticas de componentes si están disponibles
            try:
                if self.fuzzification_engine:
                    status['component_health']['fuzzification'] = self.fuzzification_engine.health_check()
                if self.rule_evaluation_engine:
                    status['component_health']['rule_evaluation'] = self.rule_evaluation_engine.health_check()
                if self.aggregation_engine:
                    status['component_health']['aggregation'] = self.aggregation_engine.health_check()
                if self.defuzzification_engine:
                    status['component_health']['defuzzification'] = self.defuzzification_engine.health_check()
                
                status['performance_stats'] = self.metrics.get_performance_summary()
                
            except Exception as e:
                self.logger.warning(f"Error obteniendo estado de componentes: {e}")
                status['component_health']['error'] = str(e)
            
            return status
    
    def health_check(self) -> Dict[str, Any]:
        """Verifica el estado de salud completo del motor."""
        try:
            overall_health = {
                'status': 'healthy',
                'timestamp': datetime.now(timezone.utc).isoformat(),
                'engine_state': self._state.value,
                'components': {},
                'checks': {},
                'performance': {},
                'issues': []
            }
            
            # Verificar estado del motor
            if not self._is_ready():
                overall_health['status'] = 'unhealthy'
                overall_health['issues'].append(f"Motor no está listo: {self._state.value}")
            
            # Verificar salud de componentes
            components_to_check = [
                ('fuzzification', self.fuzzification_engine),
                ('rule_evaluation', self.rule_evaluation_engine),
                ('aggregation', self.aggregation_engine),
                ('defuzzification', self.defuzzification_engine)
            ]
            
            for component_name, component in components_to_check:
                if component:
                    try:
                        component_health = component.health_check()
                        overall_health['components'][component_name] = component_health
                        
                        if component_health.get('status') != 'healthy':
                            overall_health['status'] = 'degraded'
                            issues = component_health.get('issues', [])
                            overall_health['issues'].extend([f"{component_name}: {issue}" for issue in issues])
                    except Exception as e:
                        overall_health['components'][component_name] = {'status': 'error', 'error': str(e)}
                        overall_health['status'] = 'degraded'
                        overall_health['issues'].append(f"{component_name}: Error en health check - {e}")
            
            # Verificar salud del async task manager
            if self.async_task_manager and self._async_enabled:
                try:
                    worker_count = len(self.async_task_manager.task_manager.workers)
                    active_workers = len([w for w in self.async_task_manager.task_manager.workers if w._current_task is not None])
                    queue_size = self.async_task_manager.task_manager.task_queue.qsize()
                    
                    async_healthy = worker_count > 0 and queue_size < 1000  # Threshold for queue size
                    async_check = {
                        'status': 'healthy' if async_healthy else 'degraded',
                        'message': 'Async processing operational' if async_healthy else 'Async processing degraded',
                        'metrics': {
                            'worker_count': worker_count,
                            'active_workers': active_workers,
                            'queue_size': queue_size,
                            'total_workers': worker_count,
                            'load_balancer_strategy': self.async_task_manager.load_balancer.config.strategy.value
                        }
                    }
                    overall_health['checks']['async_processing'] = async_check
                    
                    if not async_healthy:
                        if worker_count == 0:
                            overall_health['issues'].append("Async processing: No hay workers disponibles")
                        if queue_size >= 1000:
                            overall_health['issues'].append(f"Async processing: Cola de tareas muy grande ({queue_size})")
                        overall_health['status'] = 'degraded'
                        
                except Exception as e:
                    overall_health['checks']['async_processing'] = {
                        'status': 'error', 
                        'message': f'Error en health check: {e}',
                        'metrics': {}
                    }
                    overall_health['status'] = 'degraded'
                    overall_health['issues'].append(f"Async processing: Error en health check - {e}")
            else:
                overall_health['checks']['async_processing'] = {
                    'status': 'disabled', 
                    'message': 'Async processing disabled',
                    'metrics': {'enabled': False}
                }
            
            # Verificar métricas de rendimiento
            try:
                perf_summary = self.metrics.get_performance_summary()
                overall_health['performance'] = perf_summary
                
                # Verificar alertas de rendimiento
                if perf_summary.get('alerts'):
                    overall_health['status'] = 'degraded'
                    overall_health['issues'].extend(perf_summary['alerts'])
                    
            except Exception as e:
                overall_health['issues'].append(f"Error obteniendo métricas de rendimiento: {e}")
            
            return overall_health
            
        except Exception as e:
            return {
                'status': 'unhealthy',
                'timestamp': datetime.now(timezone.utc).isoformat(),
                'error': str(e),
                'issues': [f"Error crítico en health check: {e}"]
            }
    
    def optimize_performance(self):
        """Optimiza el rendimiento del motor."""
        try:
            self.logger.info("Iniciando optimización de rendimiento")
            
            # Limpiar caches de componentes
            if self.membership_converter:
                self.membership_converter.clear_cache()
            if self.fuzzification_engine:
                self.fuzzification_engine.clear_cache()
            if self.rule_evaluation_engine:
                self.rule_evaluation_engine.clear_cache()
            if self.aggregation_engine:
                self.aggregation_engine.clear_cache()
            if self.defuzzification_engine:
                self.defuzzification_engine.clear_cache()
            
            # Optimizar memoria en agregación
            if self.aggregation_engine:
                self.aggregation_engine.optimize_memory_usage()
            
            # Limpiar métricas antiguas
            self.metrics.cleanup_old_metrics()
            
            self.logger.info("Optimización de rendimiento completada")
            
        except Exception as e:
            self.logger.error(f"Error en optimización de rendimiento: {e}")
            raise FuzzyEngineException(f"Error optimizando rendimiento: {e}") from e
    
    async def evaluate_batch_async(self, requests: List[FuzzyEvaluationRequest]) -> List[FuzzyEvaluationResponse]:
        """Evalúa múltiples requests de forma asíncrona con balanceeo de carga."""
        if not self._is_ready():
            raise FuzzyEngineException("Motor fuzzy no está listo")
        
        if not requests:
            return []
        
        self.logger.info(f"Iniciando evaluación batch de {len(requests)} requests")
        
        # Si async task manager está habilitado, usar procesamiento paralelo optimizado
        if self.async_task_manager and self._async_enabled:
            return await self._evaluate_batch_with_load_balancer(requests)
        else:
            # Procesamiento paralelo básico
            tasks = [self._evaluate_direct_async(request) for request in requests]
            return await asyncio.gather(*tasks, return_exceptions=False)
    
    async def _evaluate_batch_with_load_balancer(self, requests: List[FuzzyEvaluationRequest]) -> List[FuzzyEvaluationResponse]:
        """Evalúa batch usando load balancer para distribución óptima."""
        start_time = time.time()
        
        try:
            # Enviar todas las tareas al task manager
            task_futures = []
            for request in requests:
                future = self.async_task_manager.submit_task(
                    task_func=self._execute_fuzzy_pipeline,
                    args=(request,),
                    priority=TaskPriority.NORMAL,
                    timeout_seconds=getattr(self.config.performance_limits, 'max_processing_time_seconds', 30)
                )
                task_futures.append((request, future))
            
            # Esperar resultados
            responses = []
            for request, future in task_futures:
                try:
                    task_result = await future
                    if task_result.status == TaskStatus.COMPLETED:
                        responses.append(task_result.result)
                    else:
                        # Crear respuesta de error
                        error_response = FuzzyEvaluationResponse(
                            request_id=request.request_id,
                            system_id=request.system_id,
                            success=False,
                            crisp_outputs={},
                            processing_time_ms=(time.time() - start_time) * 1000,
                            timestamp=datetime.now(timezone.utc),
                            error_details=task_result.error_message
                        )
                        responses.append(error_response)
                except Exception as e:
                    # Crear respuesta de error para excepciones
                    error_response = FuzzyEvaluationResponse(
                        request_id=request.request_id,
                        system_id=request.system_id,
                        success=False,
                        crisp_outputs={},
                        processing_time_ms=(time.time() - start_time) * 1000,
                        timestamp=datetime.now(timezone.utc),
                        error_details=str(e)
                    )
                    responses.append(error_response)
            
            batch_time = (time.time() - start_time) * 1000
            self.logger.info(
                f"Batch evaluation completado: {len(responses)} responses en {batch_time:.2f}ms"
            )
            
            return responses
            
        except Exception as e:
            self.logger.error(f"Error en batch evaluation: {str(e)}")
            # Retornar respuestas de error para todos los requests
            error_responses = []
            for request in requests:
                error_response = FuzzyEvaluationResponse(
                    request_id=request.request_id,
                    system_id=request.system_id,
                    success=False,
                    crisp_outputs={},
                    processing_time_ms=(time.time() - start_time) * 1000,
                    timestamp=datetime.now(timezone.utc),
                    error_details=str(e)
                )
                error_responses.append(error_response)
            return error_responses
    
    async def shutdown_async(self):
        """Cierra el motor fuzzy de forma asíncrona y libera recursos."""
        self.logger.info("Iniciando shutdown asíncrono del motor fuzzy")
        
        with self._state_lock:
            self._state = FuzzyEngineState.SHUTDOWN
        
        try:
            # Cerrar async task manager si existe
            if self.async_task_manager:
                await self.async_task_manager.stop()
                self.logger.info("Async task manager cerrado")
            
            # Esperar a que terminen las evaluaciones en curso
            max_wait_time = 30  # segundos
            wait_interval = 0.1
            waited = 0
            
            while waited < max_wait_time:
                with self._stats_lock:
                    if self._processing_stats['current_concurrent_requests'] == 0:
                        break
                await asyncio.sleep(wait_interval)
                waited += wait_interval
            
            if self._processing_stats['current_concurrent_requests'] > 0:
                self.logger.warning(
                    f"Cerrando con {self._processing_stats['current_concurrent_requests']} requests activos"
                )
            
            # Limpiar recursos
            self.optimize_performance()
            
            # Cerrar optimizador de concurrencia si existe
            if self.concurrency_optimizer:
                await self.concurrency_optimizer.stop_optimization()
                self.logger.info("Optimizador de concurrencia cerrado")
            
            # Cerrar gestor de thread pools si existe
            if self.thread_pool_manager:
                await self.thread_pool_manager.stop()
                self.logger.info("Gestor de thread pools cerrado")
            
            self.logger.info("Motor fuzzy cerrado correctamente")
            
        except Exception as e:
            self.logger.error(f"Error durante shutdown: {str(e)}")
            raise FuzzyEngineException(f"Error durante shutdown: {str(e)}")
    
    async def start_optimization_services(self) -> None:
        """Inicia los servicios de optimización asíncrona."""
        if not self._async_enabled:
            self.logger.info("Procesamiento asíncrono deshabilitado, saltando servicios de optimización")
            return
        
        try:
            self.logger.info("Iniciando servicios de optimización")
            
            # Iniciar async task manager
            if self.async_task_manager:
                await self.async_task_manager.start()
                self.logger.info("Async task manager iniciado")
            
            # Iniciar optimizador de concurrencia
            if self.concurrency_optimizer:
                await self.concurrency_optimizer.start_optimization()
                self.logger.info("Optimizador de concurrencia iniciado")
            
            # Iniciar gestor de thread pools
            if self.thread_pool_manager:
                await self.thread_pool_manager.start()
                self.logger.info("Gestor de thread pools iniciado")
            
            self.logger.info("Todos los servicios de optimización iniciados correctamente")
            
        except Exception as e:
            self.logger.error(f"Error iniciando servicios de optimización: {e}")
            raise FuzzyEngineException(f"Error iniciando servicios de optimización: {e}") from e
    
    def get_optimization_status(self) -> Dict[str, Any]:
        """Obtiene el estado de los servicios de optimización."""
        status = {
            'async_processing_enabled': self._async_enabled,
            'concurrency_optimizer': None,
            'thread_pool_manager': None,
            'async_task_manager': None
        }
        
        try:
            if self.concurrency_optimizer:
                status['concurrency_optimizer'] = self.concurrency_optimizer.get_optimization_status()
            
            if self.thread_pool_manager:
                status['thread_pool_manager'] = self.thread_pool_manager.get_status()
            
            if self.async_task_manager:
                status['async_task_manager'] = self.async_task_manager.get_status()
            
        except Exception as e:
            self.logger.error(f"Error obteniendo estado de optimización: {e}")
            status['error'] = str(e)
        
        return status
    
    def enable_concurrency_optimization(self) -> None:
        """Habilita la optimización automática de concurrencia."""
        if self.concurrency_optimizer:
            self.concurrency_optimizer.enable_optimization()
            self.logger.info("Optimización de concurrencia habilitada")
        else:
            self.logger.warning("Optimizador de concurrencia no disponible")
    
    def disable_concurrency_optimization(self) -> None:
        """Deshabilita la optimización automática de concurrencia."""
        if self.concurrency_optimizer:
            self.concurrency_optimizer.disable_optimization()
            self.logger.info("Optimización de concurrencia deshabilitada")
        else:
            self.logger.warning("Optimizador de concurrencia no disponible")
    
    def force_concurrency_optimization(self) -> None:
        """Fuerza una optimización inmediata de concurrencia."""
        if self.concurrency_optimizer:
            self.concurrency_optimizer.force_optimization()
            self.logger.info("Optimización de concurrencia forzada")
        else:
            self.logger.warning("Optimizador de concurrencia no disponible")
    
    async def execute_with_optimized_resources(self, 
                                             func: Callable,
                                             *args,
                                             use_thread_pool: bool = True,
                                             timeout_seconds: float = 30.0,
                                             **kwargs) -> Any:
        """Ejecuta una función usando recursos optimizados del pool."""
        if not use_thread_pool or not self.thread_pool_manager:
            # Ejecutar directamente
            return await asyncio.to_thread(func, *args, **kwargs)
        
        try:
            # Usar thread pool optimizado
            async with self.thread_pool_manager.get_executor_async(timeout_seconds) as executor:
                future = executor.submit(func, *args, **kwargs)
                return await asyncio.wrap_future(future)
                
        except Exception as e:
            self.logger.error(f"Error ejecutando con recursos optimizados: {e}")
            # Fallback a ejecución directa
            return await asyncio.to_thread(func, *args, **kwargs)
    
    def shutdown(self):
        """Cierra el motor fuzzy de forma segura."""
        try:
            with self._state_lock:
                self.logger.info("Iniciando cierre del motor fuzzy")
                self._state = FuzzyEngineState.SHUTDOWN
                
                # Cerrar async task manager si existe (de forma síncrona)
                if self.async_task_manager:
                    try:
                        loop = asyncio.get_event_loop()
                        if loop.is_running():
                            # Si hay un loop corriendo, crear una tarea
                            asyncio.create_task(self.async_task_manager.stop())
                        else:
                            # Si no hay loop, ejecutar directamente
                            loop.run_until_complete(self.async_task_manager.stop())
                    except RuntimeError:
                        # Si no hay loop disponible, crear uno nuevo
                        asyncio.run(self.async_task_manager.stop())
                    self.logger.info("Async task manager cerrado")
                
                # Esperar a que terminen las evaluaciones en curso
                max_wait_time = 30  # segundos
                wait_interval = 0.1
                waited = 0
                
                while waited < max_wait_time:
                    with self._stats_lock:
                        if self._processing_stats['current_concurrent_requests'] == 0:
                            break
                    time.sleep(wait_interval)
                    waited += wait_interval
                
                if self._processing_stats['current_concurrent_requests'] > 0:
                    self.logger.warning(
                        f"Cerrando con {self._processing_stats['current_concurrent_requests']} requests activos"
                    )
                
                # Cerrar componentes de optimización
                if self.concurrency_optimizer:
                    try:
                        asyncio.run(self.concurrency_optimizer.stop_optimization())
                        self.logger.info("Optimizador de concurrencia cerrado")
                    except Exception as e:
                        self.logger.error(f"Error cerrando optimizador: {e}")
                
                if self.thread_pool_manager:
                    try:
                        asyncio.run(self.thread_pool_manager.stop())
                        self.logger.info("Gestor de thread pools cerrado")
                    except Exception as e:
                        self.logger.error(f"Error cerrando thread pool manager: {e}")
                
                # Limpiar recursos
                self.optimize_performance()
                
                self.logger.info("Motor fuzzy cerrado correctamente")
                
        except Exception as e:
            self.logger.error(f"Error cerrando motor fuzzy: {e}")
            raise FuzzyEngineException(f"Error en cierre del motor: {e}") from e
    
    def __enter__(self):
        """Soporte para context manager."""
        return self
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        """Soporte para context manager."""
        self.shutdown()