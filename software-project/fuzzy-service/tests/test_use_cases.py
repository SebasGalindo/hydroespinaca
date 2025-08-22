"""Pruebas para casos de uso de la aplicación.

Este archivo contiene pruebas básicas para los casos de uso principales.
"""

import pytest
from datetime import datetime, timezone, timedelta
from unittest.mock import Mock, patch, MagicMock
from typing import List

from application.use_cases import (
    FuzzyEvaluationUseCase,
    HysteresisFilterUseCase,
    SimulationUseCase
)
from application.dtos import ReadingBatch, ReadingInput, SimulateResponseDto
from domain.models import (
    Routine, ActuatorMapping, ActuatorType, ActuatorState,
    OutputPlan, ThresholdRule, Variable
)
from infrastructure.fuzzy_engine import FuzzyEngine


class TestFuzzyEvaluationUseCase:
    """Pruebas para FuzzyEvaluationUseCase."""
    
    @pytest.fixture
    def use_case(self):
        """Fixture que crea una instancia del caso de uso."""
        return FuzzyEvaluationUseCase()
    
    @pytest.fixture
    def sample_batch(self):
        """Batch de lecturas de ejemplo."""
        return ReadingBatch(
            esp32Id="test_esp32",
            timestamp=datetime.now(timezone.utc),
            readings=[
                ReadingInput(variableId="ph", value=6.5),
                ReadingInput(variableId="ec", value=1.2),
                ReadingInput(variableId="temperature", value=22.0)
            ]
        )
    
    @pytest.fixture
    def sample_routine(self):
        """Rutina de ejemplo con mapeos de actuadores."""
        return Routine(
            id="test_routine",
            name="Test Routine",
            outputs=[
                ActuatorMapping(
                    output_name="fan_power",
                    actuatorId="fan_001",
                    esp32Id="test_esp32",
                    actuator_type=ActuatorType.VARIABLE
                ),
                ActuatorMapping(
                    output_name="pump_control",
                    actuatorId="pump_001",
                    esp32Id="test_esp32",
                    actuator_type=ActuatorType.ON_OFF
                )
            ]
        )
    
    def test_initialization(self, use_case):
        """Test que el caso de uso se inicializa correctamente."""
        assert use_case.fuzzy_engine is not None
        assert isinstance(use_case.fuzzy_engine, FuzzyEngine)
    
    @patch('application.use_cases.FuzzyEngine')
    def test_evaluate_batch_success(self, mock_engine_class, use_case, sample_batch, sample_routine):
        """Test evaluación exitosa de un batch."""
        # Configurar mock del motor difuso
        mock_engine = Mock()
        mock_engine.evaluate.return_value = {
            'intensidad': 75.0  # El motor difuso devuelve intensidad
        }
        use_case.fuzzy_engine = mock_engine
        
        # Ejecutar evaluación
        plans = use_case.evaluate_batch(sample_batch, [sample_routine])
        
        # Verificar resultados
        assert len(plans) == 2
        assert all(isinstance(plan, OutputPlan) for plan in plans)
        
        # Verificar que se llamó al motor difuso
        mock_engine.evaluate.assert_called_once()
    
    def test_evaluate_batch_empty_routines(self, use_case, sample_batch):
        """Test evaluación con lista vacía de rutinas."""
        plans = use_case.evaluate_batch(sample_batch, [])
        assert plans == []
    
    def test_evaluate_batch_no_mappings(self, use_case, sample_batch):
        """Test evaluación con rutina sin mapeos de actuadores."""
        routine_without_mappings = Routine(
            id="empty_routine",
            name="Empty Routine",
            actuator_mappings=[]
        )
        
        plans = use_case.evaluate_batch(sample_batch, [routine_without_mappings])
        assert plans == []
    
    @patch('application.use_cases.FuzzyEngine')
    def test_evaluate_batch_engine_error(self, mock_engine_class, use_case, sample_batch, sample_routine):
        """Test manejo de errores del motor difuso."""
        # Configurar mock para lanzar excepción
        mock_engine = Mock()
        mock_engine.evaluate.side_effect = Exception("Engine error")
        use_case.fuzzy_engine = mock_engine
        
        # Verificar que se propaga la excepción
        with pytest.raises(Exception, match="Engine error"):
            use_case.evaluate_batch(sample_batch, [sample_routine])


