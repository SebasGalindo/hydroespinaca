from __future__ import annotations

from typing import List, Optional
from pydantic import BaseModel, Field, PrivateAttr, field_validator, ConfigDict
from medyator import Command

from FuzzyService.Application.Features.FuzzyRules.DTOs.FuzzyRuleDto import FuzzyRuleDto, ConditionDto


class UpdateFuzzyRuleCommand(BaseModel, Command):
    """Comando para actualizar una regla difusa existente."""
    
    model_config = ConfigDict(validate_assignment=True, extra="forbid")
    
    rule_id: Optional[str] = Field(
        None, 
        description="ID único de la regla a actualizar"
    )
    name: Optional[str] = Field(
        None, 
        min_length=1, 
        max_length=100,
        description="Nuevo nombre de la regla difusa"
    )
    description: Optional[str] = Field(
        None, 
        max_length=500,
        description="Nueva descripción de la regla"
    )
    conditions: Optional[List[ConditionDto]] = Field(
        None, 
        min_length=1,
        description="Nueva lista de condiciones de la regla"
    )
    connectors: Optional[List[str]] = Field(
        None,
        description="Nueva lista de conectores lógicos (AND, OR)"
    )
    consequent: Optional[str] = Field(
        None,
        description="Nuevo ID de la rutina consecuente"
    )
    
    _result: Optional[FuzzyRuleDto] = PrivateAttr(default=None)

    @field_validator('rule_id')
    @classmethod
    def validate_rule_id(cls, v: str) -> str:
        """Valida que el rule_id sea un ObjectId válido."""
        if not v or len(v) != 24:
            raise ValueError('rule_id debe ser un ObjectId válido de 24 caracteres')
        return v

    @field_validator('name')
    @classmethod
    def validate_name(cls, v: Optional[str]) -> Optional[str]:
        """Valida que el nombre no esté vacío si se proporciona."""
        if v is not None:
            v = v.strip()
            if not v:
                raise ValueError('El nombre no puede estar vacío')
        return v

    @field_validator('connectors')
    @classmethod
    def validate_connectors(cls, v: Optional[List[str]], info) -> Optional[List[str]]:
        """Valida que los conectores sean válidos y tengan la cardinalidad correcta."""
        if v is None:
            return v
            
        valid_connectors = {'AND', 'OR'}
        
        # Validar que todos los conectores sean válidos
        for connector in v:
            if connector not in valid_connectors:
                raise ValueError(f'Conector inválido: {connector}. Debe ser AND o OR')
        
        # Validar cardinalidad si se proporcionan condiciones
        if 'conditions' in info.data and info.data['conditions'] is not None:
            conditions_count = len(info.data['conditions'])
            if len(v) != conditions_count - 1:
                raise ValueError(
                    f'Número incorrecto de conectores. '
                    f'Para {conditions_count} condiciones se requieren {conditions_count - 1} conectores'
                )
        
        return v

    @field_validator('conditions')
    @classmethod
    def validate_conditions(cls, v: Optional[List[ConditionDto]]) -> Optional[List[ConditionDto]]:
        """Valida que las condiciones sean válidas si se proporcionan."""
        if v is None:
            return v
            
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
