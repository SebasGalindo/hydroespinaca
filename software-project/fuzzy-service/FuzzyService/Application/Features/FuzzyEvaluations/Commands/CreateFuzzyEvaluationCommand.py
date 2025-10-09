from __future__ import annotations

from typing import Optional, List
from datetime import datetime

from pydantic import BaseModel, Field, PrivateAttr, field_validator
from medyator import Command

from FuzzyService.Application.Features.FuzzyEvaluations.DTOs.FuzzyEvaluationDto import (
    FuzzyEvaluationDto,
    InputValueDto,
    RuleActivationDto
)


class CreateFuzzyEvaluationCommand(BaseModel, Command):
    """Comando para crear una nueva evaluación fuzzy.
    
    Este comando permite crear evaluaciones fuzzy para pruebas,
    proporcionando datos de entrada y reglas activadas manualmente.
    """

    system_id: str = Field(
        ..., 
        min_length=1, 
        description="ID del sistema fuzzy a evaluar"
    )
    
    inputs: List[InputValueDto] = Field(
        default_factory=list,
        description="Lista de valores de entrada de sensores"
    )
    
    activated_rules: List[RuleActivationDto] = Field(
        default_factory=list,
        description="Lista de reglas activadas durante la evaluación"
    )
    
    timestamp: Optional[datetime] = Field(
        default=None,
        description="Timestamp de la evaluación (UTC). Si no se proporciona, se usa el actual"
    )

    # Resultado de la operación
    _result: Optional[FuzzyEvaluationDto] = PrivateAttr(default=None)

    @field_validator('system_id')
    @classmethod
    def validate_system_id(cls, v: str) -> str:
        """Valida que el ID del sistema no esté vacío."""
        if not v or not v.strip():
            raise ValueError('El ID del sistema no puede estar vacío')
        return v.strip()
    
    @field_validator('inputs')
    @classmethod
    def validate_inputs(cls, v: List[InputValueDto]) -> List[InputValueDto]:
        """Valida que haya al menos un valor de entrada."""
        if not v:
            raise ValueError('Debe proporcionar al menos un valor de entrada')
        return v
    
    @field_validator('activated_rules')
    @classmethod
    def validate_activated_rules(cls, v: List[RuleActivationDto]) -> List[RuleActivationDto]:
        """Valida que haya al menos una regla activada."""
        if not v:
            raise ValueError('Debe proporcionar al menos una regla activada')
        return v
