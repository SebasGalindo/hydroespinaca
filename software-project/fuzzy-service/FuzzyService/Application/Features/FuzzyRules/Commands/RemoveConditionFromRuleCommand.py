from __future__ import annotations

from typing import Optional
from pydantic import BaseModel, Field, PrivateAttr, ConfigDict
from medyator import Command

from FuzzyService.Application.Features.FuzzyRules.DTOs.FuzzyRuleDto import FuzzyRuleDto


class RemoveConditionFromRuleCommand(BaseModel, Command):
    """Comando para remover una condición de una regla difusa existente."""
    
    model_config = ConfigDict(validate_assignment=True, extra="forbid")
    
    rule_id: str = Field(
        ..., 
        min_length=1,
        description="ID único de la regla de la que se removerá la condición"
    )
    variable_id: str = Field(
        ..., 
        min_length=1,
        description="ID de la variable cuya condición se eliminará"
    )
    
    _result: Optional[FuzzyRuleDto] = PrivateAttr(default=None)
    
    @property
    def result(self) -> Optional[FuzzyRuleDto]:
        """Resultado del comando después de la ejecución."""
        return self._result