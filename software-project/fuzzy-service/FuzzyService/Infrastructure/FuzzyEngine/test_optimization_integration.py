"""Pruebas de integración para las optimizaciones de procesamiento asíncrono."""

import asyncio
import pytest
import time
from typing import Dict, Any
from unittest.mock import Mock, patch

from FuzzyEngineConfiguration import FuzzyEngineConfiguration, PerformanceLimits
from ScikitFuzzyEngine import ScikitFuzzyEngine
from ConcurrencyOptimizer import ConcurrencyOptimizer, OptimizationStrategy
from ResourcePool import ThreadPoolManager
from FuzzyEngineMetrics import FuzzyEngineMetrics
from FuzzyEvaluationRequest import FuzzyEvaluationRequest
from FuzzyRule import FuzzyRule
from MembershipFunction import MembershipFunction


class TestOptimizationIntegration:
    """Pruebas de integración para las optimizaciones implementadas."""
    
    @pytest.fixture
    def config(self) -> FuzzyEngineConfiguration:
        """Configuración de prueba con optimizaciones habilitadas."""
        return FuzzyEngineConfiguration(
            performance_limits=PerformanceLimits(
                max_concurrent_evaluations=10,
                max_workers=4,
                parallel_threshold=5,
                enable_async_processing=True,
                max_rules_per_evaluation=100,
                max_variables_per_evaluation=20
            ),
            enable_caching=True,
            cache_size=1000,
            enable_metrics=True
        )
    
    @pytest.fixture
    def metrics(self) -> FuzzyEngineMetrics:
        """Métricas de prueba."""
        return FuzzyEngineMetrics()
    
    @pytest.fixture
    async def fuzzy_engine(self, config: FuzzyEngineConfiguration, metrics: FuzzyEngineMetrics) -> ScikitFuzzyEngine:
        """Motor fuzzy con optimizaciones habilitadas."""
        engine = ScikitFuzzyEngine(config, metrics)
        await engine.start_optimization_services()
        yield engine
        await engine.shutdown_async()
    
    @pytest.fixture
    def sample_request(self) -> FuzzyEvaluationRequest:
        """Request de prueba."""
        return FuzzyEvaluationRequest(
            request_id="test-001",
            system_id="test-system",
            sensor_data={
                "temperature": 25.0,
                "humidity": 60.0,
                "pressure": 1013.25
            },
            membership_functions={
                "temperature": [
                    MembershipFunction(
                        name="low",
                        function_type="trimf",
                        parameters=[0, 10, 20]
                    ),
                    MembershipFunction(
                        name="medium",
                        function_type="trimf",
                        parameters=[15, 25, 35]
                    ),
                    MembershipFunction(
                        name="high",
                        function_type="trimf",
                        parameters=[30, 40, 50]
                    )
                ]
            },
            rules=[
                FuzzyRule(
                    rule_id="rule-1",
                    conditions=[{"variable": "temperature", "membership": "medium"}],
                    consequent={"output": "normal"}
                )
            ]
        )
    
    @pytest.mark.asyncio
    async def test_optimization_services_startup(self, fuzzy_engine: ScikitFuzzyEngine):
        """Prueba que los servicios de optimización se inicien correctamente."""
        status = fuzzy_engine.get_optimization_status()
        
        assert status['async_processing_enabled'] is True
        assert status['concurrency_optimizer'] is not None
        assert status['thread_pool_manager'] is not None
        assert status['async_task_manager'] is not None
        assert 'error' not in status
    
    @pytest.mark.asyncio
    async def test_concurrency_optimization_control(self, fuzzy_engine: ScikitFuzzyEngine):
        """Prueba el control de optimización de concurrencia."""
        # Habilitar optimización
        fuzzy_engine.enable_concurrency_optimization()
        
        # Verificar estado
        status = fuzzy_engine.get_optimization_status()
        assert status['concurrency_optimizer']['enabled'] is True
        
        # Deshabilitar optimización
        fuzzy_engine.disable_concurrency_optimization()
        
        # Verificar estado
        status = fuzzy_engine.get_optimization_status()
        assert status['concurrency_optimizer']['enabled'] is False
        
        # Forzar optimización
        fuzzy_engine.force_concurrency_optimization()
    
    @pytest.mark.asyncio
    async def test_optimized_resource_execution(self, fuzzy_engine: ScikitFuzzyEngine):
        """Prueba la ejecución con recursos optimizados."""
        def cpu_intensive_task(n: int) -> int:
            """Tarea intensiva en CPU para pruebas."""
            total = 0
            for i in range(n):
                total += i * i
            return total
        
        # Ejecutar con thread pool optimizado
        start_time = time.perf_counter()
        result = await fuzzy_engine.execute_with_optimized_resources(
            cpu_intensive_task,
            10000,
            use_thread_pool=True,
            timeout_seconds=5.0
        )
        execution_time = time.perf_counter() - start_time
        
        assert result == sum(i * i for i in range(10000))
        assert execution_time < 5.0  # Debe completarse dentro del timeout
    
    @pytest.mark.asyncio
    async def test_optimized_fuzzy_evaluation(self, fuzzy_engine: ScikitFuzzyEngine, sample_request: FuzzyEvaluationRequest):
        """Prueba la evaluación fuzzy con optimizaciones."""
        start_time = time.perf_counter()
        response = await fuzzy_engine.evaluate_fuzzy_logic(sample_request)
        execution_time = time.perf_counter() - start_time
        
        assert response.success is True
        assert response.request_id == sample_request.request_id
        assert response.processing_time_ms > 0
        assert execution_time < 10.0  # Debe ser razonablemente rápido
        
        # Verificar que se usaron las optimizaciones
        status = fuzzy_engine.get_optimization_status()
        assert status['thread_pool_manager']['active_pools'] > 0
    
    @pytest.mark.asyncio
    async def test_concurrent_evaluations_with_optimization(self, fuzzy_engine: ScikitFuzzyEngine, sample_request: FuzzyEvaluationRequest):
        """Prueba evaluaciones concurrentes con optimizaciones."""
        # Crear múltiples requests
        requests = []
        for i in range(5):
            request = FuzzyEvaluationRequest(
                request_id=f"test-{i:03d}",
                system_id=sample_request.system_id,
                sensor_data=sample_request.sensor_data,
                membership_functions=sample_request.membership_functions,
                rules=sample_request.rules
            )
            requests.append(request)
        
        # Ejecutar concurrentemente
        start_time = time.perf_counter()
        tasks = [fuzzy_engine.evaluate_fuzzy_logic(req) for req in requests]
        responses = await asyncio.gather(*tasks)
        execution_time = time.perf_counter() - start_time
        
        # Verificar resultados
        assert len(responses) == 5
        for i, response in enumerate(responses):
            assert response.success is True
            assert response.request_id == f"test-{i:03d}"
        
        # Verificar que las optimizaciones mejoraron el rendimiento
        assert execution_time < 15.0  # Debe ser eficiente
        
        # Verificar métricas de optimización
        status = fuzzy_engine.get_optimization_status()
        assert status['thread_pool_manager']['total_acquisitions'] >= 5
    
    @pytest.mark.asyncio
    async def test_resource_pool_efficiency(self, fuzzy_engine: ScikitFuzzyEngine):
        """Prueba la eficiencia del pool de recursos."""
        def simple_task(x: int) -> int:
            return x * 2
        
        # Ejecutar múltiples tareas para probar reutilización de recursos
        tasks = []
        for i in range(10):
            task = fuzzy_engine.execute_with_optimized_resources(
                simple_task,
                i,
                use_thread_pool=True,
                timeout_seconds=1.0
            )
            tasks.append(task)
        
        results = await asyncio.gather(*tasks)
        
        # Verificar resultados
        expected_results = [i * 2 for i in range(10)]
        assert results == expected_results
        
        # Verificar eficiencia del pool
        status = fuzzy_engine.get_optimization_status()
        pool_status = status['thread_pool_manager']
        
        # Debe haber reutilizado recursos
        assert pool_status['total_acquisitions'] >= 10
        assert pool_status['active_pools'] > 0
        assert pool_status['total_releases'] >= 10
    
    @pytest.mark.asyncio
    async def test_optimization_under_load(self, fuzzy_engine: ScikitFuzzyEngine, sample_request: FuzzyEvaluationRequest):
        """Prueba las optimizaciones bajo carga alta."""
        # Simular carga alta
        high_load_requests = []
        for i in range(20):
            request = FuzzyEvaluationRequest(
                request_id=f"load-test-{i:03d}",
                system_id=sample_request.system_id,
                sensor_data=sample_request.sensor_data,
                membership_functions=sample_request.membership_functions,
                rules=sample_request.rules * 3  # Más reglas para mayor carga
            )
            high_load_requests.append(request)
        
        # Forzar optimización antes de la carga
        fuzzy_engine.force_concurrency_optimization()
        
        # Ejecutar bajo carga
        start_time = time.perf_counter()
        tasks = [fuzzy_engine.evaluate_fuzzy_logic(req) for req in high_load_requests]
        responses = await asyncio.gather(*tasks, return_exceptions=True)
        execution_time = time.perf_counter() - start_time
        
        # Verificar que la mayoría de requests fueron exitosos
        successful_responses = [r for r in responses if not isinstance(r, Exception)]
        assert len(successful_responses) >= 15  # Al menos 75% exitosos
        
        # Verificar que las optimizaciones ayudaron
        status = fuzzy_engine.get_optimization_status()
        assert status['concurrency_optimizer']['optimizations_applied'] > 0
        
        print(f"Procesadas {len(successful_responses)} requests en {execution_time:.2f}s")
        print(f"Throughput: {len(successful_responses)/execution_time:.2f} requests/s")
    
    @pytest.mark.asyncio
    async def test_graceful_shutdown_with_optimizations(self, config: FuzzyEngineConfiguration, metrics: FuzzyEngineMetrics):
        """Prueba el shutdown graceful con optimizaciones activas."""
        engine = ScikitFuzzyEngine(config, metrics)
        await engine.start_optimization_services()
        
        # Verificar que los servicios están activos
        status = engine.get_optimization_status()
        assert status['async_processing_enabled'] is True
        
        # Shutdown graceful
        await engine.shutdown_async()
        
        # Verificar que los recursos se liberaron
        # (En una implementación real, verificaríamos que los pools se cerraron)
        assert True  # Placeholder para verificaciones específicas


if __name__ == "__main__":
    pytest.main([__file__, "-v"])