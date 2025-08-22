import pytest
import asyncio
from unittest.mock import AsyncMock, Mock

# Configuración global para pytest
pytest_plugins = []


@pytest.fixture(scope="session")
def event_loop():
    """Crear un event loop para toda la sesión de tests."""
    loop = asyncio.new_event_loop()
    yield loop
    loop.close()


@pytest.fixture
def mock_variable_repository():
    """Mock del repositorio de variables."""
    return AsyncMock()


@pytest.fixture
def mock_routine_repository():
    """Mock del repositorio de rutinas."""
    return AsyncMock()


@pytest.fixture
def mock_actuator_state_repository():
    """Mock del repositorio de estados de actuadores."""
    return AsyncMock()


@pytest.fixture
def mock_fuzzy_engine():
    """Mock del motor de lógica difusa."""
    return Mock()


@pytest.fixture
def sample_variables():
    """Variables de ejemplo para tests."""
    from domain.models import Variable, FuzzySet, MembershipFunctionType
    
    return [
        Variable(
            id="temperature",
            name="Temperatura",
            unit="°C",
            min_value=0.0,
            max_value=50.0,
            description="Temperatura ambiente",
            fuzzy_sets=[
                FuzzySet(
                    name="low",
                    membership_type=MembershipFunctionType.TRIANGULAR,
                    parameters=[0, 10, 20],
                    description="Temperatura baja"
                ),
                FuzzySet(
                    name="medium",
                    membership_type=MembershipFunctionType.TRIANGULAR,
                    parameters=[15, 25, 35],
                    description="Temperatura media"
                ),
                FuzzySet(
                    name="high",
                    membership_type=MembershipFunctionType.TRIANGULAR,
                    parameters=[30, 40, 50],
                    description="Temperatura alta"
                )
            ]
        ),
        Variable(
            id="humidity",
            name="Humedad",
            unit="%",
            min_value=0.0,
            max_value=100.0,
            description="Humedad relativa",
            fuzzy_sets=[
                FuzzySet(
                    name="low",
                    membership_type=MembershipFunctionType.TRIANGULAR,
                    parameters=[0, 20, 40],
                    description="Humedad baja"
                ),
                FuzzySet(
                    name="medium",
                    membership_type=MembershipFunctionType.TRIANGULAR,
                    parameters=[30, 50, 70],
                    description="Humedad media"
                ),
                FuzzySet(
                    name="high",
                    membership_type=MembershipFunctionType.TRIANGULAR,
                    parameters=[60, 80, 100],
                    description="Humedad alta"
                )
            ]
        )
    ]


@pytest.fixture
def sample_routine():
    """Rutina de ejemplo para tests."""
    from domain.models import (
        Routine, FuzzyRule, FuzzyCondition, LogicalOperator,
        ActuatorMapping, ControlParameters
    )
    
    return Routine(
        id="test_routine",
        name="Rutina de Prueba",
        description="Rutina para testing",
        active=True,
        fuzzy_rules=[
            FuzzyRule(
                id="test_rule",
                name="Regla de Prueba",
                description="Regla para testing",
                conditions=[
                    FuzzyCondition(variable_id="temperature", fuzzy_set_name="high")
                ],
                operator=LogicalOperator.AND,
                output_variable_id="fan",
                output_fuzzy_set_name="high",
                priority=1,
                active=True
            )
        ],
        threshold_rules=[],
        outputs=[
            ActuatorMapping(
                output_name="fan",
                actuatorId="fan_001",
                esp32Id="esp32_001",
                actuator_type="variable",
                on_threshold=50.0
            )
        ],
        control_parameters=ControlParameters(
            evaluation_interval=60,
            min_change_threshold=5.0,
            max_change_rate=20.0,
            cooldown_period=300
        ),
        input_variables=["temperature", "humidity"],
        output_variables=["fan"]
    )


@pytest.fixture
def sample_reading_batch():
    """Batch de lecturas de ejemplo para tests."""
    from application.dtos import ReadingBatch, ReadingInput
    from datetime import datetime
    
    return ReadingBatch(
        esp32Id="esp32_001",
        timestamp=datetime.now(),
        readings=[
            ReadingInput(
                variableId="temperature",
                value=25.0
            ),
            ReadingInput(
                variableId="humidity",
                value=60.0
            )
        ]
    )