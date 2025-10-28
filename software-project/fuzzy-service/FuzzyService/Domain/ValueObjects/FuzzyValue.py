import math
from typing import Optional, Tuple
from pydantic import field_validator
from ..Common import DomainBaseModel


class FuzzyValue(DomainBaseModel):
    """
    Value object que representa un valor difuso con su grado de membresía.
    """

    crisp_value: float  # Valor nítido (crisp)
    membership_degree: float  # Grado de membresía [0, 1]
    linguistic_label: Optional[str] = None  # Etiqueta lingüística opcional

    @field_validator("membership_degree")
    @classmethod
    def _validate_membership_degree(cls, v: float) -> float:
        if not (0.0 <= v <= 1.0):
            raise ValueError(f"El grado de membresía debe estar entre 0 y 1, recibido: {v}")
        return float(v)

    def is_fully_member(self) -> bool:
        # Use tolerance for floating-point comparison
        return abs(self.membership_degree - 1.0) < 1e-9

    def is_not_member(self) -> bool:
        # Use tolerance for floating-point comparison
        return abs(self.membership_degree) < 1e-9

    def is_partial_member(self) -> bool:
        # Member but not fully: between 0 and 1, exclusive
        return self.membership_degree > 1e-9 and self.membership_degree < (1.0 - 1e-9)

    def to_dict(self) -> dict:
        return self.model_dump()

    @classmethod
    def from_dict(cls, data: dict) -> "FuzzyValue":
        return cls(**data)

    def __str__(self) -> str:
        label = f" ({self.linguistic_label})" if self.linguistic_label else ""
        return f"{self.crisp_value}[{self.membership_degree:.3f}]{label}"


class FuzzySet(DomainBaseModel):
    """
    Value object que representa un conjunto difuso como colección de valores difusos.
    """

    name: str
    values: Tuple[FuzzyValue, ...]

    @field_validator("name")
    @classmethod
    def _validate_name(cls, v: str) -> str:
        if not v or not v.strip():
            raise ValueError("El nombre del conjunto difuso no puede estar vacío")
        return v

    @field_validator("values")
    @classmethod
    def _validate_values(cls, v: Tuple[FuzzyValue, ...]) -> Tuple[FuzzyValue, ...]:
        if not v or len(v) == 0:
            raise ValueError("Un conjunto difuso no puede estar vacío")
        return v

    def get_max_membership(self) -> FuzzyValue:
        return max(self.values, key=lambda fv: fv.membership_degree)

    def get_min_membership(self) -> FuzzyValue:
        return min(self.values, key=lambda fv: fv.membership_degree)

    def get_support(self) -> Tuple[FuzzyValue, ...]:
        return tuple(v for v in self.values if v.membership_degree > 0.0)

    def get_core(self) -> Tuple[FuzzyValue, ...]:
        return tuple(v for v in self.values if math.isclose(v.membership_degree, 1.0, rel_tol=1e-9, abs_tol=1e-9))

    def get_alpha_cut(self, alpha: float) -> Tuple[FuzzyValue, ...]:
        if not (0.0 <= alpha <= 1.0):
            raise ValueError(f"Alpha debe estar entre 0 y 1, recibido: {alpha}")
        return tuple(v for v in self.values if v.membership_degree >= alpha)

    def size(self) -> int:
        return len(self.values)

    def to_dict(self) -> dict:
        return {
            "name": self.name,
            "values": [v.model_dump() for v in self.values],
        }

    @classmethod
    def from_dict(cls, data: dict) -> "FuzzySet":
        values = tuple(FuzzyValue(**v) for v in data["values"]) if "values" in data else tuple()
        return cls(name=data["name"], values=values)

    def __str__(self) -> str:
        return f"FuzzySet({self.name}): {len(self.values)} values"
