from __future__ import annotations

from typing import List, Optional, Dict, Any

from pydantic import Field, field_validator
from medyator import Command

from FuzzyService.Domain.Common import DomainBaseModel


class StepPayload(DomainBaseModel):
    """Paso defuzzificado listo para el actuator-service."""

    actuator: Dict[str, str] = Field(
        ..., description="Referencia del actuador en formato {\"$oid\": \"...\"}"
    )
    power: float | int = Field(..., ge=0, description="Potencia a aplicar (0..100 o similar)")
    duration: float | int = Field(..., gt=0, description="Duración en segundos")

    @field_validator("actuator")
    @classmethod
    def validate_actuator(cls, v: Dict[str, str]) -> Dict[str, str]:
        if not isinstance(v, dict) or "$oid" not in v or not isinstance(v["$oid"], str) or not v["$oid"].strip():
            raise ValueError("El actuador debe tener la forma {\"$oid\": \"<id>\"}")
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

    Estructura esperada:
    [
        {
            "routineId": "rutina_riego",
            "steps": [
                { "actuator": {"$oid": "pump_001"}, "power": 60, "duration": 45 }
            ]
        }
    ]
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