class TestHysteresisFilterUseCase:
    """Pruebas para HysteresisFilterUseCase."""
    
    @pytest.fixture
    def use_case(self):
        """Fixture que crea una instancia del caso de uso."""
        return HysteresisFilterUseCase(hysteresis_delta=5.0, cooldown_seconds=30)
    
    @pytest.fixture
    def sample_plans(self):
        """Planes de salida de ejemplo."""
        fan_actuator = ActuatorMapping(
            output_name="fan_power",
            actuatorId="fan_001",
            esp32Id="test_esp32",
            actuator_type=ActuatorType.VARIABLE
        )
        pump_actuator = ActuatorMapping(
            output_name="pump_control",
            actuatorId="pump_001",
            esp32Id="test_esp32",
            actuator_type=ActuatorType.ON_OFF
        )
        return [
            OutputPlan(
                actuator=fan_actuator,
                target=75,
                hold_seconds=300
            ),
            OutputPlan(
                actuator=pump_actuator,
                target=60,
                hold_seconds=180
            )
        ]
    
    @pytest.fixture
    def sample_states(self):
        """Estados de actuadores de ejemplo."""
        return {
            "fan_001": ActuatorState(
                actuatorId="fan_001",
                last_target=70.0,
                last_emitted_at=datetime.now(timezone.utc),
                last_hold_seconds=25
            ),
            "pump_001": ActuatorState(
                actuatorId="pump_001",
                last_target=55.0,
                last_emitted_at=datetime.now(timezone.utc),
                last_hold_seconds=35
            )
        }
    
    def test_initialization(self, use_case):
        """Test que el caso de uso se inicializa correctamente."""
        assert use_case.hysteresis_delta == 5.0
        assert use_case.cooldown_seconds == 30
    
    def test_filter_plans_no_previous_state(self, use_case, sample_plans):
        """Test filtrado sin estados previos (todos los planes pasan)."""
        filtered_plans = use_case.filter_plans(sample_plans, {})
        assert len(filtered_plans) == 2
        assert filtered_plans == sample_plans
    
    def test_filter_plans_within_hysteresis(self, use_case, sample_states):
        """Test filtrado con cambios dentro del umbral de histeresis."""
        # Crear planes con cambios menores al delta (< 5.0)
        fan_actuator = ActuatorMapping(
            output_name="fan_power",
            actuatorId="fan_001",
            esp32Id="test_esp32",
            actuator_type=ActuatorType.VARIABLE
        )
        
        small_change_plans = [
            OutputPlan(
                actuator=fan_actuator,
                target=72,  # last_target=70.0, diferencia=2.0 < 5.0
                hold_seconds=300
            )
        ]
        
        filtered_plans = use_case.filter_plans(small_change_plans, sample_states)
        
        # No debería pasar ningún plan porque diferencia < delta
        assert len(filtered_plans) == 0
    
    def test_filter_plans_outside_hysteresis(self, use_case):
        """Test filtrado con cambios fuera del umbral de histeresis."""
        fan_actuator = ActuatorMapping(
            output_name="fan_power",
            actuatorId="fan_001",
            esp32Id="test_esp32",
            actuator_type=ActuatorType.VARIABLE
        )
        plans_with_big_change = [
            OutputPlan(
                actuator=fan_actuator,
                target=80,  # Diferencia de 10.0 > delta (5.0)
                hold_seconds=300
            )
        ]
        
        # Estado con cooldown expirado
        states_with_expired_cooldown = {
            "fan_001": ActuatorState(
                actuatorId="fan_001",
                last_target=70.0,
                last_emitted_at=datetime.now(timezone.utc) - timedelta(seconds=40),  # Cooldown expirado
                last_hold_seconds=300
            )
        }
        
        filtered_plans = use_case.filter_plans(plans_with_big_change, states_with_expired_cooldown)
        assert len(filtered_plans) == 1
        assert filtered_plans[0].target == 80
    
    def test_filter_plans_cooldown_active(self, use_case, sample_plans):
        """Test filtrado con cooldown activo."""
        # Estado con emisión reciente (dentro del cooldown)
        recent_states = {
            "fan_001": ActuatorState(
                actuatorId="fan_001",
                last_target=60.0,  # Diferencia > delta para pasar histéresis
                last_emitted_at=datetime.now(timezone.utc) - timedelta(seconds=10),  # 10 segundos atrás < 30 cooldown
                last_hold_seconds=300
            )
        }
        
        filtered_plans = use_case.filter_plans(sample_plans, recent_states)
        
        # El fan_001 debería ser filtrado por cooldown
        assert len(filtered_plans) == 1
        assert filtered_plans[0].actuator.actuatorId == "pump_001"
    
    def test_filter_plans_cooldown_expired(self, use_case, sample_plans):
        """Test filtrado con cooldown expirado."""
        # Estado con emisión antigua (fuera del cooldown)
        old_states = {
            "fan_001": ActuatorState(
                actuatorId="fan_001",
                last_target=60.0,  # Diferencia > delta
                last_emitted_at=datetime.now(timezone.utc) - timedelta(seconds=40),  # 40 segundos atrás > 30 cooldown
                last_hold_seconds=300
            )
        }
        
        filtered_plans = use_case.filter_plans(sample_plans, old_states)
        
        # Ambos planes deberían pasar
        assert len(filtered_plans) == 2


