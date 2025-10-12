from __future__ import annotations

from typing import List, Optional
from datetime import datetime

from pydantic import BaseModel, Field, ConfigDict, field_validator

from FuzzyService.Domain.ValueObjects import FuzzyVariableId, FuzzyTermId


class FuzzyVariableDto(BaseModel):
    """DTO para representar una variable difusa en la capa de aplicación/API."""

    model_config = ConfigDict(
        validate_assignment=True,
        populate_by_name=True,
        use_enum_values=True,
        arbitrary_types_allowed=True,
        extra="forbid",
    )

    # Identificación
    id: Optional[str] = Field(default=None, description="Identificador de la variable (ObjectId/UUID como string)")
    name: str = Field(min_length=1, max_length=100, description="Nombre de la variable difusa")
    description: Optional[str] = Field(default=None, description="Descripción de la variable difusa (opcional, hasta 500 caracteres)")

    # Configuración de la variable
    variable_type: str = Field(default="input", description="Tipo de variable: 'input' o 'output'")

    # Configuración de actuadores (solo para outputs)
    actuator_type: Optional[str] = Field(default=None, description="Tipo de actuador: 'PWM' o 'DIGITAL' (solo para outputs)")
    defuzzification_threshold: float = Field(default=50.0, ge=0, le=100, description="Umbral de defuzzificación para actuadores DIGITAL")
    universe_min: Optional[float] = Field(default=None, description="Valor mínimo del universo de discurso")
    universe_max: Optional[float] = Field(default=None, description="Valor máximo del universo de discurso")

    # Relaciones
    reference_code: Optional[str] = Field(default=None, min_length=1, description="Código estable: variable code en sensor-service (input) o control_output code en actuator-service (output)")
    actuator_code: Optional[str] = Field(default=None, min_length=1, description="Código del actuador para agrupar variables de salida (control + duración)")
    terms: List[str] = Field(default_factory=list, description="IDs de términos lingüísticos asociados")

    # Metadatos
    created_at: Optional[datetime] = Field(default=None, description="Fecha de creación (UTC ISO8601)")
    updated_at: Optional[datetime] = Field(default=None, description="Fecha de actualización (UTC ISO8601)")

    # --------------------- Validaciones ---------------------
    @field_validator("name")
    @classmethod
    def _validate_name(cls, v: str) -> str:
        v = (v or "").strip()
        if not v:
            raise ValueError("El nombre de la variable no puede estar vacío")
        if len(v) > 100:
            raise ValueError("El nombre de la variable no puede exceder 100 caracteres")
        return v

    @field_validator("description")
    @classmethod
    def _validate_description(cls, v: Optional[str]) -> Optional[str]:
        if v is None:
            return None
        if len(v) > 500:
            raise ValueError("La descripción no puede exceder 500 caracteres")
        return v

    @field_validator("variable_type")
    @classmethod
    def _validate_variable_type(cls, v: str) -> str:
        valid = {"input", "output"}
        if v not in valid:
            raise ValueError(f"variable_type inválido: {v}. Debe ser uno de: {sorted(valid)}")
        return v

    # --------------------- Conversión Entity ↔ DTO ---------------------
    @classmethod
    def from_entity(cls, entity: "FuzzyVariable") -> "FuzzyVariableDto":
        from FuzzyService.Domain.Entities.fuzzy_variable import FuzzyVariable  # evitar ciclos
        if not isinstance(entity, FuzzyVariable):
            raise TypeError("entity debe ser FuzzyVariable")
        return cls(
            id=str(entity.id) if entity.id is not None else None,
            name=entity.name,
            description=entity.description,
            variable_type=entity.variable_type,
            actuator_type=entity.actuator_type,
            defuzzification_threshold=entity.defuzzification_threshold,
            universe_min=entity.universe_min,
            universe_max=entity.universe_max,
            reference_code=entity.reference_code,
            actuator_code=entity.actuator_code,
            terms=[str(t) for t in entity.terms],
            created_at=entity.created_at,
            updated_at=entity.updated_at,
        )

    def to_entity(self) -> "FuzzyVariable":
        from FuzzyService.Domain.Entities.fuzzy_variable import FuzzyVariable  # evitar ciclos
        return FuzzyVariable(
            id=FuzzyVariableId(self.id) if self.id is not None else None,
            name=self.name,
            description=self.description or "",
            variable_type=self.variable_type,
            actuator_type=self.actuator_type,
            defuzzification_threshold=self.defuzzification_threshold,
            universe_min=self.universe_min,
            universe_max=self.universe_max,
            reference_code=self.reference_code,
            actuator_code=self.actuator_code,
            terms=[FuzzyTermId(t) for t in self.terms],
            created_at=self.created_at,
            updated_at=self.updated_at,
        )
