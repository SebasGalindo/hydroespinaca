"""Handler de mensajes MQTT para procesar lecturas de sensores.

Este handler procesa mensajes MQTT y los convierte en comandos para el motor fuzzy.
"""

import logging
from typing import Dict, Any, List, Optional
from datetime import datetime, timezone

from FuzzyService.Application.Features.SensorProcessing.Commands.ProcessSensorReadingsCommand import (
    ProcessSensorReadingsCommand,
    SensorReading
)
from medyator import Medyator
from kink import di

_logger = logging.getLogger(__name__)


class MqttMessageHandler:
    """Handler de mensajes MQTT para procesar lecturas de sensores.
    
    Responsabilidades:
    - Recibir mensajes validados del MqttSubscriber
    - Validar estructura y tipos de datos de las lecturas (timestamp, sensor_id, value)
    - Transformar lecturas en formato estándar para el motor fuzzy
    - Enviar comando ProcessSensorReadings al FuzzyEngineService vía mediator
    - Manejar errores de validación y transformación sin afectar otros mensajes
    - Registrar métricas: lecturas procesadas, transformadas, errores de validación
    - No acceder directamente a BD ni servicios externos (solo mediator)
    """
    
    def __init__(self, mediator: Optional[Medyator] = None):
        self._mediator = mediator or di[Medyator]
        
        # Métricas
        self._messages_handled = 0
        self._readings_processed = 0
        self._readings_failed = 0
        self._validation_errors = 0
    
    async def handle(self, payload: Dict[str, Any]) -> None:
        """Maneja un mensaje MQTT procesando las lecturas de sensores.
        
        Args:
            payload: Payload del mensaje MQTT ya validado por MqttSubscriber
        """
        try:
            _logger.info(f"Procesando mensaje MQTT con {len(payload.get('readings', {}))} lecturas")
            
            # Extraer metadatos
            metadata = payload.get('_metadata', {})
            esp32_id = metadata.get('esp32_id')
            received_at = metadata.get('received_at')
            
            # Validar y transformar lecturas
            sensor_readings = await self._transform_readings(
                payload['readings'],
                payload['timestamp'],
                esp32_id,
                received_at
            )
            
            if not sensor_readings:
                _logger.warning("No se pudieron procesar lecturas válidas del mensaje")
                return
            
            # Crear comando para el motor fuzzy
            command = ProcessSensorReadingsCommand(
                readings=sensor_readings,
                batch_timestamp=payload['timestamp'],
                esp32_id=esp32_id
            )
            
            # Enviar comando al mediator
            _logger.info(f"Enviando {len(sensor_readings)} lecturas al motor fuzzy")
            await self._mediator.send(command)
            
            # Verificar el resultado a través del campo result del comando
            if command.result:
                _logger.info(f"Lecturas procesadas exitosamente por el motor fuzzy")
                _logger.info(f"Resultado: {command.result}")
                self._readings_processed += len(sensor_readings)
            else:
                _logger.error(f"Error al procesar lecturas en el motor fuzzy")
                self._readings_failed += len(sensor_readings)
            
            self._messages_handled += 1
            
        except Exception as e:
            _logger.error(f"Error al manejar mensaje MQTT: {e}")
            self._readings_failed += len(payload.get('readings', {}))
    
    async def _transform_readings(
        self,
        readings,
        timestamp: str,
        esp32_id: str,
        received_at: str
    ) -> List[SensorReading]:
        """Transforma las lecturas del payload en objetos SensorReading.

        Args:
            readings: Lista de objetos con variableId y value, o diccionario sensor_id -> valor
            timestamp: Timestamp del batch
            esp32_id: ID del ESP32 que envió las lecturas
            received_at: Timestamp de cuando se recibió el mensaje

        Returns:
            Lista de objetos SensorReading válidos
        """
        sensor_readings = []
        
        # Formato lista ESP32: [{'variableId': '...', 'value': 19}, ...]
        if isinstance(readings, list):
            for reading_obj in readings:
                sensor_id = reading_obj.get('variableId')
                value = reading_obj.get('value')
                self._process_single_reading(sensor_readings, sensor_id, value, timestamp, esp32_id, received_at)
        else:
            # Formato diccionario: {'sensor_id': 123, 'sensor_id2': 456}
            for sensor_id, value in readings.items():
                self._process_single_reading(sensor_readings, sensor_id, value, timestamp, esp32_id, received_at)
        
        _logger.info(f"Transformadas {len(sensor_readings)} lecturas válidas de {len(readings)} totales")
        return sensor_readings
    
    def _process_single_reading(self, sensor_readings, sensor_id, value, timestamp, esp32_id, received_at):
         """Procesa una sola lectura de sensor."""
         try:
             # Validar sensor_id
             if not isinstance(sensor_id, str) or not sensor_id.strip():
                 _logger.warning(f"Sensor ID inválido: {sensor_id}")
                 self._validation_errors += 1
                 return
             
             # Validar y convertir valor
             if not isinstance(value, (int, float)):
                 try:
                     value = float(value)
                 except (ValueError, TypeError):
                     _logger.warning(f"Valor inválido para sensor {sensor_id}: {value}")
                     self._validation_errors += 1
                     return
             
             # Validar que el valor sea finito
             if not (isinstance(value, (int, float)) and 
                    not (value != value or value == float('inf') or value == float('-inf'))):
                 _logger.warning(f"Valor no finito para sensor {sensor_id}: {value}")
                 self._validation_errors += 1
                 return
             
             # Crear objeto SensorReading
             sensor_reading = SensorReading(
                 sensor_id=sensor_id.strip(),
                 value=float(value),
                 timestamp=timestamp,
                 metadata={
                     'esp32_id': esp32_id,
                     'received_at': received_at,
                     'processed_at': datetime.now(timezone.utc)
                 }
             )
             
             sensor_readings.append(sensor_reading)
             
             _logger.debug(f"Lectura transformada: {sensor_id} = {value}")
             
         except Exception as e:
             _logger.error(f"Error al transformar lectura del sensor {sensor_id}: {e}")
             self._validation_errors += 1
    
    @property
    def metrics(self) -> Dict[str, int]:
        """Retorna métricas del handler."""
        return {
            'messages_handled': self._messages_handled,
            'readings_processed': self._readings_processed,
            'readings_failed': self._readings_failed,
            'validation_errors': self._validation_errors
        }
