from __future__ import annotations

from typing import List, Optional
from pydantic import BaseModel, Field, PrivateAttr, field_validator, ConfigDict
from medyator import Command

from FuzzyService.Application.Features.FuzzyRules.DTOs.FuzzyRuleDto import FuzzyRuleDto


class UpdateRuleConnectorsCommand(BaseModel, Command):
    """Comando para actualizar los conectores de una regla difusa existente."""
    
    model_config = ConfigDict(validate_assignment=True, extra="forbid")
    
    rule_id: str = Field(
        ..., 
        min_length=1,
        description="ID único de la regla cuyos conectores se actualizarán"
    )
    connectors: List[str] = Field(
        ...,
        description="Nueva lista de conectores lógicos (AND, OR)"
    )
    
    _result: Optional[FuzzyRuleDto] = PrivateAttr(default=None)
    
    @property
    def result(self) -> Optional[FuzzyRuleDto]:
        """Resultado del comando después de la ejecución."""
        return self._result
    
    @field_validator('connectors')
    @classmethod
    def validate_connectors(cls, v: List[str]) -> List[str]:
        """Valida que los conectores sean válidos."""
        valid_connectors = {'AND', 'OR'}
        
        # Validar que todos los conectores sean válidos
        for connector in v:
            if connector not in valid_connectors:
                raise ValueError(f'Conector inválido: {connector}. Debe ser AND o OR')
        
        return v