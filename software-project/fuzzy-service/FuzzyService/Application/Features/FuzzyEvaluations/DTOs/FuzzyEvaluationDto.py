from __future__ import annotations

from typing import List, Optional, Dict, Any
from datetime import datetime

from pydantic import BaseModel, Field, ConfigDict, field_validator

from FuzzyService.Domain.Entities.fuzzy_evaluation import FuzzyEvaluation, InputValue, RuleActivation, OutputValue
from FuzzyService.Domain.Enums import PowerRange, DurationRange


class InputValueDto(BaseModel):
    """DTO para un valor de entrada de sensor."""
    
    model_config = ConfigDict(validate_assignment=True, extra="forbid")
    
    sensor_id: str = Field(..., description="ID del sensor")
    value: float = Field(..., description="Valor crisp del sensor")
    
    @field_validator('sensor_id')
    @classmethod
    def validate_sensor_id(cls, v: str) -> str:
        """Valida que el ID del sensor no esté vacío."""
        if not v or not v.strip():
            raise ValueError('El ID del sensor no puede estar vacío')
        return v.strip()
    
    @classmethod
    def from_entity(cls, entity: InputValue) -> InputValueDto:
        """Convierte una entidad InputValue a DTO."""
        return cls(
            sensor_id=entity.sensor_id,
            value=entity.value
        )
    
    def to_entity(self) -> InputValue:
        """Convierte el DTO a entidad InputValue."""
        return InputValue(
            sensor_id=self.sensor_id,
            value=self.value
        )


class OutputValueDto(BaseModel):
    """DTO para un valor de salida hacia actuador."""
    
    model_config = ConfigDict(validate_assignment=True, extra="forbid")
    
    actuator_id: str = Field(..., description="ID del actuador")
    power_range: str = Field(..., description="Rango de potencia")
    duration_range: str = Field(..., description="Rango de duración")
    
    @field_validator('actuator_id')
    @classmethod
    def validate_actuator_id(cls, v: str) -> str:
        """Valida que el ID del actuador no esté vacío."""
        if not v or not v.strip():
            raise ValueError('El ID del actuador no puede estar vacío')
        return v.strip()
    
    @field_validator('power_range')
    @classmethod
    def validate_power_range(cls, v: str) -> str:
        """Valida que el rango de potencia sea válido."""
        valid_ranges = [pr.value for pr in PowerRange]
        if v not in valid_ranges:
            raise ValueError(f'Rango de potencia inválido: {v}. Valores válidos: {", ".join(valid_ranges)}')
        return v
    
    @field_validator('duration_range')
    @classmethod
    def validate_duration_range(cls, v: str) -> str:
        """Valida que el rango de duración sea válido."""
        valid_ranges = [dr.value for dr in DurationRange]
        if v not in valid_ranges:
            raise ValueError(f'Rango de duración inválido: {v}. Valores válidos: {", ".join(valid_ranges)}')
        return v
    
    @classmethod
    def from_entity(cls, entity: OutputValue) -> OutputValueDto:
        """Convierte una entidad OutputValue a DTO."""
        return cls(
            actuator_id=str(entity.actuator_id),
            power_range=entity.power_range.value if hasattr(entity.power_range, 'value') else str(entity.power_range),
            duration_range=entity.duration_range.value if hasattr(entity.duration_range, 'value') else str(entity.duration_range)
        )
    
    def to_entity(self) -> OutputValue:
        """Convierte el DTO a entidad OutputValue."""
        from FuzzyService.Domain.ValueObjects.DomainId import ActuatorId
        return OutputValue(
            actuator_id=ActuatorId(self.actuator_id),
            power_range=PowerRange(self.power_range),
            duration_range=DurationRange(self.duration_range)
        )


