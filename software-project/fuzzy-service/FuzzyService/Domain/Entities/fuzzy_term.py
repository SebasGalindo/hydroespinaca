from typing import Optional, Dict, Any
from datetime import datetime, timezone

from pydantic import field_validator, model_validator, Field

from ..ValueObjects import FuzzyTermId, FuzzyVariableId, MembershipFunction
from ..Enums import MembershipFunctionType
from ..Common import DomainBaseModel


class FuzzyTerm(DomainBaseModel):
    """Entidad del dominio que representa un término difuso.
    Basado en el JSON del plan:
    {
      "_id": { "$oid": "term_temp_high" },
      "variableId": { "$oid": "var_temp_air" },
      "label": "high",
      "mf": {
        "type": "trapezoidal|triangular|gaussian",
        "params": [28, 30, 40, 40]
      },
      "createdAt": "2025-01-27T23:39:17.917+00:00"
    }

    Nota: En el dominio usamos el Value Object MembershipFunction, cuyo "type" es un enum (MembershipFunctionType).
    La evaluación y cálculos numéricos NO pertenecen a la entidad (Clean Architecture / DDD),
    por eso esta clase sólo mantiene identidad, relaciones e invariantes.
    """

    # Identidad y relaciones
    id: Optional[FuzzyTermId] = None
    variable_id: Optional[FuzzyVariableId] = None

    # Configuración del término
    label: str = ""
    membership_function: MembershipFunction = Field(
        default_factory=lambda: MembershipFunction(
            function_type=MembershipFunctionType.TRIANGULAR,
            parameters=[0.0, 0.5, 1.0],
            universe_min=0.0,
            universe_max=1.0,
        )
    )

    # Metadatos
    created_at: Optional[datetime] = None
    updated_at: Optional[datetime] = None

    # -------------------------
    # Validaciones Pydantic
    # -------------------------
    @field_validator("label")
    @classmethod
    def _validate_label(cls, v: str) -> str:
        if not v or not v.strip():
            raise ValueError("La etiqueta lingüística no puede estar vacía")
        if len(v.strip()) < 1:
            raise ValueError("La etiqueta lingüística debe tener al menos 1 caracter")
        if len(v) > 30:
            raise ValueError("La etiqueta lingüística no puede exceder 30 caracteres")
        return v.strip()

    @field_validator("membership_function")
    @classmethod
    def _validate_membership_function(cls, v: MembershipFunction) -> MembershipFunction:
        if not isinstance(v, MembershipFunction):
            raise ValueError("La función de membresía debe ser una instancia de MembershipFunction")
        return v

    @model_validator(mode="after")
    def _ensure_timestamps(self):
        if self.created_at is None:
            self.created_at = datetime.now(timezone.utc)
        if self.updated_at is None:
            self.updated_at = self.created_at
        return self

    # -------------------------
    # Métodos de negocio mínimos (invariantes de la entidad)
    # -------------------------
    def update_label(self, new_label: str):
        if not new_label or not new_label.strip():
            raise ValueError("La etiqueta lingüística no puede estar vacía")
        if len(new_label) > 30:
            raise ValueError("La etiqueta lingüística no puede exceder 30 caracteres")
        self.label = new_label.strip()
        self.updated_at = datetime.now(timezone.utc)

    def update_membership_function(self, new_function: MembershipFunction):
        if not isinstance(new_function, MembershipFunction):
            raise ValueError("La función de membresía debe ser una instancia de MembershipFunction")
        self.membership_function = new_function
        self.updated_at = datetime.now(timezone.utc)

    # -------------------------
    # Serialización simple (para capa de aplicación/infra)
    # -------------------------
    def to_dict(self) -> Dict[str, Any]:
        return {
            "_id": str(self.id) if self.id else None,
            "variableId": str(self.variable_id) if self.variable_id else None,
            "label": self.label,
            "mf": {
                # Usamos directamente el valor del enum; mapeos a sinónimos (triangle/trapezoid)
                # deben manejarse en la capa de aplicación si se requieren.
                "type": self.membership_function.function_type.value,
                "params": list(self.membership_function.parameters),
            },
            "createdAt": self.created_at.isoformat() if self.created_at else None,
            "updatedAt": self.updated_at.isoformat() if self.updated_at else None,
        }

    def __str__(self) -> str:
        return f"FuzzyTerm(label='{self.label}', type='{self.membership_function.function_type.value}')"

    def __repr__(self) -> str:
        return (
            f"FuzzyTerm(id={self.id}, variable_id={self.variable_id}, "
            f"label='{self.label}', mf_type='{self.membership_function.function_type.value}')"
        )
