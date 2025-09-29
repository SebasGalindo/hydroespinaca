from __future__ import annotations

from typing import Optional
from pydantic import BaseModel, Field, PrivateAttr, field_validator, ConfigDict
from medyator import Command

from FuzzyService.Application.Features.FuzzyRoutines.DTOs.FuzzyRoutineDto import FuzzyRoutineDto, RoutineStepDto


class UpdateStepInRoutineCommand(BaseModel, Command):
    """Comando para actualizar un paso especÃ­fico en una rutina difusa."""
    
    model_config = ConfigDict(validate_assignment=True, extra="forbid")
    
    routine_id: str = Field(
        ..., 
        description="ID Ãºnico de la rutina"
    )
    step_id: int = Field(
        ..., 
        ge=0,
        description="ID del paso a actualizar"
    )
    condition: Optional[str] = Field(
        None,
        min_length=1,
        max_length=200,
        description="Nueva condiciÃ³n del paso"
    )
    power_term_id: Optional[str] = Field(
        None,
        description="Nuevo ID del tag de potencia"
    )
    duration_term_id: Optional[str] = Field(
        None,
        description="Nuevo ID del tag de duraciÃ³n"
    )
    
    _result: Optional[FuzzyRoutineDto] = PrivateAttr(default=None)



    @field_validator('routine_id')
    @classmethod
    def validate_routine_id(cls, v: str) -> str:
        """Valida que el routine_id sea un ObjectId vÃ¡lido."""
        if not v or len(v) != 24:
            raise ValueError('routine_id debe ser un ObjectId vÃ¡lido de 24 caracteres')
        return v

    @field_validator('condition')
    @classmethod
    def validate_condition(cls, v: Optional[str]) -> Optional[str]:
        """Valida que la condiciÃ³n no estÃ© vacÃ­a si se proporciona."""
        if v is not None:
            v = v.strip()
            if not v:
                raise ValueError('La condiciÃ³n no puede estar vacÃ­a')
        return v

    @field_validator('power_term_id', 'duration_term_id')
    @classmethod
    def validate_tag_ids(cls, v: Optional[str]) -> Optional[str]:
        """Valida que los IDs de tags sean ObjectIds vÃ¡lidos si se proporcionan."""
        if v is not None and (not v or len(v) != 24):
            raise ValueError('Los IDs de tags deben ser ObjectIds vÃ¡lidos de 24 caracteres')
        return v