class RuleActivationDto(BaseModel):
    """DTO para la activación de una regla durante la evaluación."""
    
    model_config = ConfigDict(validate_assignment=True, extra="forbid")
    
    rule_id: str = Field(..., description="ID de la regla activada")
    firing_strength: float = Field(..., ge=0.0, le=1.0, description="Fuerza de activación (0.0 - 1.0)")
    output_values: List[OutputValueDto] = Field(default_factory=list, description="Valores de salida de la regla")
    
    @field_validator('rule_id')
    @classmethod
    def validate_rule_id(cls, v: str) -> str:
        """Valida que el ID de la regla no esté vacío."""
        if not v or not v.strip():
            raise ValueError('El ID de la regla no puede estar vacío')
        return v.strip()
    
    @classmethod
    def from_entity(cls, entity: RuleActivation) -> RuleActivationDto:
        """Convierte una entidad RuleActivation a DTO."""
        return cls(
            rule_id=str(entity.rule_id),
            firing_strength=entity.firing_strength,
            output_values=[OutputValueDto.from_entity(ov) for ov in entity.output_values]
        )
    
    def to_entity(self) -> RuleActivation:
        """Convierte el DTO a entidad RuleActivation."""
        from FuzzyService.Domain.ValueObjects.DomainId import FuzzyRuleId
        return RuleActivation(
            rule_id=FuzzyRuleId(self.rule_id),
            firing_strength=self.firing_strength,
            output_values=[ov.to_entity() for ov in self.output_values]
        )


class FuzzyEvaluationDto(BaseModel):
    """DTO para representar una evaluación fuzzy en la capa de aplicación/API."""
    
    model_config = ConfigDict(
        validate_assignment=True,
        populate_by_name=True,
        use_enum_values=True,
        arbitrary_types_allowed=True,
        extra="forbid",
    )
    
    # Identificación
    id: Optional[str] = Field(default=None, description="Identificador de la evaluación (ObjectId como string)")
    system_id: str = Field(..., description="ID del sistema fuzzy evaluado")
    
    # Timestamp
    timestamp: Optional[datetime] = Field(default=None, description="Fecha y hora de la evaluación (UTC ISO8601)")
    
    # Datos de la evaluación
    inputs: List[InputValueDto] = Field(default_factory=list, description="Valores de entrada de sensores")
    activated_rules: List[RuleActivationDto] = Field(default_factory=list, description="Reglas activadas durante la evaluación")
    
    # Validaciones
    @field_validator('system_id')
    @classmethod
    def validate_system_id(cls, v: str) -> str:
        """Valida que el ID del sistema no esté vacío."""
        if not v or not v.strip():
            raise ValueError('El ID del sistema no puede estar vacío')
        return v.strip()
    
    @field_validator('inputs')
    @classmethod
    def validate_inputs(cls, v: List[InputValueDto]) -> List[InputValueDto]:
        """Valida que haya al menos una entrada."""
        if not v:
            raise ValueError('Debe haber al menos un valor de entrada')
        return v
    
    # Conversión Entity ↔ DTO
    @classmethod
    def from_entity(cls, entity: FuzzyEvaluation) -> FuzzyEvaluationDto:
        """Convierte una entidad FuzzyEvaluation a DTO."""
        if not isinstance(entity, FuzzyEvaluation):
            raise TypeError("entity debe ser FuzzyEvaluation")
        
        return cls(
            id=str(entity.id) if entity.id is not None else None,
            system_id=str(entity.system_id) if entity.system_id is not None else None,
            timestamp=entity.timestamp,
            inputs=[InputValueDto.from_entity(inp) for inp in entity.inputs],
            activated_rules=[RuleActivationDto.from_entity(rule) for rule in entity.activated_rules]
        )
    
    def to_entity(self) -> FuzzyEvaluation:
        """Convierte el DTO a entidad FuzzyEvaluation."""
        from FuzzyService.Domain.ValueObjects.DomainId import FuzzyEvaluationId, FuzzySystemId
        
        return FuzzyEvaluation(
            id=FuzzyEvaluationId(self.id) if self.id else None,
            system_id=FuzzySystemId(self.system_id) if self.system_id else None,
            timestamp=self.timestamp,
            inputs=[inp.to_entity() for inp in self.inputs],
            activated_rules=[rule.to_entity() for rule in self.activated_rules]
        )
