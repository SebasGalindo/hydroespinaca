from __future__ import annotations

from typing import List, Optional, Dict, Any
from datetime import datetime
from pydantic import BaseModel, Field, field_validator, model_validator

from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule
from FuzzyService.Domain.Enums import RuleConnector, LogicalOperator


class ConditionDto(BaseModel):
    """DTO para una condición de regla difusa."""
    variable_id: str = Field(..., description="ID de la variable difusa")
    operator: str = Field(..., description="Operador lógico (IS, IS_NOT)")
    value: str = Field(..., description="Etiqueta lingüística")
    
    @field_validator('variable_id')
    @classmethod
    def validate_variable_id(cls, v: str) -> str:
        """Valida que el ID de variable sea un ObjectId válido."""
        if not v or len(v) != 24:
            raise ValueError('El ID de variable debe ser un ObjectId válido de 24 caracteres')
        return v
    
    @field_validator('operator')
    @classmethod
    def validate_operator(cls, v: str) -> str:
        """Valida que el operador sea válido para sistemas fuzzy."""
        valid_operators = {'IS', 'IS_NOT'}
        if v not in valid_operators:
            raise ValueError(f'Operador inválido: {v}. Solo se permiten: {", ".join(valid_operators)}')
        return v
    
    @field_validator('value')
    @classmethod
    def validate_value(cls, v: str) -> str:
        """Valida que la etiqueta lingüística no esté vacía."""
        v = v.strip()
        if not v:
            raise ValueError('La etiqueta lingüística no puede estar vacía')
        if len(v) > 50:
            raise ValueError('La etiqueta lingüística no puede exceder 50 caracteres')
        return v


class FuzzyRuleDto(BaseModel):
    """DTO para reglas difusas."""
    id: Optional[str] = Field(None, description="ID único de la regla")
    name: str = Field(..., description="Nombre de la regla")
    system_id: Optional[str] = Field(None, description="ID del sistema difuso")
    description: Optional[str] = Field(None, description="Descripción de la regla")
    conditions: List[ConditionDto] = Field(default_factory=list, description="Lista de condiciones")
    connectors: List[str] = Field(default_factory=list, description="Lista de conectores (AND, OR)")
    consequent: Optional[str] = Field(None, description="ID de la rutina consecuente")
    created_at: Optional[datetime] = Field(None, description="Fecha de creación")
    rule_text: Optional[str] = Field(None, description="Representación textual de la regla")
    
    @field_validator('name')
    @classmethod
    def validate_name(cls, v: str) -> str:
        """Valida que el nombre de la regla no esté vacío."""
        v = v.strip()
        if not v:
            raise ValueError('El nombre de la regla no puede estar vacío')
        if len(v) > 100:
            raise ValueError('El nombre de la regla no puede exceder 100 caracteres')
        return v
    
    @field_validator('system_id')
    @classmethod
    def validate_system_id(cls, v: Optional[str]) -> Optional[str]:
        """Valida que el ID del sistema sea un ObjectId válido si se proporciona."""
        if v is not None and (not v or len(v) != 24):
            raise ValueError('El ID del sistema debe ser un ObjectId válido de 24 caracteres')
        return v
    
    @field_validator('consequent')
    @classmethod
    def validate_consequent(cls, v: Optional[str]) -> Optional[str]:
        """Valida que el ID del consecuente sea un ObjectId válido si se proporciona."""
        if v is not None and (not v or len(v) != 24):
            raise ValueError('El ID del consecuente debe ser un ObjectId válido de 24 caracteres')
        return v
    
    @field_validator('connectors')
    @classmethod
    def validate_connectors(cls, v: List[str]) -> List[str]:
        """Valida que los conectores sean válidos."""
        valid_connectors = {'AND', 'OR'}
        for connector in v:
            if connector not in valid_connectors:
                raise ValueError(f'Conector inválido: {connector}. Solo se permiten: {", ".join(valid_connectors)}')
        return v
    
    @field_validator('conditions')
    @classmethod
    def validate_conditions(cls, v: List[ConditionDto]) -> List[ConditionDto]:
        """Valida que haya al menos una condición y que no haya variables duplicadas."""
        if len(v) == 0:
            raise ValueError('Debe haber al menos una condición')
        
        # Verificar variables únicas
        variable_ids = [condition.variable_id for condition in v]
        if len(variable_ids) != len(set(variable_ids)):
            raise ValueError('No puede haber condiciones duplicadas para la misma variable')
        
        return v
    
    @model_validator(mode='after')
    def validate_connectors_cardinality(self):
        """Valida que el número de conectores sea correcto (n-1 para n condiciones)."""
        expected_connectors = max(0, len(self.conditions) - 1)
        if len(self.connectors) != expected_connectors:
            raise ValueError(
                f'Número incorrecto de conectores. '
                f'Para {len(self.conditions)} condiciones se requieren {expected_connectors} conectores, '
                f'pero se proporcionaron {len(self.connectors)}'
            )
        return self

    @classmethod
    def from_entity(cls, entity: FuzzyRule) -> FuzzyRuleDto:
        """Convierte una entidad FuzzyRule a DTO."""
        conditions_dto = [
            ConditionDto(
                variable_id=str(condition["variableId"]),
                operator=condition["operator"].value if hasattr(condition["operator"], 'value') else str(condition["operator"]),
                value=condition["value"]
            )
            for condition in entity.conditions
        ]
        
        connectors_str = [
            connector.value if hasattr(connector, 'value') else str(connector)
            for connector in entity.connectors
        ]
        
        return cls(
            id=str(entity.id) if entity.id else None,
            name=entity.name,
            system_id=str(entity.system_id) if entity.system_id else None,
            description=entity.description,
            conditions=conditions_dto,
            connectors=connectors_str,
            consequent=str(entity.consequent) if entity.consequent else None,
            created_at=entity.created_at,
            rule_text=entity.get_rule_text()
        )

    def to_entity(self) -> FuzzyRule:
        """Convierte el DTO a entidad FuzzyRule."""
        from FuzzyService.Domain.ValueObjects.DomainId import FuzzyRuleId, FuzzySystemId, FuzzyVariableId, FuzzyRoutineId
        
        # Convertir condiciones
        conditions_dict = [
            {
                "variableId": FuzzyVariableId(condition.variable_id),
                "operator": LogicalOperator(condition.operator),
                "value": condition.value
            }
            for condition in self.conditions
        ]
        
        # Convertir conectores
        connectors_enum = [RuleConnector(connector) for connector in self.connectors]
        
        return FuzzyRule(
            id=FuzzyRuleId(self.id) if self.id else None,
            name=self.name,
            system_id=FuzzySystemId(self.system_id) if self.system_id else None,
            description=self.description,
            conditions=conditions_dict,
            connectors=connectors_enum,
            consequent=FuzzyRoutineId(self.consequent) if self.consequent else None,
            created_at=self.created_at
        )
