from __future__ import annotations

from typing import Optional
from pydantic import BaseModel, Field, PrivateAttr, field_validator, ConfigDict
from medyator import Command


class DeleteFuzzyRuleCommand(BaseModel, Command):
    """Comando para eliminar una regla difusa."""
    
    model_config = ConfigDict(validate_assignment=True, extra="forbid")
    
    rule_id: str = Field(
        ..., 
        description="ID único de la regla a eliminar"
    )
    
    _result: Optional[bool] = PrivateAttr(default=None)

    @field_validator('rule_id')
    @classmethod
    def validate_rule_id(cls, v: str) -> str:
        """Valida que el rule_id sea un ObjectId válido."""
        if not v or len(v) != 24:
            raise ValueError('rule_id debe ser un ObjectId válido de 24 caracteres')
        return v
