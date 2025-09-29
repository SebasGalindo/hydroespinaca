from __future__ import annotations

from typing import List, Optional
from pydantic import BaseModel, Field, PrivateAttr, field_validator, ConfigDict
from medyator import Command

from FuzzyService.Application.Features.FuzzyRules.DTOs.FuzzyRuleDto import FuzzyRuleDto, ConditionDto


class CreateFuzzyRuleCommand(BaseModel, Command):
    """Comando para crear una nueva regla difusa."""
    
    model_config = ConfigDict(validate_assignment=True, extra="forbid")
    
    name: str = Field(
        ..., 
        min_length=1, 
        max_length=100,
        description="Nombre de la regla difusa"
    )
    system_id: str = Field(
        ..., 
        description="ID del sistema difuso al que pertenece la regla"
    )
    description: Optional[str] = Field(
        None, 
        max_length=500,
        description="Descripción opcional de la regla"
    )
    conditions: List[ConditionDto] = Field(
        ..., 
        min_length=1,
        description="Lista de condiciones de la regla (mínimo 1)"
    )
    connectors: List[str] = Field(
        default_factory=list,
        description="Lista de conectores lógicos (AND, OR)"
    )
    consequent: Optional[str] = Field(
        None,
        description="ID de la rutina consecuente"
    )
    
    _result: Optional[FuzzyRuleDto] = PrivateAttr(default=None)

    @field_validator('name')
    @classmethod
    def validate_name(cls, v: str) -> str:
        """Valida que el nombre no esté vacío y no contenga solo espacios."""
        if not v or not v.strip():
            raise ValueError('El nombre no puede estar vacío')
        return v.strip()

    @field_validator('system_id')
    @classmethod
    def validate_system_id(cls, v: str) -> str:
        """Valida que el system_id sea un ObjectId válido."""
        if not v or len(v) != 24:
            raise ValueError('system_id debe ser un ObjectId válido de 24 caracteres')
        return v

    @field_validator('connectors')
    @classmethod
    def validate_connectors(cls, v: List[str], info) -> List[str]:
        """Valida que los conectores sean válidos y tengan la cardinalidad correcta."""
        valid_connectors = {'AND', 'OR'}
        
        # Validar que todos los conectores sean válidos
        for connector in v:
            if connector not in valid_connectors:
                raise ValueError(f'Conector inválido: {connector}. Debe ser AND o OR')
        
        # Validar cardinalidad: debe haber exactamente n-1 conectores para n condiciones
        if 'conditions' in info.data:
            conditions_count = len(info.data['conditions'])
            if len(v) != conditions_count - 1:
                raise ValueError(
                    f'Número incorrecto de conectores. '
                    f'Para {conditions_count} condiciones se requieren {conditions_count - 1} conectores'
                )
        
        return v

    @field_validator('conditions')
    @classmethod
    def validate_conditions(cls, v: List[ConditionDto]) -> List[ConditionDto]:
        """Valida que las condiciones sean válidas."""
        if not v:
            raise ValueError('Debe haber al menos una condición')
        
        # Validar que no haya variables duplicadas
        variable_ids = [condition.variable_id for condition in v]
        if len(variable_ids) != len(set(variable_ids)):
            raise ValueError('No puede haber condiciones duplicadas para la misma variable')
        
        # Validar operadores
        valid_operators = {'IS', 'IS_NOT'}
        for condition in v:
            if condition.operator not in valid_operators:
                raise ValueError(f'Operador inválido: {condition.operator}. Debe ser IS o IS_NOT')
        
        return v

    @field_validator('consequent')
    @classmethod
    def validate_consequent(cls, v: Optional[str]) -> Optional[str]:
        """Valida que el consequent sea un ObjectId válido si se proporciona."""
        if v is not None and (not v or len(v) != 24):
            raise ValueError('consequent debe ser un ObjectId válido de 24 caracteres')
        return v
