from __future__ import annotations

from typing import List, Optional
from pydantic import BaseModel, Field, field_validator
from medyator import Query

from FuzzyService.Application.Features.FuzzyRules.DTOs.FuzzyRuleDto import FuzzyRuleDto


class GetAllFuzzyRulesQuery(BaseModel, Query):
    """Query para obtener todas las reglas difusas con filtros opcionales."""
    
    system_id: Optional[str] = Field(
        None, 
        description="ID del sistema difuso para filtrar reglas"
    )
    skip: int = Field(
        0, 
        ge=0,
        description="Número de elementos a omitir para paginación"
    )
    limit: int = Field(
        100, 
        ge=1, 
        le=1000,
        description="Número máximo de elementos a retornar"
    )
    

    
    @field_validator('system_id')
    @classmethod
    def validate_system_id(cls, v: Optional[str]) -> Optional[str]:
        """Valida que el system_id sea un ObjectId válido si se proporciona."""
        if v is not None and (not v or len(v) != 24):
            raise ValueError('system_id debe ser un ObjectId válido de 24 caracteres')
        return v
