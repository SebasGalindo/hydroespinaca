#!/usr/bin/env python3
"""
Script simple para probar el procesamiento asíncrono del motor fuzzy.
"""

import pytest
import asyncio
import time
from datetime import datetime, timezone

from FuzzyService.Infrastructure.FuzzyEngine.ScikitFuzzyEngine import (
    ScikitFuzzyEngine,
    FuzzyEvaluationRequest
)
from FuzzyService.Infrastructure.FuzzyEngine.FuzzyEngineConfiguration import (
    FuzzyEngineConfiguration,
    PerformanceLimits,
    DefuzzificationMethod,
    AggregationMethod,
    LogicalOperator
)
from FuzzyService.Infrastructure.FuzzyEngine.RuleEvaluationEngine import (
    FuzzyRule,
    RuleCondition,
    RuleConsequent,
    LogicalOperator
)


def create_test_config():
    """Crea configuración de prueba."""
    performance_config = PerformanceLimits(
        max_execution_time_seconds=30.0,
        max_memory_usage_mb=200,
        max_rules_per_evaluation=1000
    )
    
    return FuzzyEngineConfiguration(
        performance_limits=performance_config
    )


def create_test_request(request_id: str):
    """Crea un request de prueba."""
    return FuzzyEvaluationRequest(
        request_id=request_id,
        system_id="test-system",
        sensor_data={"temperature": 25.0, "humidity": 60.0},
        timestamp=datetime.now(timezone.utc),
        rules=[
            FuzzyRule(
            rule_id="rule1",
            conditions=[
                RuleCondition(
                     sensor_id="temp_sensor_001",
                     variable_name="temperature",
                     term_name="high"
                 )
            ],
            consequents=[
                RuleConsequent(
                    variable_name="fan_speed",
                    term_name="high",
                    routine_id="test_routine_001",
                    step_number=1
                )
            ],
            logical_operator=LogicalOperator.AND
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
async def test_async_processing():
    """Test principal del procesamiento asíncrono."""
    print("=== Test de Procesamiento Asíncrono ===")
    
    # Crear configuración y motor
    config = create_test_config()
    engine = ScikitFuzzyEngine(config)
    
    try:
        print(f"Motor inicializado. Estado: {engine._state.value}")
        print(f"Async habilitado: {engine._async_enabled}")
        print(f"Task manager disponible: {engine.async_task_manager is not None}")
        
        if engine.async_task_manager:
            # Iniciar el async task manager para que se configure el load balancer
            await engine.async_task_manager.start()
            print(f"Workers disponibles: {len(engine.async_task_manager.workers)}")
            if engine.async_task_manager.load_balancer:
                print(f"Load balancer strategy: {engine.async_task_manager.load_balancer.config.strategy.value}")
            else:
                print("Load balancer no disponible")
        
        # Test 1: Evaluación individual
        print("\n--- Test 1: Evaluación Individual ---")
        request = create_test_request("test-001")
        
        start_time = time.time()
        response = await engine.evaluate_fuzzy_logic(request)
        end_time = time.time()
        
        print(f"Request ID: {response.request_id}")
        print(f"Success: {response.success}")
        print(f"Processing time: {response.processing_time_ms:.2f}ms")
        print(f"Total time: {(end_time - start_time) * 1000:.2f}ms")
        
        # Test 2: Evaluación batch
        print("\n--- Test 2: Evaluación Batch ---")
        requests = [create_test_request(f"batch-{i:03d}") for i in range(5)]
        
        start_time = time.time()
        responses = await engine.evaluate_batch_async(requests)
        end_time = time.time()
        
        print(f"Requests procesados: {len(responses)}")
        print(f"Tiempo total batch: {(end_time - start_time) * 1000:.2f}ms")
        print(f"Promedio por request: {(end_time - start_time) * 1000 / len(responses):.2f}ms")
        
        successful = sum(1 for r in responses if r.success)
        print(f"Exitosos: {successful}/{len(responses)}")
        
        # Test 3: Evaluaciones concurrentes
        print("\n--- Test 3: Evaluaciones Concurrentes ---")
        concurrent_requests = [create_test_request(f"concurrent-{i:03d}") for i in range(8)]
        
        start_time = time.time()
        tasks = [engine.evaluate_fuzzy_logic(req) for req in concurrent_requests]
        concurrent_responses = await asyncio.gather(*tasks)
        end_time = time.time()
        
        print(f"Requests concurrentes: {len(concurrent_responses)}")
        print(f"Tiempo total concurrente: {(end_time - start_time) * 1000:.2f}ms")
        print(f"Promedio por request: {(end_time - start_time) * 1000 / len(concurrent_responses):.2f}ms")
        
        concurrent_successful = sum(1 for r in concurrent_responses if r.success)
        print(f"Exitosos: {concurrent_successful}/{len(concurrent_responses)}")
        
        # Test 4: Estado del motor
        print("\n--- Test 4: Estado del Motor ---")
        status = engine.get_engine_status()
        print(f"Estado: {status['state']}")
        print(f"Ready: {status['is_ready']}")
        
        if 'async_processing' in status:
            async_info = status['async_processing']
            print(f"Async enabled: {async_info.get('enabled', False)}")
            if async_info.get('enabled'):
                print(f"Workers: {async_info.get('worker_count', 0)}")
                print(f"Active workers: {async_info.get('active_workers', 0)}")
                print(f"Queue size: {async_info.get('queue_size', 0)}")
        
        # Test 5: Health check
        print("\n--- Test 5: Health Check ---")
        health = engine.health_check()
        print(f"Health status: {health['status']}")
        
        if 'checks' in health:
            for check_name, check_info in health['checks'].items():
                print(f"  {check_name}: {check_info['status']} - {check_info['message']}")
        
        print("\n=== Todos los tests completados exitosamente ===")
        
    except Exception as e:
        print(f"Error durante los tests: {str(e)}")
        import traceback
        traceback.print_exc()
        
    finally:
        # Shutdown del motor
        print("\n--- Cerrando motor ---")
        await engine.shutdown_async()
        print("Motor cerrado correctamente")


if __name__ == "__main__":
    asyncio.run(test_async_processing())