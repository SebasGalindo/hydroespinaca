"""Cliente MQTT para suscripción a mensajes de sensores.

Este cliente configura y mantiene una conexión de SOLO LECTURA al broker MQTT usando asyncio-mqtt.
"""

import asyncio
import logging
import ssl
from typing import Optional, AsyncGenerator
from contextlib import asynccontextmanager

import aiomqtt
from aiomqtt import Client, MqttError
from aiomqtt.client import Message

from FuzzyService.Infrastructure.Configuration.ExternalServicesConfiguration import (
    get_mqtt_settings,
    MqttSettings
)

_logger = logging.getLogger(__name__)


class MqttClient:
    """Cliente MQTT para suscripción a tópicos de sensores.
    
    Responsabilidades:
    - Configurar conexión (host, puerto, credenciales, TLS opcional) y keep-alive
    - Manejar reconexión automática con backoff exponencial
    - Suscribirse a los topics de sensores definidos por configuración
    - Exponer un stream asíncrono de mensajes
    - Logging detallado de conexión, reconexiones y errores
    - IMPORTANTE: Sin publicar mensajes (no se usa publish), SOLO escucha
    """
    
    def __init__(self, settings: Optional[MqttSettings] = None):
        self._settings = settings or get_mqtt_settings()
        self._reconnect_attempts = 0
        self._client: Optional[Client] = None
        self._is_connected = False
        
    def _create_tls_context(self) -> Optional[ssl.SSLContext]:
        """Crea el contexto TLS si está habilitado."""
        if not self._settings.tls_enabled:
            return None
            
        tls_context = ssl.create_default_context(ssl.Purpose.SERVER_AUTH)
        # Enforce minimum TLS version to prevent weak protocols (TLS 1.0/1.1)
        tls_context.minimum_version = ssl.TLSVersion.TLSv1_2
        if self._settings.tls_ca_file:
            tls_context.load_verify_locations(self._settings.tls_ca_file)
        if self._settings.tls_cert_file and self._settings.tls_key_file:
            tls_context.load_cert_chain(self._settings.tls_cert_file, self._settings.tls_key_file)
        return tls_context
    
    @asynccontextmanager
    async def get_client(self) -> AsyncGenerator[Client, None]:
        """Context manager que proporciona un cliente MQTT conectado."""
        tls_context = self._create_tls_context()
        
        try:
            async with Client(
                hostname=self._settings.broker_host,
                port=self._settings.broker_port,
                username=self._settings.username,
                password=self._settings.password,
                keepalive=self._settings.keepalive,
                identifier=self._settings.client_id,
                tls_context=tls_context
            ) as client:
                self._reconnect_attempts = 0
                _logger.info(
                    f"Conectado al broker MQTT {self._settings.broker_host}:{self._settings.broker_port} "
                    f"con client_id '{self._settings.client_id}'"
                )
                yield client
                
        except Exception as e:
            _logger.error(f"Error al conectar con el broker MQTT: {e}")
            raise
    
    async def subscribe_and_listen(self, topic: str, qos: Optional[int] = None) -> AsyncGenerator[Message, None]:
        """Async generator que se conecta, suscribe a un tópico y proporciona un stream de mensajes."""
        qos_level = qos if qos is not None else self._settings.qos
        
        async with self.get_client() as client:
            try:
                await client.subscribe(topic, qos=qos_level)
                _logger.info(f"Suscrito al tópico '{topic}' con QoS {qos_level}")
                
                async for message in client.messages:
                    yield message
                        
            except MqttError as e:
                _logger.error(f"Error en MQTT: {e}")
                raise
            except Exception as e:
                _logger.error(f"Error al suscribirse al tópico '{topic}': {e}")
                raise
    
    def _calculate_reconnect_delay(self) -> float:
        """Calcula el delay de reconexión usando backoff exponencial."""
        if self._reconnect_attempts >= self._settings.max_reconnect_attempts:
            return self._settings.max_reconnect_delay

        delay = min(
            self._settings.reconnect_delay * (self._settings.reconnect_exponential_base ** self._reconnect_attempts),
            self._settings.max_reconnect_delay
        )

        return delay
    
    async def reconnect_with_backoff(self) -> bool:
        """Intenta reconectar con backoff exponencial.
        
        Returns:
            True si la reconexión fue exitosa, False si se agotaron los intentos.
        """
        while self._reconnect_attempts < self._settings.max_reconnect_attempts:
            self._reconnect_attempts += 1
            delay = self._calculate_reconnect_delay()
            
            _logger.info(
                f"Intento de reconexión {self._reconnect_attempts}/{self._settings.max_reconnect_attempts} "
                f"en {delay:.1f} segundos..."
            )
            
            await asyncio.sleep(delay)
            
            try:
                await self.connect()
                return True
            except Exception as e:
                _logger.warning(f"Fallo en intento de reconexión {self._reconnect_attempts}: {e}")
        
        _logger.error(f"Se agotaron los {self._settings.max_reconnect_attempts} intentos de reconexión")
        return False
    
    def disconnect(self) -> None:
        """Desconecta el cliente MQTT de forma segura."""
        try:
            # Simplemente marcar como desconectado
            # El context manager de aiomqtt se encarga de la limpieza
            self._is_connected = False
            _logger.info("Cliente MQTT desconectado")
        except Exception as e:
            _logger.warning(f"Error al desconectar cliente MQTT: {e}")
        finally:
            self._client = None
    
    @property
    def is_connected(self) -> bool:
        """Indica si el cliente está conectado."""
        return self._is_connected
    
    @property
    def settings(self) -> MqttSettings:
        """Configuración del cliente MQTT."""
        return self._settings
