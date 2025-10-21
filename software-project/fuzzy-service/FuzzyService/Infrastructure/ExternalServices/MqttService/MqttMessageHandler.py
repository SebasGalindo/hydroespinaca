import logging
from typing import Dict, Any, List, Optional
from datetime import datetime

from FuzzyService.Application.Features.SensorProcessing.Commands.ProcessSensorReadingsCommand import (
    ProcessSensorReadingsCommand,
    SensorReading
)
from medyator import Medyator
from kink import di

_logger = logging.getLogger(__name__)


class MqttMessageHandler:

    def __init__(self, mediator: Optional[Medyator] = None):
        self._mediator = mediator or di[Medyator]
        self._messages_handled = 0
        self._readings_processed = 0
        self._validation_errors = 0
    
    async def handle(self, payload: Dict[str, Any]) -> None:
        if not all(k in payload for k in ["esp32Id", "timestamp", "readings"]):
            _logger.warning("Payload inválido recibido desde MQTT: faltan campos base.")
            self._validation_errors += 1
            return

        if not isinstance(payload["readings"], list):
            _logger.warning("Campo 'readings' debe ser una lista")
            self._validation_errors += 1
            return

        esp32_id = payload["esp32Id"]
        timestamp = payload["timestamp"]
        readings = payload["readings"]

        _logger.info(f"Procesando mensaje MQTT con {len(readings)} lecturas")

        sensor_readings = []
        for reading in readings:
            if not all(k in reading for k in ["physicalId", "variableCode", "value"]):
                _logger.warning(f"Lectura incompleta ignorada: {reading}")
                self._validation_errors += 1
                continue

            variable_code = reading["variableCode"]
            value = reading["value"]

            sensor_readings.append(SensorReading(
                sensor_id=variable_code,
                value=float(value),
                timestamp=timestamp
            ))

        if not sensor_readings:
            _logger.warning("No se procesaron lecturas válidas")
            return

        command = ProcessSensorReadingsCommand(
            readings=sensor_readings,
            batch_timestamp=timestamp,
            esp32_id=esp32_id
        )

        _logger.info(f"Enviando {len(sensor_readings)} lecturas al motor fuzzy")
        await self._mediator.send(command)

        if command.result:
            _logger.info(f"Lecturas procesadas exitosamente: {command.result}")
            self._readings_processed += len(sensor_readings)
        else:
            _logger.warning("Procesamiento completado sin resultado explícito")

        self._messages_handled += 1

    @property
    def metrics(self) -> Dict[str, int]:
        return {
            'messages_handled': self._messages_handled,
            'readings_processed': self._readings_processed,
            'validation_errors': self._validation_errors
        }
