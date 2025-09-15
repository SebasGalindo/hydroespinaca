"""Tests de integración para el flujo de fuzzificación completo."""
import asyncio
import sys
import pathlib
from unittest.mock import AsyncMock, MagicMock, patch
from datetime import datetime, timezone
import pytest

# Agregar el directorio raíz al path
ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from FuzzyService.Application.Features.SensorProcessing.Handlers.ProcessSensorReadingsHandler import ProcessSensorReadingsHandler
from FuzzyService.Application.Features.SensorProcessing.Commands.ProcessSensorReadingsCommand import (
    ProcessSensorReadingsCommand,
    SensorReading
)
from FuzzyService.Domain.Enums.EntityStatus import FuzzySystemStatus
from FuzzyService.Domain.Enums.MembershipFunctionType import MembershipFunctionType
from FuzzyService.Domain.ValueObjects.MembershipFunction import MembershipFunction
from FuzzyService.Infrastructure.ExternalServices.FuzzyEngine.ScikitFuzzyEngine import FuzzificationResult


@pytest.mark.asyncio
@patch('FuzzyService.Application.Features.SensorProcessing.Handlers.ProcessSensorReadingsHandler.di')
async def test_fuzzification_integration_flow(mock_di):
    """Test de integración del flujo completo de fuzzificación."""
    # Arrange - Configurar mocks de repositorios
    mock_fuzzy_system_repo = AsyncMock()
    mock_fuzzy_variable_repo = AsyncMock()
    mock_fuzzy_rule_repo = AsyncMock()
    mock_fuzzy_term_repo = AsyncMock()
    mock_fuzzy_evaluation_repo = AsyncMock()
    mock_fuzzy_engine = AsyncMock()
    
    def di_getitem(interface):
        if 'IFuzzySystemRepository' in str(interface):
            return mock_fuzzy_system_repo
        elif 'IFuzzyVariableRepository' in str(interface):
            return mock_fuzzy_variable_repo
        elif 'IFuzzyRuleRepository' in str(interface):
            return mock_fuzzy_rule_repo
        elif 'IFuzzyTermRepository' in str(interface):
            return mock_fuzzy_term_repo
        elif 'IFuzzyEvaluationRepository' in str(interface):
            return mock_fuzzy_evaluation_repo
        elif 'IFuzzyEngine' in str(interface):
            return mock_fuzzy_engine
        return MagicMock()
    
    mock_di.__getitem__.side_effect = di_getitem
    
    # Mock del sistema fuzzy activo
    mock_system = MagicMock()
    mock_system.id = "507f1f77bcf86cd799439011"  # ObjectId válido
    mock_system.name = "Test Hydroponic System"
    mock_system.status = FuzzySystemStatus.ACTIVE
    mock_fuzzy_system_repo.get_by_status.return_value = [mock_system]
    
    # Mock de variables fuzzy
    mock_temp_variable = MagicMock()
    mock_temp_variable.id = "temp_var_id"
    mock_temp_variable.name = "temperature"
    mock_temp_variable.device_id = "temp_sensor_01"
    mock_temp_variable.variable_type = "input"
    
    mock_humidity_variable = MagicMock()
    mock_humidity_variable.id = "humidity_var_id"
    mock_humidity_variable.name = "humidity"
    mock_humidity_variable.device_id = "humidity_sensor_01"
    mock_humidity_variable.variable_type = "input"
    
    mock_fuzzy_variable_repo.get_by_system_id.return_value = [
        mock_temp_variable, mock_humidity_variable
    ]
    
    # Mock de reglas fuzzy
    mock_fuzzy_rule_repo.get_by_system_id.return_value = [MagicMock(), MagicMock()]
    
    # Mock de términos fuzzy
    mock_temp_low_term = MagicMock()
    mock_temp_low_term.id = "temp_low_id"
    mock_temp_low_term.name = "low"
    mock_temp_low_term.variable_id = "temp_var_id"
    mock_temp_low_term.membership_function = MembershipFunction(
        function_type=MembershipFunctionType.TRIANGULAR,
        parameters=[15.0, 20.0, 25.0],
        universe_min=0.0,
        universe_max=50.0
    )
    
    mock_temp_medium_term = MagicMock()
    mock_temp_medium_term.id = "temp_medium_id"
    mock_temp_medium_term.name = "medium"
    mock_temp_medium_term.variable_id = "temp_var_id"
    mock_temp_medium_term.membership_function = MembershipFunction(
        function_type=MembershipFunctionType.TRIANGULAR,
        parameters=[20.0, 25.0, 30.0],
        universe_min=0.0,
        universe_max=50.0
    )
    
    mock_humidity_low_term = MagicMock()
    mock_humidity_low_term.id = "humidity_low_id"
    mock_humidity_low_term.name = "low"
    mock_humidity_low_term.variable_id = "humidity_var_id"
    mock_humidity_low_term.membership_function = MembershipFunction(
        function_type=MembershipFunctionType.TRIANGULAR,
        parameters=[30.0, 40.0, 50.0],
        universe_min=0.0,
        universe_max=100.0
    )
    
    # Configurar retorno de términos por variable
    def get_terms_by_variable_id(variable_id):
        if variable_id == "temp_var_id":
            return [mock_temp_low_term, mock_temp_medium_term]
        elif variable_id == "humidity_var_id":
            return [mock_humidity_low_term]
        return []
    
    mock_fuzzy_term_repo.get_by_variable_id.side_effect = get_terms_by_variable_id
    
    # Mock del motor fuzzy - configurar resultado de evaluación completa
    mock_fuzzy_engine.complete_fuzzy_evaluation.return_value = {
        "fuzzification_results": [
            {
                "variable_name": "temperature",
                "sensor_id": "temp_sensor_01",
                "value": 22.5,
                "term_activations": {"low": 0.5, "medium": 0.8}
            },
            {
                "variable_name": "humidity",
                "sensor_id": "humidity_sensor_01",
                "value": 45.0,
                "term_activations": {"low": 0.9}
            }
        ],
        "rule_evaluation_results": [],
        "defuzzification_results": {}
    }
    
    # Configurar mock del repositorio de evaluaciones fuzzy
    mock_fuzzy_evaluation = MagicMock()
    mock_fuzzy_evaluation.id = "507f1f77bcf86cd799439012"
    mock_fuzzy_evaluation.model_dump.return_value = {
        "id": "507f1f77bcf86cd799439012",
        "system_id": "507f1f77bcf86cd799439011",
        "fuzzy_evaluation_results": {
            "fuzzification_results": [
                {
                    "variable_name": "temperature",
                    "sensor_id": "temp_sensor_01",
                    "value": 22.5,
                    "term_activations": {"low": 0.5, "medium": 0.8}
                },
                {
                    "variable_name": "humidity",
                    "sensor_id": "humidity_sensor_01",
                    "value": 45.0,
                    "term_activations": {"low": 0.9}
                }
            ],
            "rule_evaluation_results": [],
            "defuzzification_results": {}
        }
    }
    mock_fuzzy_evaluation_repo.create.return_value = mock_fuzzy_evaluation
    
    # Crear handler y comando
    handler = ProcessSensorReadingsHandler()
    
    readings = [
        SensorReading(
            sensor_id="temp_sensor_01",
            value=22.5,
            timestamp=datetime.now(timezone.utc).isoformat(),
            metadata={"unit": "celsius"}
        ),
        SensorReading(
            sensor_id="humidity_sensor_01",
            value=45.0,
            timestamp=datetime.now(timezone.utc).isoformat(),
            metadata={"unit": "percent"}
        )
    ]
    
    command = ProcessSensorReadingsCommand(
        readings=readings,
        batch_timestamp=datetime.now(timezone.utc).isoformat(),
        esp32_id="ESP32_001"
    )
    
    # Act - Ejecutar el handler
    await handler(command)
    
    # Assert - Verificar que se llamaron los métodos correctos
    mock_fuzzy_system_repo.get_by_status.assert_called_once_with(FuzzySystemStatus.ACTIVE)
    mock_fuzzy_variable_repo.get_by_system_id.assert_called_once_with("507f1f77bcf86cd799439011")
    mock_fuzzy_rule_repo.get_by_system_id.assert_called_once_with("507f1f77bcf86cd799439011")
    
    # Verificar que se cargaron términos para ambas variables
    assert mock_fuzzy_term_repo.get_by_variable_id.call_count == 2
    mock_fuzzy_term_repo.get_by_variable_id.assert_any_call("temp_var_id")
    mock_fuzzy_term_repo.get_by_variable_id.assert_any_call("humidity_var_id")
    
    # Verificar que se llamó al motor fuzzy con los parámetros correctos
    mock_fuzzy_engine.complete_fuzzy_evaluation.assert_called_once()
    call_args = mock_fuzzy_engine.complete_fuzzy_evaluation.call_args
    
    # Verificar argumentos de la llamada
    assert len(call_args.kwargs['variables']) == 2
    assert len(call_args.kwargs['terms']) == 3  # 2 términos de temp + 1 de humidity
    assert call_args.kwargs['sensor_readings'] == {
        "temp_sensor_01": 22.5,
        "humidity_sensor_01": 45.0
    }
    
    # Verificar el resultado del comando
    assert command.result is not None
    assert 'fuzzy_evaluation_results' in command.result
    assert len(command.result['fuzzy_evaluation_results']['fuzzification_results']) == 2
    
    # Verificar estructura del resultado
    temp_var_result = next(
        (r for r in command.result['fuzzy_evaluation_results']['fuzzification_results'] if r['variable_name'] == 'temperature'), 
        None
    )
    assert temp_var_result is not None
    assert temp_var_result['sensor_id'] == 'temp_sensor_01'
    assert temp_var_result['value'] == 22.5
    assert temp_var_result['term_activations']['low'] == 0.5
    assert temp_var_result['term_activations']['medium'] == 0.8
    
    humidity_var_result = next(
        (r for r in command.result['fuzzy_evaluation_results']['fuzzification_results'] if r['variable_name'] == 'humidity'), 
        None
    )
    assert humidity_var_result is not None
    assert humidity_var_result['sensor_id'] == 'humidity_sensor_01'
    assert humidity_var_result['value'] == 45.0
    assert humidity_var_result['term_activations']['low'] == 0.9
    
    print("✅ test_fuzzification_integration_flow PASSED")


