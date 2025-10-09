from __future__ import annotations

from typing import List, Optional, Dict, Any

from pydantic import Field, field_validator
from medyator import Command

from FuzzyService.Domain.Common import DomainBaseModel


class StepPayload(DomainBaseModel):
    """Paso defuzzificado listo para el actuator-service.

    Estructura esperada por actuator-service:
    - outputVariable: ObjectId del control_output (string)
    - power: "ON" | "OFF" (solo para DIGITAL)
    - dutyCycle: 0-100 (solo para PWM)
    - duration: segundos
    """

    outputVariable: str = Field(
        ..., min_length=1, description="ObjectId del control_output en actuator-service"
    )
    power: Optional[str] = Field(
        None, description="ON/OFF para outputs DIGITAL"
    )
    dutyCycle: Optional[float | int] = Field(
        None, ge=0, le=100, description="0-100 para outputs PWM"
    )
    duration: float | int = Field(..., ge=0, description="Duración en segundos (0.0 para apagado instantáneo)")

    @field_validator("outputVariable")
    @classmethod
    def validate_output_variable(cls, v: str) -> str:
        if not v or not v.strip():
            raise ValueError("outputVariable no puede estar vacío")
        return v.strip()

    @field_validator("power")
    @classmethod
    def validate_power(cls, v: Optional[str]) -> Optional[str]:
        if v is not None and v not in ["ON", "OFF"]:
            raise ValueError("power debe ser 'ON' o 'OFF'")
        return v


class RoutinePayload(DomainBaseModel):
    """Rutina a ejecutar con pasos defuzzificados."""

    routineId: str = Field(..., min_length=1, max_length=100)
    steps: List[StepPayload] = Field(default_factory=list)

    @field_validator("steps")
    @classmethod
    def validate_steps(cls, v: List[StepPayload]) -> List[StepPayload]:
        if not v:
            raise ValueError("Cada rutina debe incluir al menos un paso")
        return v


class SendRoutinesToActuatorCommand(DomainBaseModel, Command):
    """Comando para enviar rutinas defuzzificadas al actuator-service.

    Estructura esperada por actuator-service (/api/commands/multi-routine):
    {
        "routines": [
            {
                "routineId": "ControlTemperatura",
                "steps": [
                    {
                        "outputVariable": "6883fff7b079309f3ba4f240",
                        "dutyCycle": 75,
                        "duration": 300
                    }
                ]
            }
        ]
    }

    El campo outputVariable es el reference_id de una FuzzyVariable de tipo "output",
    que apunta a un control_output en actuator-service.
    """

    routines: List[RoutinePayload] = Field(
        description="Lista de rutinas con pasos defuzzificados para ejecutar en actuadores",
        default_factory=list,
    )

    # Resultado del comando (se establece después de la ejecución)
    _result: Optional[bool] = None

    @field_validator("routines")
    @classmethod
    def validate_routines(cls, v: List[RoutinePayload]) -> List[RoutinePayload]:
        if not v or len(v) == 0:
            raise ValueError("Debe proporcionar al menos una rutina para enviar")
        if len(v) > 50:
            raise ValueError("No se pueden enviar más de 50 rutinas a la vez")
        return v