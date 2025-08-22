"""Cliente MQTT para recibir lecturas de sensores.

Maneja la suscripción a topics MQTT y el procesamiento de mensajes
de lecturas provenientes de los ESP32.
"""

import asyncio
import json
import logging
from typing import Callable, Optional

from asyncio_mqtt import Client as MQTTClient
from application.dtos import ReadingBatch
from domain.interfaces.mqtt_client import IMQTTClient


class SensorMQTTClient(IMQTTClient):
    """Cliente MQTT para recibir lecturas de sensores.
    
    Se suscribe a topics configurables y procesa mensajes JSON
    que contienen lotes de lecturas de sensores.
    """
    
    def __init__(
        self,
        broker_host: str,
        broker_port: int = 1883,
        username: Optional[str] = None,
        password: Optional[str] = None,
        topic_pattern: str = "hydro/+/readings"
    ):
        """
        Args:
            broker_host: Hostname del broker MQTT
            broker_port: Puerto del broker MQTT
            username: Usuario para autenticación (opcional)
            password: Contraseña para autenticación (opcional)
            topic_pattern: Patrón de topics a suscribirse (ej: "hydro/+/readings")
        """
        self.broker_host = broker_host
        self.broker_port = broker_port
        self.username = username
        self.password = password
        self.topic_pattern = topic_pattern
        
        self._client: Optional[MQTTClient] = None
        self._message_handler: Optional[Callable[[ReadingBatch], None]] = None
        self._running = False
        
        logging.info(
            "SensorMQTTClient configurado: %s:%d, topic=%s",
            broker_host,
            broker_port,
            topic_pattern
        )
    
    def set_message_handler(self, handler: Callable[[ReadingBatch], None]) -> None:
        """Configura el handler para procesar mensajes recibidos.
        
        Args:
            handler: Función que será llamada con cada ReadingBatch recibido
        """
        self._message_handler = handler
        logging.info("Message handler configurado")
    
    async def connect(self) -> None:
        """Connect to MQTT broker."""
        pass  # Implementation will be in start_listening
    
    async def disconnect(self) -> None:
        """Disconnect from MQTT broker."""
        await self.stop()
    
    async def subscribe_to_topics(self, topics: list[str]) -> None:
        """Subscribe to MQTT topics."""
        # This will be handled in start_listening with the configured topic_pattern
        pass
    
    async def start_listening(self) -> None:
        """Start listening for messages."""
        await self.start()
    
    async def stop_listening(self) -> None:
        """Stop listening for messages."""
        await self.stop()
    
    async def start(self) -> None:
        """Inicia la conexión MQTT y comienza a escuchar mensajes."""
        if self._running:
            logging.warning("MQTT client ya está ejecutándose")
            return
        
        if not self._message_handler:
            raise ValueError("Message handler no configurado")
        
        self._running = True
        
        # Configurar cliente MQTT
        client_kwargs = {
            "hostname": self.broker_host,
            "port": self.broker_port,
        }
        
        if self.username and self.password:
            client_kwargs["username"] = self.username
            client_kwargs["password"] = self.password
        
        try:
            async with MQTTClient(**client_kwargs) as client:
                self._client = client
                
                # Suscribirse al topic pattern
                await client.subscribe(self.topic_pattern)
                logging.info("Suscrito a topic: %s", self.topic_pattern)
                
                # Procesar mensajes
                async for message in client.messages:
                    if not self._running:
                        break
                    
                    await self._process_message(message)
                    
        except Exception as e:
            logging.error("Error en cliente MQTT: %s", str(e))
            raise
        finally:
            self._client = None
            self._running = False
            logging.info("Cliente MQTT desconectado")
    
    async def stop(self) -> None:
        """Detiene el cliente MQTT."""
        self._running = False
        logging.info("Deteniendo cliente MQTT...")
    
    async def _process_message(self, message) -> None:
        """Procesa un mensaje MQTT recibido.
        
        Args:
            message: Mensaje MQTT recibido
        """
        try:
            # Decodificar payload JSON
            payload_str = message.payload.decode('utf-8')
            payload_data = json.loads(payload_str)
            
            # Validar y crear ReadingBatch
            batch = ReadingBatch(**payload_data)
            
            logging.debug(
                "Mensaje recibido: topic=%s, esp32=%s, readings=%d",
                message.topic,
                batch.esp32Id,
                len(batch.readings)
            )
            
            # Llamar al handler configurado
            if self._message_handler:
                await self._call_handler_safely(batch)
                
        except json.JSONDecodeError as e:
            logging.error(
                "Error al decodificar JSON del topic %s: %s",
                message.topic,
                str(e)
            )
        except Exception as e:
            logging.error(
                "Error al procesar mensaje del topic %s: %s",
                message.topic,
                str(e)
            )
    
    async def _call_handler_safely(self, batch: ReadingBatch) -> None:
        """Llama al message handler de forma segura.
        
        Args:
            batch: Lote de lecturas a procesar
        """
        try:
            if asyncio.iscoroutinefunction(self._message_handler):
                await self._message_handler(batch)
            else:
                self._message_handler(batch)
                
        except Exception as e:
            logging.error(
                "Error en message handler para esp32=%s: %s",
                batch.esp32Id,
                str(e)
            )
    
    @property
    def is_running(self) -> bool:
        """Indica si el cliente está ejecutándose."""
        return self._running