from __future__ import annotations

from typing import List, Optional
from pydantic import BaseModel, Field, PrivateAttr, field_validator, ConfigDict
from medyator import Query

from FuzzyService.Application.Features.FuzzyRules.DTOs.FuzzyRuleDto import FuzzyRuleDto


class GetFuzzyRulesBySystemQuery(BaseModel, Query):
    """Query para obtener todas las reglas difusas de un sistema específico."""
    
    model_config = ConfigDict(validate_assignment=True, extra="forbid")
    
    system_id: str = Field(
        ..., 
        description="ID del sistema difuso"
    )
    skip: int = Field(
        default=0, 
        ge=0,
        description="Número de registros a omitir para paginación"
    )
    limit: int = Field(
        default=100, 
        ge=1, 
        le=1000,
        description="Número máximo de registros a retornar"
    )
    
    _result: Optional[List[FuzzyRuleDto]] = PrivateAttr(default=None)



    @field_validator('system_id')
    @classmethod
    def validate_system_id(cls, v: str) -> str:
        """Valida que el system_id sea un ObjectId válido."""
        if not v or len(v) != 24:
            raise ValueError('system_id debe ser un ObjectId válido de 24 caracteres')
        return v