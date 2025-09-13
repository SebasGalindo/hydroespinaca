from __future__ import annotations

from datetime import datetime
from typing import List, Optional, Dict, Any
from pydantic import BaseModel, Field, field_validator, model_validator

from FuzzyService.Domain.Entities.fuzzy_routine import FuzzyRoutine, RoutineStep
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyRoutineId


class RoutineStepDto(BaseModel):
    """DTO para un paso de rutina difusa."""
    
    step_id: int = Field(
        ..., 
        ge=0,
        description="ID Ãºnico del paso dentro de la rutina"
    )
    condition: str = Field(
        ..., 
        min_length=1,
        max_length=200,
        description="CondiciÃ³n que debe cumplirse para ejecutar este paso"
    )
    power_term_id: str = Field(
        ..., 
        description="ID del tag de potencia"
    )
    duration_term_id: str = Field(
        ..., 
        description="ID del tag de duraciÃ³n"
    )

    @field_validator('condition')
    @classmethod
    def validate_condition(cls, v: str) -> str:
        """Valida que la condiciÃ³n no estÃ© vacÃ­a."""
        v = v.strip()
        if not v:
            raise ValueError('La condiciÃ³n no puede estar vacÃ­a')
        return v

    @field_validator('power_term_id', 'duration_term_id')
    @classmethod
    def validate_tag_ids(cls, v: str) -> str:
        """Valida que los IDs de tags sean ObjectIds vÃ¡lidos."""
        if not v or len(v) != 24:
            raise ValueError('Los IDs de tags deben ser ObjectIds vÃ¡lidos de 24 caracteres')
        return v

    @classmethod
    def from_entity(cls, step: RoutineStep) -> RoutineStepDto:
        """Convierte una entidad RoutineStep a DTO."""
        return cls(
            step_id=step.step_id,
            condition=step.condition,
            power_term_id=str(step.power_term_id),
            duration_term_id=str(step.duration_term_id)
        )

    def to_entity(self) -> RoutineStep:
        """Convierte el DTO a entidad RoutineStep."""
        return RoutineStep(
            step_id=self.step_id,
            condition=self.condition,
            power_term_id=self.power_term_id,
            duration_term_id=self.duration_term_id
        )


class FuzzyRoutineDto(BaseModel):
    """DTO para rutina difusa."""
    
    id: Optional[str] = Field(
        None, 
        description="ID Ãºnico de la rutina"
    )
    routine_name: str = Field(
        ..., 
        min_length=1,
        max_length=100,
        description="Nombre de la rutina difusa"
    )
    system_id: Optional[str] = Field(
        None, 
        description="ID del sistema difuso"
    )
    created_at: Optional[datetime] = Field(
        None, 
        description="Fecha y hora de creaciÃ³n"
    )
    steps: List[RoutineStepDto] = Field(
        default_factory=list,
        description="Lista de pasos de la rutina"
    )

    @field_validator('routine_name')
    @classmethod
    def validate_routine_name(cls, v: str) -> str:
        """Valida que el nombre de la rutina no estÃ© vacÃ­o."""
        v = v.strip()
        if not v:
            raise ValueError('El nombre de la rutina no puede estar vacÃ­o')
        if len(v) > 100:
            raise ValueError('El nombre de la rutina no puede exceder 100 caracteres')
        return v

    @field_validator('system_id')
    @classmethod
    def validate_system_id(cls, v: Optional[str]) -> Optional[str]:
        """Valida que el ID del sistema sea un ObjectId vÃ¡lido si se proporciona."""
        if v is not None and (not v or len(v) != 24):
            raise ValueError('El ID del sistema debe ser un ObjectId vÃ¡lido de 24 caracteres')
        return v

    @field_validator('steps')
    @classmethod
    def validate_steps(cls, v: List[RoutineStepDto]) -> List[RoutineStepDto]:
        """Valida que no haya step_ids duplicados."""
        if v:
            step_ids = [step.step_id for step in v]
            if len(step_ids) != len(set(step_ids)):
                raise ValueError('No puede haber step_ids duplicados en la rutina')
        return v

    @model_validator(mode='after')
    def validate_routine_steps_order(self) -> 'FuzzyRoutineDto':
        """Valida que los pasos estÃ©n ordenados correctamente."""
        if self.steps:
            step_ids = [step.step_id for step in self.steps]
            
            # Validar que no haya step_ids duplicados
            if len(step_ids) != len(set(step_ids)):
                raise ValueError('No puede haber step_ids duplicados')
            
            # Validar que los step_ids estÃ©n ordenados
            sorted_step_ids = sorted(step_ids)
            if step_ids != sorted_step_ids:
                raise ValueError('Los pasos deben estar ordenados por step_id')
        
        return self

    @classmethod
    def from_entity(cls, routine: FuzzyRoutine) -> FuzzyRoutineDto:
        """Convierte una entidad FuzzyRoutine a DTO."""
        return cls(
            id=str(routine.id) if routine.id else None,
            routine_name=routine.routine_name,
            created_at=routine.created_at,
            steps=[RoutineStepDto.from_entity(step) for step in routine.steps]
        )

    def to_entity(self) -> FuzzyRoutine:
        """Convierte el DTO a entidad FuzzyRoutine."""
        return FuzzyRoutine(
            id=self.id,
            routine_name=self.routine_name,
            created_at=self.created_at,
            steps=[step.to_entity() for step in self.steps]
        )


class UpdateRoutineStepDto(BaseModel):
    """DTO para actualizar un paso especÃ­fico en una rutina difusa."""
    
    step_id: Optional[int] = Field(
        None,
        ge=0,
        description="ID Ãºnico del paso dentro de la rutina"
    )
    condition: Optional[str] = Field(
        None, 
        min_length=1,
        max_length=200,
        description="CondiciÃ³n que debe cumplirse para ejecutar este paso"
    )
    power_term_id: Optional[str] = Field(
        None, 
        description="ID del tag de potencia"
    )
    duration_term_id: Optional[str] = Field(
        None, 
        description="ID del tag de duraciÃ³n"
    )

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

