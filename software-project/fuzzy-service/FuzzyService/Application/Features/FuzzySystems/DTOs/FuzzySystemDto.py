from __future__ import annotations

from typing import List, Optional
from datetime import datetime

from pydantic import BaseModel, Field, ConfigDict, field_validator

from FuzzyService.Domain.Enums import FuzzySystemStatus, DefuzzificationMethod
from FuzzyService.Domain.ValueObjects import FuzzySystemId, FuzzyVariableId, FuzzyRuleId, OperatorsConfig


class FuzzySystemDto(BaseModel):
    """DTO para representar un sistema difuso en la capa de aplicación/API."""

    model_config = ConfigDict(
        validate_assignment=True,
        populate_by_name=True,
        use_enum_values=True,
        arbitrary_types_allowed=True,
        extra="forbid",
    )

    # Identificación
    id: Optional[str] = Field(default=None, description="Identificador del sistema (ObjectId/UUID como string)")
    name: str = Field(min_length=3, max_length=100, description="Nombre del sistema difuso")

    # Estado y configuración
    status: FuzzySystemStatus = Field(default=FuzzySystemStatus.DRAFT, description="Estado del sistema")
    defuzzification_method: DefuzzificationMethod = Field(
        default=DefuzzificationMethod.CENTROID, description="Método de defuzzificación"
    )
    operators: OperatorsConfig = Field(default_factory=OperatorsConfig, description="Configuración de operadores")

    # Relaciones (IDs como strings)
    input_variable_ids: List[str] = Field(default_factory=list, description="IDs de variables de entrada")
    output_variable_ids: List[str] = Field(default_factory=list, description="IDs de variables de salida")
    rule_ids: List[str] = Field(default_factory=list, description="IDs de reglas del sistema")

    # Metadatos
    created_at: Optional[datetime] = Field(default=None, description="Fecha de creación (UTC ISO8601)")
    updated_at: Optional[datetime] = Field(default=None, description="Fecha de actualización (UTC ISO8601)")
    created_by: Optional[str] = Field(default=None, description="Usuario creador")

    # Validaciones adicionales
    @field_validator("name")
    @classmethod
    def _validate_name(cls, v: str) -> str:
        v = v.strip()
        if not v:
            raise ValueError("El nombre del sistema no puede estar vacío")
        return v

    # --------------------- Conversión Entity ↔ DTO ---------------------
    @classmethod
    def from_entity(cls, entity: "FuzzySystem") -> "FuzzySystemDto":
        from FuzzyService.Domain.Entities.fuzzy_system import FuzzySystem  # import local para evitar ciclos

        if not isinstance(entity, FuzzySystem):
            raise TypeError("entity debe ser FuzzySystem")
        return cls(
            id=str(entity.id) if entity.id is not None else None,
            name=entity.name,
            status=entity.status,
            defuzzification_method=entity.defuzzification_method,
            operators=entity.operators,
            input_variable_ids=[str(v) for v in entity.input_variable_ids],
            output_variable_ids=[str(v) for v in entity.output_variable_ids],
            rule_ids=[str(r) for r in entity.rule_ids],
            created_at=entity.created_at,
            updated_at=entity.updated_at,
            created_by=entity.created_by,
        )

    def to_entity(self) -> "FuzzySystem":
        from FuzzyService.Domain.Entities.fuzzy_system import FuzzySystem  # import local para evitar ciclos

        return FuzzySystem(
            id=FuzzySystemId(self.id) if self.id is not None else None,
            name=self.name,
            status=self.status,
            defuzzification_method=self.defuzzification_method,
            operators=self.operators if isinstance(self.operators, OperatorsConfig) else OperatorsConfig(**self.operators),
            input_variable_ids=[FuzzyVariableId(v) for v in self.input_variable_ids],
            output_variable_ids=[FuzzyVariableId(v) for v in self.output_variable_ids],
            rule_ids=[FuzzyRuleId(r) for r in self.rule_ids],
            created_at=self.created_at,
            updated_at=self.updated_at,
            created_by=self.created_by,
        )
