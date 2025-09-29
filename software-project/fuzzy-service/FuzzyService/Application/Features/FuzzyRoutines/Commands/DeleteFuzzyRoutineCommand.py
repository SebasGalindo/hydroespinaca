from __future__ import annotations

from typing import Optional
from pydantic import BaseModel, Field, PrivateAttr, field_validator, ConfigDict
from medyator import Command


class DeleteFuzzyRoutineCommand(BaseModel, Command):
    """Comando para eliminar una rutina difusa."""
    
    model_config = ConfigDict(validate_assignment=True, extra="forbid")
    
    routine_id: str = Field(
        ..., 
        description="ID único de la rutina a eliminar"
    )
    
    _result: Optional[bool] = PrivateAttr(default=None)



    @field_validator('routine_id')
    @classmethod
    def validate_routine_id(cls, v: str) -> str:
        """Valida que el routine_id sea un ObjectId válido."""
        if not v or len(v) != 24:
            raise ValueError('routine_id debe ser un ObjectId válido de 24 caracteres')
        return v
