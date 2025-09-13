"""Pruebas para el ProcessSensorReadingsHandler"""
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


@pytest.mark.asyncio
@patch('FuzzyService.Application.Features.SensorProcessing.Handlers.ProcessSensorReadingsHandler.di')
async def test_process_sensor_readings_handler_basic(mock_di):
    """Prueba básica del ProcessSensorReadingsHandler."""
    # Arrange
    mock_fuzzy_system_repo = AsyncMock()
    mock_fuzzy_variable_repo = AsyncMock()
    mock_fuzzy_rule_repo = AsyncMock()
    mock_fuzzy_term_repo = AsyncMock()
    mock_fuzzy_engine = AsyncMock()
    
    # Configurar el mock del contenedor DI
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
    
    handler = ProcessSensorReadingsHandler()
    
    # Mock del sistema fuzzy activo
    mock_system = MagicMock()
    mock_system.id = "test_system_id"
    mock_system.name = "Test System"
    mock_system.status = FuzzySystemStatus.ACTIVE
    mock_fuzzy_system_repo.get_by_status.return_value = [mock_system]
    
    # Mock de variables
    mock_variable = MagicMock()
    mock_variable.id = "var_id_1"
    mock_variable.name = "Soil Moisture"
    mock_variable.device_id = "sensor_soil_moisture_1"
    mock_fuzzy_variable_repo.get_by_system_id.return_value = [mock_variable]
    
    # Mock de reglas
    mock_fuzzy_rule_repo.get_by_system_id.return_value = []
    
    # Mock de términos fuzzy
    mock_term = MagicMock()
    mock_term.id = "term_id_1"
    mock_term.label = "medium"
    mock_fuzzy_term_repo.get_by_variable_id.return_value = [mock_term]
    
    # Mock del motor fuzzy - configurar resultado de evaluación
    mock_fuzzy_engine.complete_fuzzy_evaluation.return_value = {
        "fuzzification_results": [],
        "rule_evaluation_results": [],
        "defuzzification_results": {}
    }
    
    # Crear comando
    sensor_reading = SensorReading(
        sensor_id="sensor_soil_moisture_1",
        value=45.2,
        timestamp="2024-01-15T10:30:00Z",
        metadata={"esp32_id": "esp32_001"}
    )
    
    command = ProcessSensorReadingsCommand(
        readings=[sensor_reading],
        batch_timestamp="2024-01-15T10:30:00Z",
        esp32_id="esp32_001"
    )
    
    # Act
    await handler(command)
    
    # Assert
    assert command.result is not None
    assert "active_system" in command.result
    assert command.result["active_system"]["name"] == "Test System"
    assert "active_variables" in command.result
    assert len(command.result["active_variables"]) == 1
    assert command.result["active_variables"][0]["name"] == "Soil Moisture"
    
    # Verificar que se llamaron los métodos correctos
    mock_fuzzy_system_repo.get_by_status.assert_called_once_with(FuzzySystemStatus.ACTIVE)
    mock_fuzzy_variable_repo.get_by_system_id.assert_called_once_with("test_system_id")
    mock_fuzzy_rule_repo.get_by_system_id.assert_called_once_with("test_system_id")
    
    print("✅ test_process_sensor_readings_handler_basic PASSED")


@pytest.mark.asyncio
@patch('FuzzyService.Application.Features.SensorProcessing.Handlers.ProcessSensorReadingsHandler.di')
async def test_process_sensor_readings_handler_no_active_system(mock_di):
    """Prueba cuando no hay sistema fuzzy activo."""
    # Arrange
    mock_fuzzy_system_repo = AsyncMock()
    mock_fuzzy_variable_repo = AsyncMock()
    mock_fuzzy_rule_repo = AsyncMock()
    
    # Configurar el mock del contenedor DI
    def di_getitem(interface):
        if 'IFuzzySystemRepository' in str(interface):
            return mock_fuzzy_system_repo
        elif 'IFuzzyVariableRepository' in str(interface):
            return mock_fuzzy_variable_repo
        elif 'IFuzzyRuleRepository' in str(interface):
            return mock_fuzzy_rule_repo
        return MagicMock()
    
    mock_di.__getitem__.side_effect = di_getitem
    
    handler = ProcessSensorReadingsHandler()
    
    # No hay sistema activo
    mock_fuzzy_system_repo.get_by_status.return_value = []
    
    # Crear comando
    sensor_reading = SensorReading(
        sensor_id="sensor_soil_moisture_1",
        value=45.2,
        timestamp="2024-01-15T10:30:00Z",
        metadata={"esp32_id": "esp32_001"}
    )
    
    command = ProcessSensorReadingsCommand(
        readings=[sensor_reading],
        batch_timestamp="2024-01-15T10:30:00Z",
        esp32_id="esp32_001"
    )
    
    # Act & Assert - Debe lanzar excepción
    try:
        await handler(command)
        assert False, "Debería haber lanzado EntityNotFoundError"
    except Exception as e:
        assert "No se encontró un sistema fuzzy activo" in str(e)
    
    # Solo debe llamar a get_by_status
    mock_fuzzy_system_repo.get_by_status.assert_called_once_with(FuzzySystemStatus.ACTIVE)
    mock_fuzzy_variable_repo.get_by_system_id.assert_not_called()
    mock_fuzzy_rule_repo.get_by_system_id.assert_not_called()
    
    print("✅ test_process_sensor_readings_handler_no_active_system PASSED")


if __name__ == "__main__":
    asyncio.run(test_process_sensor_readings_handler_basic())
    asyncio.run(test_process_sensor_readings_handler_no_active_system())