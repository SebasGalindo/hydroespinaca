"""Test de integración con payload real del ESP32.

Valida que el procesamiento MQTT maneja correctamente el formato de datos
enviado por el firmware ESP32 en producción.
"""

import pytest
import json
from datetime import datetime, timezone

from FuzzyService.Infrastructure.ExternalServices.MqttService.MqttMessageHandler import MqttMessageHandler
from FuzzyService.Application.Features.SensorProcessing.Commands.ProcessSensorReadingsCommand import (
    ProcessSensorReadingsCommand,
    SensorReading
)


class TestESP32PayloadIntegration:
    """Test suite para validar integración con payloads reales del ESP32."""

    @pytest.fixture
    def esp32_payload_real(self):
        """Payload real enviado por el firmware ESP32."""
        return {
            "esp32Id": "6883fff7b079309f3ba4f238",
            "timestamp": "2025-10-01T00:53:58Z",
            "readings": [
                {"physicalId": "DHT22-A1", "variableId": "688970ab7f02137645d58398", "value": 19},
                {"physicalId": "DHT22-A1", "variableId": "688970af7f02137645d58399", "value": 75.80000305},
                {"physicalId": "SEN0161-A1", "variableId": "688970a27f02137645d58396", "value": 5.150858402},
                {"physicalId": "TDS-A1", "variableId": "688970a77f02137645d58397", "value": 1.102167845},
                {"physicalId": "NTC-A1", "variableId": "68bb4d8cbdcb66fc5738f9af", "value": 17.3305912},
                {"physicalId": "HC-SR04-A1", "variableId": "68d1d07307c249cda4c369b0", "value": 36.80059814}
            ]
        }

    @pytest.fixture
    def mqtt_message_handler(self):
        """Handler MQTT sin mediator (solo para parsing)."""
        # Crear mock del mediator
        class MockMediator:
            async def send(self, command):
                # No hace nada, solo para testing
                pass

        return MqttMessageHandler(mediator=MockMediator())

    async def test_esp32_payload_parsing(self, mqtt_message_handler, esp32_payload_real):
        """Valida que el payload del ESP32 se parsea correctamente."""
        # Agregar metadata simulada
        payload_with_metadata = esp32_payload_real.copy()
        payload_with_metadata['_metadata'] = {
            'topic': 'sensor/readings',
            'esp32_id': esp32_payload_real['esp32Id'],
            'received_at': datetime.now(timezone.utc),
            'qos': 1
        }

        # Transformar lecturas
        sensor_readings = await mqtt_message_handler._transform_readings(
            payload_with_metadata['readings'],
            payload_with_metadata['timestamp'],
            payload_with_metadata['esp32Id'],
            payload_with_metadata['_metadata']['received_at']
        )

        # Validaciones
        assert len(sensor_readings) == 6, "Deben procesarse las 6 lecturas del ESP32"

        # Verificar que usa variableId como sensor_id
        expected_variable_ids = [
            "688970ab7f02137645d58398",  # DHT22-A1 Temperatura
            "688970af7f02137645d58399",  # DHT22-A1 Humedad
            "688970a27f02137645d58396",  # SEN0161-A1 pH
            "688970a77f02137645d58397",  # TDS-A1 TDS
            "68bb4d8cbdcb66fc5738f9af",  # NTC-A1 Temp Agua
            "68d1d07307c249cda4c369b0",  # HC-SR04-A1 Nivel Agua
        ]

        actual_sensor_ids = [reading.sensor_id for reading in sensor_readings]
        assert set(actual_sensor_ids) == set(expected_variable_ids), \
            f"Los sensor_id deben ser los variableId. Esperados: {expected_variable_ids}, Recibidos: {actual_sensor_ids}"

        # Verificar valores
        assert sensor_readings[0].value == 19.0
        assert sensor_readings[1].value == 75.80000305
        assert sensor_readings[2].value == 5.150858402
        assert sensor_readings[3].value == 1.102167845
        assert sensor_readings[4].value == 17.3305912
        assert sensor_readings[5].value == 36.80059814

    async def test_sensor_reading_command_creation(self, mqtt_message_handler, esp32_payload_real):
        """Valida que se puede crear ProcessSensorReadingsCommand desde el payload ESP32."""
        # Agregar metadata
        payload_with_metadata = esp32_payload_real.copy()
        payload_with_metadata['_metadata'] = {
            'topic': 'sensor/readings',
            'esp32_id': esp32_payload_real['esp32Id'],
            'received_at': datetime.now(timezone.utc),
            'qos': 1
        }

        # Transformar lecturas
        sensor_readings = await mqtt_message_handler._transform_readings(
            payload_with_metadata['readings'],
            payload_with_metadata['timestamp'],
            payload_with_metadata['esp32Id'],
            payload_with_metadata['_metadata']['received_at']
        )

        # Crear comando
        command = ProcessSensorReadingsCommand(
            readings=sensor_readings,
            batch_timestamp=esp32_payload_real['timestamp'],
            esp32_id=esp32_payload_real['esp32Id']
        )

        # Validaciones
        assert command.readings == sensor_readings
        assert command.batch_timestamp == "2025-10-01T00:53:58Z"
        assert command.esp32_id == "6883fff7b079309f3ba4f238"
        assert len(command.readings) == 6

    async def test_readings_list_validation(self, esp32_payload_real):
        """Valida que readings como lista es válido (corrigiendo bug anterior)."""
        readings = esp32_payload_real['readings']

        # Debe ser lista
        assert isinstance(readings, list), "readings debe ser una lista"

        # Cada elemento debe tener los campos requeridos
        for reading in readings:
            assert 'variableId' in reading or 'physicalId' in reading, \
                "Cada lectura debe tener variableId o physicalId"
            assert 'value' in reading, "Cada lectura debe tener value"
            assert isinstance(reading['value'], (int, float)), \
                "El valor debe ser numérico"

    async def test_timestamp_format_validation(self, esp32_payload_real):
        """Valida que el formato de timestamp del ESP32 es válido."""
        timestamp = esp32_payload_real['timestamp']

        # Debe poder parsearse como ISO 8601
        parsed = datetime.fromisoformat(timestamp.replace('Z', '+00:00'))

        assert parsed is not None
        assert parsed.tzinfo is not None, "Timestamp debe incluir timezone"

    def test_payload_json_serialization(self, esp32_payload_real):
        """Valida que el payload ESP32 se puede serializar/deserializar correctamente."""
        # Serializar
        json_str = json.dumps(esp32_payload_real)

        # Deserializar
        deserialized = json.loads(json_str)

        # Validar estructura
        assert deserialized['esp32Id'] == esp32_payload_real['esp32Id']
        assert deserialized['timestamp'] == esp32_payload_real['timestamp']
        assert len(deserialized['readings']) == len(esp32_payload_real['readings'])

        # Validar que los valores numéricos se preservan
        for i, reading in enumerate(deserialized['readings']):
            original_reading = esp32_payload_real['readings'][i]
            assert reading['value'] == original_reading['value']
            assert reading['variableId'] == original_reading['variableId']
            assert reading['physicalId'] == original_reading['physicalId']


class TestMQTTTopicValidation:
    """Test suite para validar configuración de topics MQTT."""

    def test_sensor_readings_topic_match(self):
        """Valida que el topic configurado coincide con el esperado."""
        from FuzzyService.Infrastructure.Configuration.ExternalServicesConfiguration import get_mqtt_settings
        import os

        # Simular variable de entorno
        os.environ['MQTT_SENSOR_TOPIC_PATTERN'] = 'sensor/readings'

        # Limpiar cache de settings
        from FuzzyService.Infrastructure.Configuration import ExternalServicesConfiguration
        ExternalServicesConfiguration._mqtt_settings = None

        # Obtener settings
        settings = get_mqtt_settings()

        # Validar topic
        assert settings.sensor_topic_pattern == 'sensor/readings', \
            f"Topic debe ser 'sensor/readings', pero es '{settings.sensor_topic_pattern}'"
