from __future__ import annotations

from typing import Optional
from pydantic import BaseModel, Field, PrivateAttr, field_validator, ConfigDict
from medyator import Command

from FuzzyService.Application.Features.FuzzyRules.DTOs.FuzzyRuleDto import FuzzyRuleDto


class AddConditionToRuleCommand(BaseModel, Command):
    """Comando para agregar una condición a una regla difusa existente."""
    
    model_config = ConfigDict(validate_assignment=True, extra="forbid")
    
    rule_id: str = Field(
        ..., 
        min_length=1,
        description="ID único de la regla a la que se agregará la condición"
    )
    variable_id: str = Field(
        ..., 
        min_length=1,
        description="ID de la variable para la nueva condición"
    )
    operator: str = Field(
        ..., 
        description="Operador lógico para la condición (IS, IS_NOT)"
    )
    value: str = Field(
        ..., 
        min_length=1,
        description="Valor/etiqueta lingüística para la condición"
    )
    connector: Optional[str] = Field(
        None,
        description="Conector lógico para unir con la condición anterior (AND, OR). Requerido si ya existen condiciones."
    )
    
    _result: Optional[FuzzyRuleDto] = PrivateAttr(default=None)
    
    @property
    def result(self) -> Optional[FuzzyRuleDto]:
        """Resultado del comando después de la ejecución."""
        return self._result
    
    @field_validator('operator')
    @classmethod
    def validate_operator(cls, v: str) -> str:
        """Valida que el operador sea válido."""
        valid_operators = {'IS', 'IS_NOT'}
        if v not in valid_operators:
            raise ValueError(f'Operador inválido: {v}. Debe ser IS o IS_NOT')
        return v
    
    @field_validator('connector')
    @classmethod
    def validate_connector(cls, v: Optional[str]) -> Optional[str]:
        """Valida que el conector sea válido si se proporciona."""
        if v is None:
            return v
        
        valid_connectors = {'AND', 'OR'}
        if v not in valid_connectors:
            raise ValueError(f'Conector inválido: {v}. Debe ser AND o OR')
        return v
    
    @field_validator('value')
    @classmethod
    def validate_value(cls, v: str) -> str:
        """Valida que el valor no esté vacío."""
        if not v.strip():
            raise ValueError('El valor de la condición no puede estar vacío')
        return v.strip()