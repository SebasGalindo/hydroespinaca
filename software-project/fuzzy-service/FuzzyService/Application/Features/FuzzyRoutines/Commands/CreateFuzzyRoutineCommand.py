from __future__ import annotations

from typing import List, Optional
from pydantic import BaseModel, Field, PrivateAttr, field_validator, ConfigDict
from medyator import Command

from FuzzyService.Application.Features.FuzzyRoutines.DTOs.FuzzyRoutineDto import FuzzyRoutineDto, RoutineStepDto


class CreateFuzzyRoutineCommand(BaseModel, Command):
    """Comando para crear una nueva rutina difusa."""
    
    model_config = ConfigDict(validate_assignment=True, extra="forbid")
    
    routine_name: str = Field(
        ..., 
        min_length=1, 
        max_length=100,
        description="Nombre de la rutina difusa"
    )
    steps: List[RoutineStepDto] = Field(
        default_factory=list,
        description="Lista de pasos de la rutina"
    )
    
    _result: Optional[FuzzyRoutineDto] = PrivateAttr(default=None)

    @field_validator('routine_name')
    @classmethod
    def validate_routine_name(cls, v: str) -> str:
        """Valida que el nombre de la rutina no esté vacío."""
        v = v.strip()
        if not v:
            raise ValueError('El nombre de la rutina no puede estar vacío')
        return v

    @field_validator('steps')
    @classmethod
    def validate_steps(cls, v: List[RoutineStepDto]) -> List[RoutineStepDto]:
        """Valida que no haya step_ids duplicados."""
        if v:
            step_ids = [step.step_id for step in v]
            if len(step_ids) != len(set(step_ids)):
                raise ValueError('No puede haber step_ids duplicados en la rutina')
            
            # Validar que los step_ids sean secuenciales desde 1
            sorted_ids = sorted(step_ids)
            expected_ids = list(range(1, len(step_ids) + 1))
            if sorted_ids != expected_ids:
                raise ValueError('Los step_ids deben ser secuenciales comenzando desde 1')
        
        return v
