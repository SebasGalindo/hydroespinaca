"""
Entidad de dominio para consecuentes de reglas difusas.

Este modelo permite que las reglas apunten directamente a variables de salida
con múltiples términos, habilitando agregación multi-regla Mamdani completa.
"""

from datetime import datetime, timezone
from typing import List, Optional, Dict, Any

from pydantic import Field, field_validator, model_validator

from ..ValueObjects import FuzzyVariableId, FuzzyTermId
from ..Common import DomainBaseModel


class RuleConsequent(DomainBaseModel):
    """Consecuente de una regla difusa que apunta directamente a una variable de salida.

    Estructura:
    {
        "variable_id": "68e05364d86d6edc39982875",
        "terms": ["68e05364d86d6edc39982890", "68e05364d86d6edc39982891"],
        "aggregation_method": "max"
    }

    Permite:
    - Múltiples términos por variable de salida (multi-regla)
    - Agregación configurable (max, sum, probabilistic_or)
    - Defuzzificación continua mediante centroide
    """

    # Variable de salida objetivo
    variable_id: FuzzyVariableId

    # Lista de términos difusos que esta regla activa para esta variable
    # Cuando múltiples reglas afectan la misma variable, se agregan
    terms: List[FuzzyTermId] = Field(default_factory=list)

    # Método de agregación para combinar contribuciones
    # Opciones: "max" (Mamdani estándar), "sum", "probabilistic_or"
    aggregation_method: str = "max"

    # -------------------------
    # Validaciones Pydantic
    # -------------------------
    @field_validator("variable_id")
    @classmethod
    def _validate_variable_id(cls, v: FuzzyVariableId) -> FuzzyVariableId:
        if not v or str(v).strip() == "":
            raise ValueError("variable_id no puede estar vacío")
        return v

    @field_validator("terms")
    @classmethod
    def _validate_terms(cls, v: List[FuzzyTermId]) -> List[FuzzyTermId]:
        if not v or len(v) == 0:
            raise ValueError("Un consecuente debe tener al menos un término")

        # Validar que no haya términos duplicados
        term_strings = [str(t) for t in v]
        if len(term_strings) != len(set(term_strings)):
            raise ValueError("No se permiten términos duplicados en un consecuente")

        return v

    @field_validator("aggregation_method")
    @classmethod
    def _validate_aggregation_method(cls, v: str) -> str:
        valid_methods = ["max", "sum", "probabilistic_or"]
        if v not in valid_methods:
            raise ValueError(
                f"aggregation_method inválido: {v}. "
                f"Debe ser uno de: {valid_methods}"
            )
        return v

    # -------------------------
    # Métodos de negocio
    # -------------------------
    def add_term(self, term_id: FuzzyTermId):
        """Agrega un término a este consecuente."""
        if term_id not in self.terms:
            self.terms.append(term_id)

    def remove_term(self, term_id: FuzzyTermId):
        """Remueve un término de este consecuente."""
        if term_id in self.terms:
            self.terms.remove(term_id)

    def has_term(self, term_id: FuzzyTermId) -> bool:
        """Verifica si el consecuente contiene un término específico."""
        return term_id in self.terms

    def get_term_count(self) -> int:
        """Retorna el número de términos en este consecuente."""
        return len(self.terms)

    # -------------------------
    # Serialización
    # -------------------------
    def to_dict(self) -> Dict[str, Any]:
        return {
            "variable_id": str(self.variable_id),
            "terms": [str(t) for t in self.terms],
            "aggregation_method": self.aggregation_method
        }

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "RuleConsequent":
        """Crea un RuleConsequent desde un diccionario."""
        return cls(
            variable_id=FuzzyVariableId(data["variable_id"]),
            terms=[FuzzyTermId(t) for t in data.get("terms", [])],
            aggregation_method=data.get("aggregation_method", "max")
        )

    def __str__(self) -> str:
        return f"RuleConsequent(variable={self.variable_id}, terms={len(self.terms)}, method={self.aggregation_method})"
