from __future__ import annotations

from typing import Optional
from pydantic import BaseModel, Field, field_validator, PrivateAttr
from medyator import Query

from FuzzyService.Application.Features.FuzzyRules.DTOs.FuzzyRuleDto import FuzzyRuleDto


class GetFuzzyRuleByIdQuery(BaseModel, Query):
    """Query para obtener una regla difusa por su ID."""
    
    rule_id: str = Field(
        ..., 
        description="ID único de la regla difusa"
    )
    
    _result: Optional[FuzzyRuleDto] = PrivateAttr(default=None)
    
    @field_validator('rule_id')
    @classmethod
    def validate_rule_id(cls, v: str) -> str:
        """Valida que el rule_id sea un ObjectId válido."""
        if not v or len(v) != 24:
            raise ValueError('rule_id debe ser un ObjectId válido de 24 caracteres')
        return v
