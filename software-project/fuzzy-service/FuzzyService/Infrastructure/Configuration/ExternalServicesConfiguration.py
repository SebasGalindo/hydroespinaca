"""Configuración de servicios externos para Infrastructure.

Este módulo maneja la configuración de conexiones a servicios externos como MQTT y ActuatorService.
"""

import os
from typing import Optional
from pydantic import BaseModel, Field


class MqttSettings(BaseModel):
    """Configuración para el cliente MQTT."""
    
    broker_host: str = Field(default="localhost", description="Host del broker MQTT")
    broker_port: int = Field(default=1883, description="Puerto del broker MQTT")
    username: Optional[str] = Field(default=None, description="Usuario MQTT")
    password: Optional[str] = Field(default=None, description="Contraseña MQTT")
    keepalive: int = Field(default=60, description="Keepalive en segundos")
    qos: int = Field(default=1, description="Quality of Service por defecto")
    client_id: str = Field(default="fuzzy-service", description="ID del cliente MQTT")
    
    # Configuración de tópicos
    sensor_topic_pattern: str = Field(default="sensor/readings", description="Patrón de tópicos de sensores")
    
    # Configuración de reconexión
    reconnect_delay: int = Field(default=5, description="Delay inicial de reconexión en segundos")
    max_reconnect_delay: int = Field(default=300, description="Delay máximo de reconexión en segundos")
    reconnect_exponential_base: float = Field(default=2.0, description="Base exponencial para backoff")
    max_reconnect_attempts: int = Field(default=10, description="Máximo número de intentos de reconexión")
    
    # TLS/SSL
    tls_enabled: bool = Field(default=False, description="Habilitar TLS")
    tls_ca_file: Optional[str] = Field(default=None, description="Archivo CA para TLS")
    tls_cert_file: Optional[str] = Field(default=None, description="Archivo de certificado para TLS")
    tls_key_file: Optional[str] = Field(default=None, description="Archivo de clave privada para TLS")


class ActuatorServiceSettings(BaseModel):
    """Configuración para el servicio de actuadores."""
    
    base_url: str = Field(default="http://localhost:8001", description="URL base del servicio de actuadores")
    timeout: int = Field(default=30, description="Timeout en segundos")
    max_retries: int = Field(default=3, description="Máximo número de reintentos")
    retry_delay: int = Field(default=1, description="Delay entre reintentos en segundos")


_mqtt_settings: Optional[MqttSettings] = None
_actuator_settings: Optional[ActuatorServiceSettings] = None


def get_mqtt_settings() -> MqttSettings:
    """Obtiene la configuración MQTT desde variables de entorno."""
    global _mqtt_settings
    if _mqtt_settings is not None:
        return _mqtt_settings
    
    _mqtt_settings = MqttSettings(
        broker_host=os.getenv("MQTT_HOST", "localhost"),
        broker_port=int(os.getenv("MQTT_PORT", "1883")),
        username=os.getenv("MQTT_USERNAME"),
        password=os.getenv("MQTT_PASSWORD"),
        keepalive=int(os.getenv("MQTT_KEEPALIVE", "60")),
        qos=int(os.getenv("MQTT_QOS", "1")),
        client_id=os.getenv("MQTT_CLIENT_ID", "fuzzy-service"),
        sensor_topic_pattern=os.getenv("MQTT_SENSOR_TOPIC_PATTERN", "sensor/readings"),
        reconnect_delay=int(os.getenv("MQTT_RECONNECT_DELAY", "5")),
        max_reconnect_delay=int(os.getenv("MQTT_MAX_RECONNECT_DELAY", "300")),
        reconnect_exponential_base=float(os.getenv("MQTT_RECONNECT_EXPONENTIAL_BASE", "2.0")),
        max_reconnect_attempts=int(os.getenv("MQTT_MAX_RECONNECT_ATTEMPTS", "10")),
        tls_enabled=os.getenv("MQTT_TLS_ENABLED", "false").lower() == "true",
        tls_ca_file=os.getenv("MQTT_TLS_CA_FILE"),
        tls_cert_file=os.getenv("MQTT_TLS_CERT_FILE"),
        tls_key_file=os.getenv("MQTT_TLS_KEY_FILE")
    )
    
    return _mqtt_settings


def get_actuator_service_settings() -> ActuatorServiceSettings:
    """Obtiene la configuración del servicio de actuadores desde variables de entorno."""
    global _actuator_settings
    if _actuator_settings is not None:
        return _actuator_settings
    
    _actuator_settings = ActuatorServiceSettings(
        base_url=os.getenv("ACTUATOR_SERVICE_URL", "http://localhost:8001"),
        timeout=int(os.getenv("ACTUATOR_SERVICE_TIMEOUT", "30")),
        max_retries=int(os.getenv("ACTUATOR_SERVICE_MAX_RETRIES", "3")),
        retry_delay=int(os.getenv("ACTUATOR_SERVICE_RETRY_DELAY", "1"))
    )
    
    return _actuator_settings
