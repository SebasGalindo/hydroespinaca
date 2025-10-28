"""Suscriptor MQTT para gestionar la suscripción a tópicos y enrutamiento de mensajes.

Este suscriptor gestiona la suscripción a tópicos y el enrutamiento de mensajes a handlers.
"""

import asyncio
import json
import logging
import re
from typing import Optional, List, Callable, Awaitable
from datetime import datetime, timezone

from aiomqtt.client import Message

from .MqttClient import MqttClient
from .MqttMessageHandler import MqttMessageHandler
from FuzzyService.Infrastructure.Configuration.ExternalServicesConfiguration import (
    get_mqtt_settings,
    MqttSettings
)

_logger = logging.getLogger(__name__)


class MqttSubscriber:
    """Suscriptor MQTT para gestionar la suscripción a tópicos y enrutamiento de mensajes.
    
    Responsabilidades:
    - Suscribirse a tópicos de sensores: 'sensor/+/batch'
    - Filtrar y validar topics y payloads (JSON)
    - Enviar cada mensaje válido al MqttMessageHandler para su procesamiento
    - Configurar QoS por tipo de sensor (por defecto 1) según criticidad
    - Manejar reintentos de suscripción y reconexión
    - Registrar métricas: mensajes procesados, descartados, latencia, errores
    - No publicar nunca (solo lectura), sin side-effects fuera del handler
    """
    
    def __init__(
        self,
        mqtt_client: Optional[MqttClient] = None,
        message_handler: Optional[MqttMessageHandler] = None,
        settings: Optional[MqttSettings] = None
    ):
        self._settings = settings or get_mqtt_settings()
        self._mqtt_client = mqtt_client or MqttClient(self._settings)
        self._message_handler = message_handler or MqttMessageHandler()
        self._is_running = False
        self._subscribed_topics: List[str] = []
        
        # Métricas
        self._messages_processed = 0
        self._messages_discarded = 0
        self._messages_failed = 0
        
        # Compilar patrón de tópico para validación
        self._topic_pattern = self._compile_topic_pattern(self._settings.sensor_topic_pattern)
    
    def _compile_topic_pattern(self, pattern: str) -> re.Pattern:
        """Compila el patrón de tópico MQTT a regex para validación.
        
        Convierte patrones MQTT como 'sensor/+/batch' a regex como '^sensor/[^/]+/batch$'
        """
        # Escapar caracteres especiales de regex excepto + y #
        escaped = re.escape(pattern)
        
        # Reemplazar + con [^/]+ (uno o más caracteres que no sean /)
        escaped = escaped.replace(r'\+', '[^/]+')
        
        # Reemplazar # con .* (cualquier carácter, incluyendo /)
        escaped = escaped.replace(r'\#', '.*')
        
        # Anclar al inicio y final
        regex_pattern = f'^{escaped}$'
        
        return re.compile(regex_pattern)
    
    def _is_valid_topic(self, topic: str) -> bool:
        """Valida si un tópico coincide con el patrón esperado."""
        return bool(self._topic_pattern.match(topic))
    
    def _extract_esp32_id(self, topic: str) -> Optional[str]:
        """Extrae el ESP32 ID del tópico.
        
        Para tópicos como 'sensor/esp32_001/batch', extrae 'esp32_001'
        """
        parts = topic.split('/')
        if len(parts) >= 2 and parts[0] == 'sensor':
            return parts[1]
        return None
    
    def _validate_and_parse_message(self, message: Message) -> Optional[dict]:
        """Valida y parsea un mensaje MQTT.
        
        Returns:
            Dict con el payload parseado si es válido, None si es inválido
        """
        try:
            # Validar tópico
            topic = message.topic.value
            if not self._is_valid_topic(topic):
                _logger.warning(f"Tópico inválido recibido: {topic}")
                self._messages_discarded += 1
                return None
            
            # Decodificar payload
            payload_str = message.payload.decode('utf-8')
            payload = json.loads(payload_str)
            
            # Validar estructura básica del payload
            if not isinstance(payload, dict):
                _logger.warning(f"Payload no es un objeto JSON válido: {payload_str}")
                self._messages_discarded += 1
                return None
            
            # Validar campos requeridos
            if 'timestamp' not in payload or 'readings' not in payload:
                _logger.warning(f"Payload no tiene campos requeridos (timestamp, readings): {payload}")
                self._messages_discarded += 1
                return None

            if not isinstance(payload['readings'], list):
                _logger.warning(f"Campo 'readings' no es una lista: {payload['readings']}")
                self._messages_discarded += 1
                return None

            # Extraer ESP32 ID del payload (primero) o del tópico (fallback)
            esp32_id = payload.get('esp32Id') or self._extract_esp32_id(topic)
            if not esp32_id:
                _logger.warning(f"No se pudo extraer ESP32 ID del payload ni del tópico: {topic}")
                self._messages_discarded += 1
                return None
            
            # Agregar metadatos
            payload['_metadata'] = {
                'topic': topic,
                'esp32_id': esp32_id,
                'received_at': datetime.now(timezone.utc),
                'qos': message.qos
            }
            
            return payload
            
        except json.JSONDecodeError as e:
            _logger.warning(f"Error al decodificar JSON del mensaje: {e}")
            self._messages_discarded += 1
            return None
        except UnicodeDecodeError as e:
            _logger.warning(f"Error al decodificar UTF-8 del mensaje: {e}")
            self._messages_discarded += 1
            return None
        except Exception as e:
            _logger.error(f"Error inesperado al validar mensaje: {e}")
            self._messages_failed += 1
            return None
    
    async def start(self) -> None:
        """Inicia el suscriptor MQTT."""
        if self._is_running:
            _logger.warning("El suscriptor MQTT ya está ejecutándose")
            return
        
        _logger.info("Iniciando suscriptor MQTT...")
        
        try:
            self._is_running = True
            _logger.info(f"Suscriptor MQTT iniciado, escuchando en '{self._settings.sensor_topic_pattern}'")
            
            # Procesar mensajes usando el nuevo patrón de context manager
            await self._process_messages()
            
        except Exception as e:
            _logger.error(f"Error al iniciar suscriptor MQTT: {e}")
            self._is_running = False
            raise
    
    def stop(self) -> None:
        """Detiene el suscriptor MQTT.

        Convertido a síncrono: No realiza operaciones I/O ni llamadas asíncronas.
        Solo actualiza el flag _is_running para señalar al loop que debe detenerse.
        """
        if not self._is_running:
            return

        _logger.info("Deteniendo suscriptor MQTT...")

        try:
            self._is_running = False
            _logger.info("Suscriptor MQTT detenido")

        except Exception as e:
            _logger.error(f"Error al detener suscriptor MQTT: {e}")
    
    def stop_subscription(self) -> None:
        """Detiene la suscripción MQTT (alias síncrono de stop()).

        Convertido a síncrono: Simplemente delega a stop() que también es síncrono.
        No hay operaciones asíncronas involucradas en el proceso de detención.
        """
        self.stop()
    
    async def _process_messages(self) -> None:
        """Procesa mensajes MQTT de forma continua."""
        while self._is_running:
            try:
                async for message in self._mqtt_client.subscribe_and_listen(self._settings.sensor_topic_pattern):
                    if not self._is_running:
                        break
                    
                    await self._handle_message(message)
                        
            except Exception as e:
                _logger.error(f"Error en el procesamiento de mensajes MQTT: {e}")
                
                if self._is_running:
                    # Esperar antes de reintentar
                    await asyncio.sleep(5)
                else:
                    break
    
    async def _handle_message(self, message: Message) -> None:
        """Maneja un mensaje MQTT individual."""
        try:
            # Validar y parsear mensaje
            parsed_payload = self._validate_and_parse_message(message)
            
            if parsed_payload is None:
                return  # Mensaje inválido, ya se registró el error
            
            # Log del mensaje recibido
            esp32_id = parsed_payload['_metadata']['esp32_id']
            readings_count = len(parsed_payload['readings'])
            
            _logger.info(
                f"Mensaje recibido de ESP32 '{esp32_id}': {readings_count} lecturas "
                f"en timestamp {parsed_payload['timestamp']}"
            )
            
            # Enviar al handler para procesamiento
            await self._message_handler.handle(parsed_payload)
            
            self._messages_processed += 1
            
        except Exception as e:
            _logger.error(f"Error al manejar mensaje MQTT: {e}")
            self._messages_failed += 1
    
    @property
    def is_running(self) -> bool:
        """Indica si el suscriptor está ejecutándose."""
        return self._is_running
    
    @property
    def metrics(self) -> dict:
        """Retorna métricas del suscriptor."""
        return {
            'messages_processed': self._messages_processed,
            'messages_discarded': self._messages_discarded,
            'messages_failed': self._messages_failed,
            'is_running': self._is_running,
            'subscribed_topics': self._subscribed_topics.copy()
        }
