import pytest
import pytest_asyncio
import asyncio
import time
from unittest.mock import Mock, patch
from datetime import datetime, timezone
from typing import List

from FuzzyService.Infrastructure.FuzzyEngine.ScikitFuzzyEngine import (
    ScikitFuzzyEngine,
    FuzzyEvaluationRequest,
    FuzzyEvaluationResponse
)
from FuzzyService.Infrastructure.FuzzyEngine.FuzzyEngineConfiguration import (
    FuzzyEngineConfiguration,
    PerformanceLimits,
    DefuzzificationMethod,
    AggregationMethod
)
from FuzzyService.Infrastructure.FuzzyEngine.RuleEvaluationEngine import (
    FuzzyRule,
    RuleCondition,
    RuleConsequent,
    LogicalOperator
)
from FuzzyService.Infrastructure.FuzzyEngine.AsyncTaskQueue import (
    TaskPriority,
    TaskStatus,
    AsyncTaskManager
)
from FuzzyService.Infrastructure.FuzzyEngine.AsyncLoadBalancer import (
    LoadBalancingStrategy,
    LoadBalancerConfig
)


class TestAsyncProcessing:
    """Tests para el sistema de procesamiento asíncrono."""
    
    @pytest.fixture
    def performance_config(self):
        """Configuración de rendimiento para tests."""
        return PerformanceLimits(
            max_concurrent_evaluations=10,
            cache_size_limit=100,
            max_processing_time_seconds=30,
            parallel_threshold=5,
            max_workers=4
        )
    
    @pytest.fixture
    def engine_config(self, performance_config):
        """Configuración del motor fuzzy para tests."""
        return FuzzyEngineConfiguration(
            default_defuzzification_method=DefuzzificationMethod.CENTROID,
            default_aggregation_method=AggregationMethod.MAX,
            performance_limits=performance_config
        )
    
    @pytest_asyncio.fixture
    async def fuzzy_engine(self, engine_config):
        """Motor fuzzy configurado para tests."""
        engine = ScikitFuzzyEngine(engine_config)
        
        # Inicializar async task manager si está habilitado
        if engine.async_task_manager and engine._async_enabled:
            await engine.async_task_manager.start()
        
        yield engine
        
        # Cleanup asíncrono
        if engine.async_task_manager and engine._async_enabled:
            await engine.async_task_manager.stop()
        engine.shutdown()
    
    @pytest.fixture
    def sample_request(self):
        """Request de ejemplo para tests."""
        return FuzzyEvaluationRequest(
            request_id="test-001",
            system_id="test-system",
            sensor_data={"temperature": 25.0, "humidity": 60.0},
            timestamp=datetime.now(timezone.utc),
            rules=[
                FuzzyRule(
                    rule_id="rule1",
                    conditions=[
                        RuleCondition(
                            sensor_id="temp_sensor_1",
                            variable_name="temperature",
                            term_name="medium",
                            operator=None
                        )
                    ],
                    consequents=[
                        RuleConsequent(
                            variable_name="fan_speed",
                            term_name="medium",
                            routine_id="fan_control_routine",
                            step_number=1
                        )
                    ],
                    logical_operator=LogicalOperator.AND,
                    priority=1
                )
            ],
            membership_functions={
                "temperature": {
                    "low": {"type": "trimf", "params": [0, 0, 25]},
                    "medium": {"type": "trimf", "params": [15, 25, 35]},
                    "high": {"type": "trimf", "params": [25, 50, 50]}
                },
                "fan_speed": {
                    "low": {"type": "trimf", "params": [0, 0, 50]},
                    "medium": {"type": "trimf", "params": [25, 50, 75]},
                    "high": {"type": "trimf", "params": [50, 100, 100]}
                }
            }
        )
    
    @pytest.mark.asyncio
    async def test_async_task_manager_initialization(self, fuzzy_engine):
        """Test que el async task manager se inicializa correctamente."""
        assert fuzzy_engine.async_task_manager is not None
        assert fuzzy_engine._async_enabled is True
        
        # Verificar que el load balancer está configurado
        assert fuzzy_engine.async_task_manager.load_balancer is not None
        assert fuzzy_engine.async_task_manager.load_balancer.config.strategy == LoadBalancingStrategy.ADAPTIVE
    
    @pytest.mark.asyncio
    async def test_single_async_evaluation_with_task_queue(self, fuzzy_engine, sample_request):
        """Test evaluación asíncrona individual usando task queue."""
        start_time = time.time()
        
        response = await fuzzy_engine.evaluate_fuzzy_logic(sample_request)
        
        end_time = time.time()
        processing_time = (end_time - start_time) * 1000
        
        # Verificar respuesta
        assert response is not None
        assert response.request_id == sample_request.request_id
        assert response.system_id == sample_request.system_id
        assert isinstance(response.success, bool)
        assert response.processing_time_ms > 0
        assert processing_time < 5000  # Menos de 5 segundos
        
        print(f"Async evaluation completada en {processing_time:.2f}ms")
    
    @pytest.mark.asyncio
    async def test_batch_async_evaluation(self, fuzzy_engine, sample_request):
        """Test evaluación batch asíncrona con load balancing."""
        # Crear múltiples requests
        requests = []
        for i in range(5):
            request = FuzzyEvaluationRequest(
                request_id=f"batch-{i:03d}",
                system_id=sample_request.system_id,
                sensor_data=sample_request.sensor_data.copy(),
                timestamp=datetime.now(timezone.utc),
                rules=sample_request.rules.copy(),
                membership_functions=sample_request.membership_functions.copy()
            )
            requests.append(request)
        
        start_time = time.time()
        
        responses = await fuzzy_engine.evaluate_batch_async(requests)
        
        end_time = time.time()
        batch_time = (end_time - start_time) * 1000
        
        # Verificar respuestas
        assert len(responses) == len(requests)
        
        for i, response in enumerate(responses):
            assert response.request_id == f"batch-{i:03d}"
            assert response.system_id == sample_request.system_id
            assert isinstance(response.success, bool)
            assert response.processing_time_ms > 0
        
        print(f"Batch evaluation de {len(requests)} requests completada en {batch_time:.2f}ms")
        print(f"Promedio por request: {batch_time / len(requests):.2f}ms")
    
    @pytest.mark.asyncio
    async def test_concurrent_evaluations_performance(self, fuzzy_engine, sample_request):
        """Test rendimiento con evaluaciones concurrentes."""
        num_concurrent = 10
        
        # Crear requests concurrentes
        tasks = []
        for i in range(num_concurrent):
            request = FuzzyEvaluationRequest(
                request_id=f"concurrent-{i:03d}",
                system_id=sample_request.system_id,
                sensor_data=sample_request.sensor_data.copy(),
                timestamp=datetime.now(timezone.utc),
                rules=sample_request.rules.copy(),
                membership_functions=sample_request.membership_functions.copy()
            )
            task = fuzzy_engine.evaluate_fuzzy_logic(request)
            tasks.append(task)
        
        start_time = time.time()
        
        responses = await asyncio.gather(*tasks, return_exceptions=True)
        
        end_time = time.time()
        total_time = (end_time - start_time) * 1000
        
        # Verificar que no hay excepciones
        successful_responses = [r for r in responses if isinstance(r, FuzzyEvaluationResponse)]
        exceptions = [r for r in responses if isinstance(r, Exception)]
        
        assert len(exceptions) == 0, f"Se encontraron {len(exceptions)} excepciones"
        assert len(successful_responses) == num_concurrent
        
        print(f"Concurrent evaluation de {num_concurrent} requests completada en {total_time:.2f}ms")
        print(f"Promedio por request: {total_time / num_concurrent:.2f}ms")
    
    def test_engine_status_with_async_info(self, fuzzy_engine):
        """Test que el estado del motor incluye información async."""
        status = fuzzy_engine.get_engine_status()
        
        assert 'async_processing' in status
        assert status['async_processing']['enabled'] is True
        assert 'worker_count' in status['async_processing']
        assert 'queue_size' in status['async_processing']
        assert 'load_balancer' in status['async_processing']
        
        # Verificar configuración
        assert 'configuration' in status
        assert status['configuration']['async_processing_enabled'] is True
    
    def test_health_check_with_async_components(self, fuzzy_engine):
        """Test que el health check incluye componentes async."""
        health = fuzzy_engine.health_check()
        
        assert 'checks' in health
        assert 'async_processing' in health['checks']
        
        async_check = health['checks']['async_processing']
        assert 'status' in async_check
        assert 'metrics' in async_check
        assert async_check['metrics']['total_workers'] > 0
    
    @pytest.mark.asyncio
    async def test_task_priority_handling(self, fuzzy_engine, sample_request):
        """Test que las tareas de alta prioridad se procesan primero."""
        if not fuzzy_engine.async_task_manager:
            pytest.skip("Async task manager no disponible")
        
        # Crear tareas de diferentes prioridades
        high_priority_request = FuzzyEvaluationRequest(
            request_id="high-priority",
            system_id=sample_request.system_id,
            sensor_data=sample_request.sensor_data.copy(),
            timestamp=datetime.now(timezone.utc),
            rules=sample_request.rules.copy(),
            membership_functions=sample_request.membership_functions.copy()
        )
        
        low_priority_request = FuzzyEvaluationRequest(
            request_id="low-priority",
            system_id=sample_request.system_id,
            sensor_data=sample_request.sensor_data.copy(),
            timestamp=datetime.now(timezone.utc),
            rules=sample_request.rules.copy(),
            membership_functions=sample_request.membership_functions.copy()
        )
        
        # Enviar tarea de baja prioridad primero
        low_task = fuzzy_engine.async_task_manager.submit_task(
            task_func=fuzzy_engine._execute_fuzzy_pipeline,
            args=(low_priority_request,),
            priority=TaskPriority.LOW,
            timeout_seconds=30
        )
        
        # Enviar tarea de alta prioridad después
        high_task = fuzzy_engine.async_task_manager.submit_task(
            task_func=fuzzy_engine._execute_fuzzy_pipeline,
            args=(high_priority_request,),
            priority=TaskPriority.HIGH,
            timeout_seconds=30
        )
        
        # Esperar resultados
        high_result = await high_task
        low_result = await low_task
        
        # Verificar que ambas tareas se completaron
        assert high_result.status == TaskStatus.COMPLETED
        assert low_result.status == TaskStatus.COMPLETED
        
        # La tarea de alta prioridad debería completarse primero o al mismo tiempo
        assert high_result.completed_at <= low_result.completed_at
    
    @pytest.mark.asyncio
    async def test_load_balancer_worker_distribution(self, fuzzy_engine, sample_request):
        """Test que el load balancer distribuye tareas entre workers."""
        if not fuzzy_engine.async_task_manager:
            pytest.skip("Async task manager no disponible")
        
        num_tasks = 8
        tasks = []
        
        # Crear múltiples tareas
        for i in range(num_tasks):
            request = FuzzyEvaluationRequest(
                request_id=f"load-test-{i:03d}",
                system_id=sample_request.system_id,
                sensor_data=sample_request.sensor_data.copy(),
                timestamp=datetime.now(timezone.utc),
                rules=sample_request.rules.copy(),
                membership_functions=sample_request.membership_functions.copy()
            )
            
            task = fuzzy_engine.async_task_manager.submit_task(
                task_func=fuzzy_engine._execute_fuzzy_pipeline,
                args=(request,),
                priority=TaskPriority.NORMAL,
                timeout_seconds=30
            )
            tasks.append(task)
        
        # Esperar que todas las tareas se completen
        results = await asyncio.gather(*tasks)
        
        # Verificar que todas las tareas se completaron exitosamente
        completed_tasks = [r for r in results if r.status == TaskStatus.COMPLETED]
        assert len(completed_tasks) == num_tasks
        
        # Verificar que se utilizaron múltiples workers (si hay más de uno)
        worker_count = len(fuzzy_engine.async_task_manager.task_manager.workers)
        if worker_count > 1:
            # Al menos debería haber alguna distribución de carga
            worker_usage = {}
            for result in results:
                worker_id = getattr(result, 'worker_id', 'unknown')
                worker_usage[worker_id] = worker_usage.get(worker_id, 0) + 1
            
            # Debería haber al menos 2 workers diferentes utilizados
            assert len(worker_usage) >= min(2, worker_count)
    
    @pytest.mark.asyncio
    async def test_async_shutdown_graceful(self, engine_config):
        """Test que el shutdown asíncrono es graceful."""
        engine = ScikitFuzzyEngine(engine_config)
        
        # Verificar que el motor está listo
        assert engine._is_ready()
        assert engine.async_task_manager is not None
        
        # Realizar shutdown asíncrono
        await engine.shutdown_async()
        
        # Verificar que el motor está en estado shutdown
        assert engine._state.value == "shutdown"
    
    def test_async_disabled_fallback(self, performance_config):
        """Test que el motor funciona correctamente cuando async está deshabilitado."""
        # Deshabilitar async processing
        performance_config.enable_async_processing = False
        
        config = FuzzyEngineConfiguration(
            defuzzification_method=DefuzzificationMethod.CENTROID,
            aggregation_method=AggregationMethod.MAX,
            performance=performance_config
        )
        
        engine = ScikitFuzzyEngine(config)
        
        try:
            # Verificar que async está deshabilitado
            assert engine._async_enabled is False
            assert engine.async_task_manager is None
            
            # Verificar estado del motor
            status = engine.get_engine_status()
            assert status['async_processing']['enabled'] is False
            
        finally:
            engine.shutdown()