@pytest.mark.asyncio
@patch('FuzzyService.Application.Features.SensorProcessing.Handlers.ProcessSensorReadingsHandler.di')
async def test_fuzzification_no_terms_error(mock_di):
    """Test que verifica el manejo de error cuando no hay términos fuzzy."""
    # Arrange
    mock_fuzzy_system_repo = AsyncMock()
    mock_fuzzy_variable_repo = AsyncMock()
    mock_fuzzy_rule_repo = AsyncMock()
    mock_fuzzy_term_repo = AsyncMock()
    mock_fuzzy_engine = AsyncMock()
    
    def di_getitem(interface):
        if 'IFuzzySystemRepository' in str(interface):
            return mock_fuzzy_system_repo
        elif 'IFuzzyVariableRepository' in str(interface):
            return mock_fuzzy_variable_repo
        elif 'IFuzzyRuleRepository' in str(interface):
            return mock_fuzzy_rule_repo
        elif 'IFuzzyTermRepository' in str(interface):
            return mock_fuzzy_term_repo
        elif 'IFuzzyEngine' in str(interface):
            return mock_fuzzy_engine
        return MagicMock()
    
    mock_di.__getitem__.side_effect = di_getitem
    
    # Mock del sistema fuzzy activo
    mock_system = MagicMock()
    mock_system.id = "test_system_id"
    mock_system.name = "Test System"
    mock_system.status = FuzzySystemStatus.ACTIVE
    mock_fuzzy_system_repo.get_by_status.return_value = [mock_system]
    
    # Mock de variable sin términos
    mock_variable = MagicMock()
    mock_variable.id = "var_id"
    mock_variable.name = "temperature"
    mock_variable.device_id = "temp_sensor_01"
    mock_variable.variable_type = "input"
    mock_fuzzy_variable_repo.get_by_system_id.return_value = [mock_variable]
    
    # Mock de reglas
    mock_fuzzy_rule_repo.get_by_system_id.return_value = [MagicMock()]
    
    # No hay términos para la variable
    mock_fuzzy_term_repo.get_by_variable_id.return_value = []
    
    handler = ProcessSensorReadingsHandler()
    
    readings = [
        SensorReading(
            sensor_id="temp_sensor_01",
            value=22.5,
            timestamp=datetime.now(timezone.utc).isoformat(),
            metadata={"unit": "celsius"}
        )
    ]
    
    command = ProcessSensorReadingsCommand(
        readings=readings,
        batch_timestamp=datetime.now(timezone.utc).isoformat(),
        esp32_id="ESP32_001"
    )
    
    # Act & Assert - Verificar que se lanza la excepción esperada
    try:
        await handler(command)
        assert False, "Se esperaba una BusinessRuleViolationError"
    except Exception as e:
        assert "No se encontraron términos fuzzy" in str(e)
        print("✅ test_fuzzification_no_terms_error PASSED")


if __name__ == "__main__":
    asyncio.run(test_fuzzification_integration_flow())
    asyncio.run(test_fuzzification_no_terms_error())