"""DTOs (Data Transfer Objects) para la capa de aplicación.

Estos objetos definen los contratos de entrada y salida para los casos de uso,
alineados con los servicios externos (MQTT, actuator-service).
"""

from __future__ import annotations

from datetime import datetime
from typing import Any, Dict, List, Optional

from pydantic import BaseModel


# ===== DTOs de entrada (MQTT) =====
class ReadingInput(BaseModel):
    """Lectura individual de un sensor.
    
    Alineado con el formato esperado desde los ESP32 vía MQTT.
    """
    physicalId: Optional[str] = None  # ID físico del sensor (opcional)
    variableId: str  # ID de la variable que mide este sensor
    value: float     # valor medido


class ReadingBatch(BaseModel):
    """Lote de lecturas de un ESP32.
    
    Formato del payload MQTT que llega del topic hydro/+/readings.
    """
    esp32Id: str
    timestamp: datetime
    readings: List[ReadingInput]


# ===== DTOs de salida (actuator-service) =====
class CommandMetadataDto(BaseModel):
    """Metadata del comando enviado al actuator-service.
    
    Información adicional para trazabilidad y debugging.
    """
    Source: str  # "fuzzy-service"
    FuzzyRule: Optional[str] = None  # descripción de la regla aplicada
    Inputs: Optional[Dict[str, float]] = None  # snapshot de las entradas


class CreateCommandDto(BaseModel):
    """DTO para crear comandos en el actuator-service.
    
    Debe coincidir exactamente con el contrato del actuator-service.
    """
    ActuatorId: str
    Esp32Id: str
    Action: str  # formato: "pwm:TARGET" (ej: "pwm:70")
    DurationMs: Optional[int] = None  # duración en milisegundos
    Trigger: str  # "Fuzzy"
    RoutineId: Optional[str] = None
    RoutineStepOrder: Optional[int] = None
    Metadata: Optional[CommandMetadataDto] = None


# ===== DTOs para API REST =====
class VariableCreateDto(BaseModel):
    """DTO para crear variables vía API."""
    id: str
    name: str
    unit: Optional[str] = None
    description: Optional[str] = None


class RoutineCreateDto(BaseModel):
    """DTO para crear rutinas vía API."""
    id: str
    name: str
    active: bool = True
    threshold_rules: List[Dict[str, Any]] = []  # será validado en el caso de uso
    outputs: List[Dict[str, Any]] = []         # será validado en el caso de uso


class RoutineStateUpdateDto(BaseModel):
    """DTO para actualizar estado de rutina."""
    active: bool


class SimulationRequest(BaseModel):
    """DTO de solicitud para simulación."""
    routineId: str
    scenarios: List[Dict[str, float]]  # Escenarios de simulación con variables y valores


class SimulateResponseDto(BaseModel):
    """DTO de respuesta para simulación."""
    plans: List[Dict[str, Any]]  # OutputPlan serializado


# ===== DTOs para gestión de reglas difusas =====
class FuzzyConditionDto(BaseModel):
    """DTO para condiciones difusas."""
    variable_id: str
    fuzzy_set_name: str
    weight: float = 1.0


class FuzzyRuleCreateDto(BaseModel):
    """DTO para crear reglas difusas."""
    id: str
    name: str
    description: Optional[str] = None
    conditions: List[FuzzyConditionDto]
    operator: str = "and"  # "and" o "or"
    output_variable_id: str
    output_fuzzy_set_name: str
    priority: int = 1
    active: bool = True


class FuzzyRuleResponseDto(BaseModel):
    """DTO de respuesta para reglas difusas."""
    id: str
    name: str
    description: Optional[str] = None
    conditions_text: str
    output_variable_id: str
    output_fuzzy_set_name: str
    priority: int
    active: bool


class FuzzyVariableInfoDto(BaseModel):
    """DTO para información de variables difusas."""
    name: str
    universe_range: List[float]
    sets: List[str]


class ActuatorStatusDto(BaseModel):
    """DTO para estado de actuadores."""
    actuator_id: str
    last_command_time: Optional[datetime] = None
    last_target: Optional[int] = None
    in_cooldown: bool = False
    cooldown_remaining_seconds: Optional[int] = None
    hysteresis_active: bool = False


class SystemMetricsDto(BaseModel):
    """DTO para métricas del sistema."""
    active_rules_count: int
    total_evaluations: int
    commands_sent_today: int
    average_response_time_ms: float
    system_uptime_seconds: int


class SystemConfigDto(BaseModel):
    """DTO para configuración del sistema."""
    hysteresis_delta: float = 5.0
    cooldown_seconds: int = 30
    max_evaluations_per_minute: int = 60
    log_level: str = "INFO"
    mqtt_enabled: bool = True
    actuator_service_enabled: bool = True
    fuzzy_engine_debug: bool = False