class TestAsyncPerformanceBenchmarks:
    """Benchmarks de rendimiento para procesamiento asíncrono."""
    
    @pytest.fixture
    def benchmark_config(self):
        """Configuración optimizada para benchmarks."""
        return FuzzyEngineConfiguration(
            default_defuzzification_method=DefuzzificationMethod.CENTROID,
            default_aggregation_method=AggregationMethod.MAX,
            performance_limits=PerformanceLimits(
                max_concurrent_evaluations=20,
                cache_size_limit=1000,
                max_processing_time_seconds=60,
                parallel_threshold=5,
                max_workers=8
            )
        )
    
    @pytest.mark.asyncio
    async def test_async_vs_sync_performance(self, benchmark_config, sample_request):
        """Benchmark comparando rendimiento async vs sync."""
        engine = ScikitFuzzyEngine(benchmark_config)
        
        try:
            num_requests = 20
            
            # Test asíncrono
            async_start = time.time()
            async_tasks = []
            for i in range(num_requests):
                request = FuzzyEvaluationRequest(
                    request_id=f"async-bench-{i:03d}",
                    system_id="benchmark",
                    sensor_data={"temp": 20.0 + i, "humidity": 50.0 + i},
                    timestamp=datetime.now(timezone.utc),
                    rules=sample_request.rules.copy(),
                    membership_functions=sample_request.membership_functions.copy()
                )
                task = engine.evaluate_fuzzy_logic(request)
                async_tasks.append(task)
            
            async_responses = await asyncio.gather(*async_tasks)
            async_end = time.time()
            async_time = (async_end - async_start) * 1000
            
            # Test síncrono
            sync_start = time.time()
            sync_responses = []
            for i in range(num_requests):
                request = FuzzyEvaluationRequest(
                    request_id=f"sync-bench-{i:03d}",
                    system_id="benchmark",
                    sensor_data={"temp": 20.0 + i, "humidity": 50.0 + i},
                    timestamp=datetime.now(timezone.utc),
                    rules=sample_request.rules.copy(),
                    membership_functions=sample_request.membership_functions.copy()
                )
                response = engine.evaluate_fuzzy_logic_sync(request)
                sync_responses.append(response)
            
            sync_end = time.time()
            sync_time = (sync_end - sync_start) * 1000
            
            # Verificar resultados
            assert len(async_responses) == num_requests
            assert len(sync_responses) == num_requests
            
            # Mostrar resultados
            print(f"\nBenchmark Results ({num_requests} requests):")
            print(f"Async processing: {async_time:.2f}ms ({async_time/num_requests:.2f}ms/req)")
            print(f"Sync processing:  {sync_time:.2f}ms ({sync_time/num_requests:.2f}ms/req)")
            print(f"Speedup: {sync_time/async_time:.2f}x")
            
            # El procesamiento asíncrono debería ser más rápido para múltiples requests
            if num_requests > 5:
                assert async_time < sync_time, "Async processing should be faster for multiple requests"
            
        finally:
            await engine.shutdown_async()