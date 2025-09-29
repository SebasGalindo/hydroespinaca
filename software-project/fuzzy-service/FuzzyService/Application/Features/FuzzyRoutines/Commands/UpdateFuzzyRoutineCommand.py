from __future__ import annotations

from typing import List, Optional
from pydantic import BaseModel, Field, PrivateAttr, field_validator, ConfigDict
from medyator import Command

from FuzzyService.Application.Features.FuzzyRoutines.DTOs.FuzzyRoutineDto import FuzzyRoutineDto, RoutineStepDto


class UpdateFuzzyRoutineCommand(BaseModel, Command):
    """Comando para actualizar una rutina difusa existente."""
    
    model_config = ConfigDict(validate_assignment=True, extra="forbid")
    
    routine_id: Optional[str] = Field(
        None, 
        description="ID único de la rutina a actualizar"
    )
    routine_name: Optional[str] = Field(
        None, 
        min_length=1, 
        max_length=100,
        description="Nuevo nombre de la rutina difusa"
    )
    steps: Optional[List[RoutineStepDto]] = Field(
        None,
        description="Nueva lista de pasos de la rutina"
    )
    
    _result: Optional[FuzzyRoutineDto] = PrivateAttr(default=None)



    @field_validator('routine_id')
    @classmethod
    def validate_routine_id(cls, v: str) -> str:
        """Valida que el routine_id sea un ObjectId válido."""
        if not v or len(v) != 24:
            raise ValueError('routine_id debe ser un ObjectId válido de 24 caracteres')
        return v

    @field_validator('routine_name')
    @classmethod
    def validate_routine_name(cls, v: Optional[str]) -> Optional[str]:
        """Valida que el nombre de la rutina no esté vacío si se proporciona."""
        if v is not None:
            v = v.strip()
            if not v:
                raise ValueError('El nombre de la rutina no puede estar vacío')
        return v

    @field_validator('steps')
    @classmethod
    def validate_steps(cls, v: Optional[List[RoutineStepDto]]) -> Optional[List[RoutineStepDto]]:
        """Valida que no haya step_ids duplicados si se proporcionan."""
        if v is not None and v:  # Si la lista no está vacía
            step_ids = [step.step_id for step in v]
            if len(step_ids) != len(set(step_ids)):
                raise ValueError('No puede haber step_ids duplicados en la rutina')
            
            # Validar que los step_ids sean secuenciales desde 1
            sorted_ids = sorted(step_ids)
            expected_ids = list(range(1, len(step_ids) + 1))
            if sorted_ids != expected_ids:
                raise ValueError('Los step_ids deben ser secuenciales comenzando desde 1')
        
        return v
