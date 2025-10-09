from __future__ import annotations

from typing import Optional, List
from datetime import datetime

from pydantic import BaseModel, Field, ConfigDict, field_validator

from FuzzyService.Domain.ValueObjects import FuzzyTermId, FuzzyVariableId, MembershipFunction
from FuzzyService.Domain.Enums import MembershipFunctionType


class MembershipFunctionDto(BaseModel):
    """DTO para representar una función de membresía en la capa de aplicación/API."""

    model_config = ConfigDict(validate_assignment=True, use_enum_values=True, extra="forbid")

    function_type: str = Field(..., description="Tipo de función de membresía")
    parameters: List[float] = Field(..., description="Parámetros de la función de membresía")
    universe_min: float = Field(..., description="Mínimo del universo de discurso")
    universe_max: float = Field(..., description="Máximo del universo de discurso")

    @field_validator("function_type")
    @classmethod
    def _validate_function_type(cls, v: str) -> str:
        v = (v or "").strip().lower()
        if not MembershipFunctionType.is_valid_type(v):
            raise ValueError(f"function_type inválido: {v}. Tipos soportados: {MembershipFunctionType.get_supported_types()}")
        return v

    @field_validator("parameters")
    @classmethod
    def _validate_parameters(cls, params: List[float]) -> List[float]:
        if params is None or len(params) == 0:
            raise ValueError("parameters no puede estar vacío")
        return params

    @field_validator("universe_max")
    @classmethod
    def _validate_universe(cls, max_v: float, info) -> float:  # type: ignore[override]
        # Access to other fields is limited in field validators; cross-check in DTO->Domain conversion.
        return max_v

    def to_value_object(self) -> MembershipFunction:
        return MembershipFunction(
            function_type=MembershipFunctionType(self.function_type),
            parameters=self.parameters,
            universe_min=self.universe_min,
            universe_max=self.universe_max,
        )

    @classmethod
    def from_value_object(cls, vo: MembershipFunction) -> "MembershipFunctionDto":
        # Manejar tanto enum como string debido a use_enum_values=True
        function_type_value = vo.function_type.value if hasattr(vo.function_type, 'value') else vo.function_type
        return cls(
            function_type=function_type_value,
            parameters=list(vo.parameters),
            universe_min=vo.universe_min,
            universe_max=vo.universe_max,
        )


class FuzzyTermDto(BaseModel):
    """DTO para representar un término difuso (etiqueta lingüística)."""

    model_config = ConfigDict(
        validate_assignment=True,
        populate_by_name=True,
        use_enum_values=True,
        arbitrary_types_allowed=True,
        extra="forbid",
    )

    # Identificación y relación
    id: Optional[str] = Field(default=None, description="Identificador del término (ObjectId/UUID como string)")
    variable_id: Optional[str] = Field(default=None, description="ID de la variable difusa a la que pertenece")

    # Configuración
    label: str = Field(min_length=1, max_length=30, description="Etiqueta lingüística del término")
    membership_function: MembershipFunctionDto = Field(..., description="Función de membresía asociada al término")

    # Metadatos
    created_at: Optional[datetime] = Field(default=None, description="Fecha de creación (UTC ISO8601)")
    updated_at: Optional[datetime] = Field(default=None, description="Fecha de actualización (UTC ISO8601)")

    # --------------------- Validaciones ---------------------
    @field_validator("label")
    @classmethod
    def _validate_label(cls, v: str) -> str:
        v = (v or "").strip()
        if not v:
            raise ValueError("La etiqueta lingüística no puede estar vacía")
        if len(v) > 30:
            raise ValueError("La etiqueta lingüística no puede exceder 30 caracteres")
        return v

    # --------------------- Conversión Entity ↔ DTO ---------------------
    @classmethod
    def from_entity(cls, entity: "FuzzyTerm") -> "FuzzyTermDto":
        from FuzzyService.Domain.Entities.fuzzy_term import FuzzyTerm  # evitar ciclos
        if not isinstance(entity, FuzzyTerm):
            raise TypeError("entity debe ser FuzzyTerm")
        return cls(
            id=str(entity.id) if entity.id is not None else None,
            variable_id=str(entity.variable_id) if entity.variable_id is not None else None,
            label=entity.label,
            membership_function=MembershipFunctionDto.from_value_object(entity.membership_function),
            created_at=entity.created_at,
            updated_at=entity.updated_at,
        )

    def to_entity(self) -> "FuzzyTerm":
        from FuzzyService.Domain.Entities.fuzzy_term import FuzzyTerm  # evitar ciclos
        return FuzzyTerm(
            id=FuzzyTermId(self.id) if self.id is not None else None,
            variable_id=FuzzyVariableId(self.variable_id) if self.variable_id is not None else None,
            label=self.label,
            membership_function=self.membership_function.to_value_object(),
            created_at=self.created_at,
            updated_at=self.updated_at,
        )
