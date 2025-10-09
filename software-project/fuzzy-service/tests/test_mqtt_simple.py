"""Pruebas simples para el flujo MQTT sin dependencias del conftest.py"""
import asyncio
import json
import sys
import pathlib
from unittest.mock import AsyncMock
import pytest

# Agregar el directorio raíz al path
ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from FuzzyService.Infrastructure.ExternalServices.MqttService.MqttMessageHandler import MqttMessageHandler
from FuzzyService.Application.Features.SensorProcessing.Commands.ProcessSensorReadingsCommand import (
    ProcessSensorReadingsCommand,
    SensorReading
)


@pytest.mark.asyncio
async def test_mqtt_message_handler_basic():
    """Prueba básica del MqttMessageHandler."""
    # Arrange
    mock_mediator = AsyncMock()
    mqtt_handler = MqttMessageHandler(mock_mediator)
    
    valid_message = {
        "timestamp": "2024-01-15T10:30:00Z",
        "readings": {
            "sensor_soil_moisture_1": 45.2
        },
        "_metadata": {
            "esp32_id": "esp32_001",
            "received_at": "2024-01-15T10:30:00Z"
        }
    }
    
    topic = "sensor/esp32_001/batch"
    payload = json.dumps(valid_message).encode('utf-8')
    
    # Configurar el mock
    async def mock_send(command):
        command.result = {"active_variables": ["Soil Moisture"]}
        return None
    
    mock_mediator.send.side_effect = mock_send
    
    # Act
    await mqtt_handler.handle(valid_message)
    
    # Assert
    assert mock_mediator.send.called
    call_args = mock_mediator.send.call_args[0][0]
    assert isinstance(call_args, ProcessSensorReadingsCommand)
    assert call_args.esp32_id == "esp32_001"
    assert len(call_args.readings) == 1
    assert call_args.readings[0].sensor_id == "sensor_soil_moisture_1"
    assert call_args.readings[0].value == 45.2
    assert mqtt_handler._readings_processed == 1
    
    print("✅ test_mqtt_message_handler_basic PASSED")


@pytest.mark.asyncio
async def test_mqtt_message_handler_multiple_readings():
    """Prueba el MqttMessageHandler con múltiples lecturas."""
    # Arrange
    mock_mediator = AsyncMock()
    mqtt_handler = MqttMessageHandler(mock_mediator)
    
    valid_message = {
        "timestamp": "2024-01-15T10:30:00Z",
        "readings": {
            "sensor_soil_moisture_1": 45.2,
            "sensor_temperature_1": 23.8,
            "sensor_light_1": 750.5
        },
        "_metadata": {
            "esp32_id": "esp32_001",
            "received_at": "2024-01-15T10:30:00Z"
        }
    }
    
    # Configurar el mock
    async def mock_send(command):
        command.result = {"active_variables": ["Soil Moisture", "Ambient Temperature", "Light Intensity"]}
        return None
    
    mock_mediator.send.side_effect = mock_send
    
    # Act
    await mqtt_handler.handle(valid_message)
    
    # Assert
    assert mock_mediator.send.called
    call_args = mock_mediator.send.call_args[0][0]
    assert isinstance(call_args, ProcessSensorReadingsCommand)
    assert call_args.esp32_id == "esp32_001"
    assert len(call_args.readings) == 3
    
    # Verificar que todas las lecturas están presentes
    sensor_ids = [reading.sensor_id for reading in call_args.readings]
    assert "sensor_soil_moisture_1" in sensor_ids
    assert "sensor_temperature_1" in sensor_ids
    assert "sensor_light_1" in sensor_ids
    
    assert mqtt_handler._readings_processed == 3
    
    print("✅ test_mqtt_message_handler_multiple_readings PASSED")


@pytest.mark.asyncio
async def test_mqtt_message_handler_invalid_values():
    """Prueba el MqttMessageHandler con valores inválidos."""
    # Arrange
    mock_mediator = AsyncMock()
    mqtt_handler = MqttMessageHandler(mock_mediator)
    
    invalid_message = {
        "timestamp": "2024-01-15T10:30:00Z",
        "readings": {
            "sensor_valid": 45.2,
            "sensor_invalid_string": "invalid",
            "sensor_invalid_none": None,
            "sensor_valid_2": 23.8
        },
        "_metadata": {
            "esp32_id": "esp32_001",
            "received_at": "2024-01-15T10:30:00Z"
        }
    }
    
    # Configurar el mock
    async def mock_send(command):
        command.result = {"active_variables": ["Valid Sensor"]}
        return None
    
    mock_mediator.send.side_effect = mock_send
    
    # Act
    await mqtt_handler.handle(invalid_message)
    
    # Assert
    assert mock_mediator.send.called
    call_args = mock_mediator.send.call_args[0][0]
    assert isinstance(call_args, ProcessSensorReadingsCommand)
    
    # Solo deben procesarse las lecturas válidas
    assert len(call_args.readings) == 2
    sensor_ids = [reading.sensor_id for reading in call_args.readings]
    assert "sensor_valid" in sensor_ids
    assert "sensor_valid_2" in sensor_ids
    assert "sensor_invalid_string" not in sensor_ids
    assert "sensor_invalid_none" not in sensor_ids
    
    # Verificar que se registraron errores de validación
    assert mqtt_handler._validation_errors == 2
    assert mqtt_handler._readings_processed == 2
    
    print("✅ test_mqtt_message_handler_invalid_values PASSED")


if __name__ == "__main__":
    asyncio.run(test_mqtt_message_handler_basic())
    asyncio.run(test_mqtt_message_handler_multiple_readings())
    asyncio.run(test_mqtt_message_handler_invalid_values())