class SystemConfigUpdateDto(BaseModel):
    """DTO para actualizar configuración del sistema."""
    hysteresis_delta: Optional[float] = None
    cooldown_seconds: Optional[int] = None
    max_evaluations_per_minute: Optional[int] = None
    log_level: Optional[str] = None
    mqtt_enabled: Optional[bool] = None
    actuator_service_enabled: Optional[bool] = None
    fuzzy_engine_debug: Optional[bool] = None


# ===== DTOs para CRUD de sistemas difusos =====
class FuzzySystemCreateDto(BaseModel):
    """DTO para crear sistemas difusos."""
    name: str
    description: Optional[str] = None
    version: str = "1.0.0"
    status: str = "draft"  # draft, published


class FuzzySystemResponseDto(BaseModel):
    """DTO de respuesta para sistemas difusos."""
    id: str
    name: str
    description: Optional[str] = None
    version: str
    status: str
    created_at: datetime
    updated_at: datetime
    variables_count: int
    rules_count: int
    routines_count: int


class FuzzySystemUpdateDto(BaseModel):
    """DTO para actualizar sistemas difusos."""
    name: Optional[str] = None
    description: Optional[str] = None


class FuzzySystemExportDto(BaseModel):
    """DTO para exportar sistemas difusos."""
    system: Dict[str, Any]
    variables: List[Dict[str, Any]]
    rules: List[Dict[str, Any]]
    routines: List[Dict[str, Any]]
    export_timestamp: datetime


# ===== DTOs para CRUD de variables y términos =====
class FuzzySetCreateDto(BaseModel):
    """DTO para crear conjuntos difusos (términos)."""
    name: str
    membership_type: str  # triangular, trapezoidal, gaussian, sigmoid
    parameters: List[float]
    description: Optional[str] = None


class FuzzySetResponseDto(BaseModel):
    """DTO de respuesta para conjuntos difusos."""
    id: str
    name: str
    membership_type: str
    parameters: List[float]
    description: Optional[str] = None
    variable_id: str


class VariableResponseDto(BaseModel):
    """DTO de respuesta para variables."""
    id: str
    name: str
    unit: Optional[str] = None
    description: Optional[str] = None
    min_value: Optional[float] = None
    max_value: Optional[float] = None
    fuzzy_sets: List[FuzzySetResponseDto] = []
    system_id: Optional[str] = None


class VariableUpdateDto(BaseModel):
    """DTO para actualizar variables."""
    name: Optional[str] = None
    unit: Optional[str] = None
    description: Optional[str] = None
    min_value: Optional[float] = None
    max_value: Optional[float] = None


class TermCreateDto(BaseModel):
    """DTO para crear términos (alias para FuzzySetCreateDto)."""
    name: str
    membership_type: str
    parameters: List[float]
    description: Optional[str] = None
    variable_id: str


class TermResponseDto(BaseModel):
    """DTO de respuesta para términos."""
    id: str
    name: str
    membership_type: str
    parameters: List[float]
    description: Optional[str] = None
    variable_id: str
    variable_name: str


class TermUpdateDto(BaseModel):
    """DTO para actualizar términos."""
    name: Optional[str] = None
    membership_type: Optional[str] = None
    parameters: Optional[List[float]] = None
    description: Optional[str] = None


# ===== DTOs para CRUD de rutinas =====
class RoutineResponseDto(BaseModel):
    """DTO de respuesta para rutinas."""
    id: str
    name: str
    description: Optional[str] = None
    active: bool
    fuzzy_rules_count: int
    threshold_rules_count: int
    outputs_count: int
    input_variables: List[str]
    output_variables: List[str]
    system_id: Optional[str] = None


class RoutineUpdateDto(BaseModel):
    """DTO para actualizar rutinas."""
    name: Optional[str] = None
    description: Optional[str] = None
    active: Optional[bool] = None


# ===== DTOs para CRUD de mapeos de actuadores =====
class ActuatorMappingCreateDto(BaseModel):
    """DTO para crear mapeos de actuadores."""
    output_name: str
    actuatorId: str
    esp32Id: str
    actuator_type: str = "variable"  # "on_off" o "variable"
    on_threshold: float = 50.0  # Umbral para actuadores on/off
    description: Optional[str] = None


class ActuatorMappingResponseDto(BaseModel):
    """DTO de respuesta para mapeos de actuadores."""
    id: str
    output_name: str
    actuatorId: str
    esp32Id: str
    actuator_type: str = "variable"  # "on_off" o "variable"
    on_threshold: float = 50.0  # Umbral para actuadores on/off
    description: Optional[str] = None
    routine_id: Optional[str] = None
    created_at: datetime
    updated_at: datetime


class ActuatorMappingUpdateDto(BaseModel):
    """DTO para actualizar mapeos de actuadores."""
    output_name: Optional[str] = None
    actuatorId: Optional[str] = None
    esp32Id: Optional[str] = None
    actuator_type: Optional[str] = None
    on_threshold: Optional[float] = None
    description: Optional[str] = None