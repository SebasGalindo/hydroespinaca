import pytest
import json
import sys
import pathlib
from unittest.mock import AsyncMock, MagicMock
from datetime import datetime, timezone

# Agregar el directorio raíz al path
ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from FuzzyService.Infrastructure.ExternalServices.MqttService.MqttMessageHandler import MqttMessageHandler
from FuzzyService.Application.Features.SensorProcessing.Commands.ProcessSensorReadingsCommand import (
    ProcessSensorReadingsCommand,
    SensorReading
)


class TestMqttMessageHandler:
    """Pruebas unitarias para MqttMessageHandler."""
    
    @pytest.fixture
    def mock_mediator(self):
        """Mock del mediator para las pruebas."""
        return AsyncMock()
    
    @pytest.fixture
    def mqtt_handler(self, mock_mediator):
        """Instancia de MqttMessageHandler para pruebas."""
        return MqttMessageHandler(mock_mediator)
    
    @pytest.fixture
    def valid_mqtt_message(self):
        """Mensaje MQTT válido para pruebas."""
        return {
            "timestamp": "2024-01-15T10:30:00Z",
            "esp32_id": "esp32_001",
            "_metadata": {
                "esp32_id": "esp32_001",
                "received_at": "2024-01-15T10:30:01Z"
            },
            "readings": [
                {
                    "device_id": "sensor_soil_moisture_1",
                    "value": 45.2,
                    "unit": "%",
                    "timestamp": "2024-01-15T10:30:00Z"
                },
                {
                    "device_id": "sensor_temperature_1",
                    "value": 23.8,
                    "unit": "°C",
                    "timestamp": "2024-01-15T10:30:00Z"
                }
            ]
        }
    
    @pytest.mark.asyncio
    async def test_handle_message_success(self, mqtt_handler, mock_mediator, valid_mqtt_message):
        """Prueba el manejo exitoso de un mensaje MQTT válido."""
        # Arrange
        topic = "sensor/esp32_001/batch"
        payload = json.dumps(valid_mqtt_message).encode('utf-8')
        
        # Configurar el mock para simular procesamiento exitoso
        async def mock_send(command):
            command.result = {"active_variables": ["Soil Moisture", "Ambient Temperature"]}
            return None
        
        mock_mediator.send.side_effect = mock_send
        
        # Act
        await mqtt_handler.handle(valid_mqtt_message)
        
        # Assert
        mock_mediator.send.assert_called_once()
        call_args = mock_mediator.send.call_args[0][0]
        
        assert isinstance(call_args, ProcessSensorReadingsCommand)
        assert call_args.esp32_id == "esp32_001"
        assert len(call_args.readings) == 2
        assert call_args.readings[0].sensor_id == "sensor_soil_moisture_1"
        assert call_args.readings[0].value == 45.2
        assert mqtt_handler._readings_processed == 2