"""Configuración de la aplicación.

Utiliza Pydantic Settings para cargar configuración desde variables
de entorno con valores por defecto sensatos.
"""

from typing import Optional

from pydantic import ConfigDict, Field
from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    """Configuración principal de la aplicación.
    
    Todas las configuraciones pueden ser sobrescritas via variables de entorno
    usando el prefijo FUZZY_.
    """
    
    # ===== Configuración del servidor =====
    host: str = Field(default="0.0.0.0", description="Host del servidor FastAPI")
    port: int = Field(default=8000, description="Puerto del servidor FastAPI")
    debug: bool = Field(default=False, description="Modo debug")
    
    # ===== Configuración MQTT =====
    mqtt_broker_host: str = Field(default="localhost", description="Host del broker MQTT")
    mqtt_broker_port: int = Field(default=1883, description="Puerto del broker MQTT")
    mqtt_username: Optional[str] = Field(default=None, description="Usuario MQTT")
    mqtt_password: Optional[str] = Field(default=None, description="Contraseña MQTT")
    mqtt_topic_pattern: str = Field(
        default="hydro/+/readings",
        description="Patrón de topics MQTT para lecturas"
    )
    
    # ===== Configuración del actuator-service =====
    actuator_service_url: str = Field(
        default="http://localhost:8080",
        description="URL base del actuator-service"
    )
    actuator_service_api_key: str = Field(
        default="dev-api-key",
        description="API key para autenticación con actuator-service"
    )
    actuator_service_timeout: float = Field(
        default=10.0,
        description="Timeout en segundos para requests al actuator-service"
    )
    
    # ===== Configuración de seguridad JWT =====
    jwt_secret_key: Optional[str] = Field(
        default=None,
        description="Clave secreta para validación JWT (opcional para desarrollo)"
    )
    jwt_algorithm: str = Field(default="HS256", description="Algoritmo JWT")
    jwt_issuer: str = Field(default="auth-service", description="Issuer esperado en JWT")
    jwt_audience: str = Field(default="fuzzy-api", description="Audience esperado en JWT")
    
    # ===== Configuración de lógica difusa =====
    hysteresis_delta_percent: float = Field(
        default=5.0,
        description="Delta mínimo en % para evitar oscilaciones (histeresis)"
    )
    cooldown_seconds: int = Field(
        default=10,
        description="Tiempo de cooldown en segundos entre comandos por actuador"
    )
    ramp_step_seconds: int = Field(
        default=5,
        description="Intervalo en segundos entre pasos del descenso gradual"
    )
    ramp_min_step_percent: float = Field(
        default=5.0,
        description="Paso mínimo en % para el descenso gradual"
    )
    
    # ===== Configuración de MongoDB =====
    mongo_connection_string: str = Field(
        default="mongodb://localhost:27017",
        description="Cadena de conexión a MongoDB"
    )
    mongo_database: str = Field(
        default="fuzzy_service",
        description="Nombre de la base de datos MongoDB"
    )
    mongo_timeout: int = Field(
        default=10000,
        description="Timeout de conexión a MongoDB en milisegundos"
    )
    
    # ===== Configuración de logging =====
    log_level: str = Field(default="INFO", description="Nivel de logging")
    log_format: str = Field(
        default="json",
        description="Formato de logs: 'json' o 'text'"
    )
    
    model_config = ConfigDict(
        env_prefix="FUZZY_",
        case_sensitive=False,
        env_file=".env",
        env_file_encoding="utf-8"
    )


# Instancia global de configuración
settings = Settings()