class TestSimulationUseCase:
    """Pruebas para SimulationUseCase."""
    
    @pytest.fixture
    def use_case(self):
        """Fixture que crea una instancia del caso de uso."""
        return SimulationUseCase()
    
    @pytest.fixture
    def sample_batch(self):
        """Batch de lecturas de ejemplo."""
        return ReadingBatch(
            esp32Id="test_esp32",
            timestamp=datetime.now(timezone.utc),
            readings=[
                ReadingInput(variableId="ph", value=6.5),
                ReadingInput(variableId="ec", value=1.2)
            ]
        )
    
    @pytest.fixture
    def sample_routine(self):
        """Rutina de ejemplo."""
        return Routine(
            id="test_routine",
            name="Test Routine",
            outputs=[
                ActuatorMapping(
                    output_name="fan_power",
                    actuatorId="fan_001",
                    esp32Id="test_esp32",
                    actuator_type=ActuatorType.VARIABLE
                )
            ]
        )
    
    @pytest.fixture
    def mock_evaluation_use_case(self):
        """Mock del caso de uso de evaluación."""
        mock_use_case = Mock(spec=FuzzyEvaluationUseCase)
        fan_actuator = ActuatorMapping(
            output_name="fan_power",
            actuatorId="fan_001",
            esp32Id="test_esp32",
            actuator_type=ActuatorType.VARIABLE
        )
        mock_use_case.evaluate_batch.return_value = [
            OutputPlan(
                actuator=fan_actuator,
                target=75,
                hold_seconds=300
            )
        ]
        return mock_use_case
    
    def test_initialization(self, use_case):
        """Test que el caso de uso se inicializa correctamente."""
        assert use_case is not None
    
    @pytest.mark.asyncio
    async def test_simulate_success(self, use_case, sample_batch, sample_routine, mock_evaluation_use_case):
        """Test simulación exitosa."""
        result = await use_case.simulate(sample_batch, [sample_routine], mock_evaluation_use_case)
        
        # Verificar que se llamó al caso de uso de evaluación
        mock_evaluation_use_case.evaluate_batch.assert_called_once_with(sample_batch, [sample_routine])
        
        # Verificar el resultado
        assert isinstance(result, SimulateResponseDto)
        assert len(result.plans) == 1
        assert result.plans[0]['actuator']['actuatorId'] == "fan_001"
        assert result.plans[0]['target'] == 75
    
    @pytest.mark.asyncio
    async def test_simulate_empty_routines(self, use_case, sample_batch, mock_evaluation_use_case):
        """Test simulación con rutinas vacías."""
        mock_evaluation_use_case.evaluate_batch.return_value = []
        
        result = await use_case.simulate(sample_batch, [], mock_evaluation_use_case)
        
        assert isinstance(result, SimulateResponseDto)
        assert len(result.plans) == 0
    
    @pytest.mark.asyncio
    async def test_simulate_evaluation_error(self, use_case, sample_batch, sample_routine, mock_evaluation_use_case):
        """Test manejo de errores en la evaluación."""
        mock_evaluation_use_case.evaluate_batch.side_effect = Exception("Evaluation error")
        
        with pytest.raises(Exception, match="Evaluation error"):
            await use_case.simulate(sample_batch, [sample_routine], mock_evaluation_use_case)