from typing import List
from pydantic import field_validator, model_validator
from ..Enums import LogicalOperator
from ..Common import DomainBaseModel


class RuleCondition(DomainBaseModel):
    """
    Value object que representa una condición individual en una regla difusa.
    """

    condition_id: str
    sensor_name: str  # Nombre de la variable de entrada
    operator: LogicalOperator
    target_value: str | List[str]  # Etiqueta lingüística o lista de etiquetas

    @field_validator("condition_id", "sensor_name")
    @classmethod
    def _not_empty(cls, v: str) -> str:
        if not v or not v.strip():
            raise ValueError("El valor no puede estar vacío")
        return v

    @model_validator(mode="after")
    def _validate_by_operator(self):
        # BETWEEN no aplica en nuestro dominio de etiquetas lingüísticas, pero mantenemos la validación si se usa
        if self.operator == LogicalOperator.BETWEEN:
            if not isinstance(self.target_value, list) or len(self.target_value) != 2:
                raise ValueError("Operador BETWEEN requiere una lista de 2 valores [min, max]")
            min_val, max_val = self.target_value
            if not isinstance(min_val, str) or not isinstance(max_val, str):
                raise ValueError("Para BETWEEN en este dominio, se esperan etiquetas lingüísticas como strings")
            if not min_val.strip() or not max_val.strip():
                raise ValueError("Para BETWEEN: ambas etiquetas deben ser strings no vacíos")

        elif self.operator in [LogicalOperator.IN, LogicalOperator.NOT_IN]:
            if not isinstance(self.target_value, list) or len(self.target_value) == 0:
                raise ValueError(f"Operador {self.operator.value} requiere una lista no vacía de valores")
            if not all(isinstance(v, str) and v.strip() for v in self.target_value):
                raise ValueError(f"Operador {self.operator.value} requiere una lista de etiquetas lingüísticas válidas")

        elif self.operator in [LogicalOperator.IS, LogicalOperator.NOT]:
            if not isinstance(self.target_value, str) or not self.target_value.strip():
                raise ValueError(f"El operador {self.operator.value} requiere una etiqueta lingüística válida")

        # Los operadores numéricos no se usan en sistemas fuzzy con etiquetas
        if self.operator in [
            LogicalOperator.GREATER_THAN,
            LogicalOperator.LESS_THAN,
            LogicalOperator.GREATER_EQUAL,
            LogicalOperator.LESS_EQUAL,
        ]:
            raise ValueError(
                f"El operador {self.operator.value} no es válido en sistemas fuzzy basados en etiquetas lingüísticas."
            )
        return self

    def is_numeric_condition(self) -> bool:
        """Compatibilidad: indica si la condición parece numérica (no usada en este dominio)."""
        # Dado que usamos etiquetas, esto siempre será False, pero mantenemos compatibilidad
        if isinstance(self.target_value, list):
            return all(isinstance(v, (int, float)) for v in self.target_value)
        return isinstance(self.target_value, (int, float))

    def is_linguistic_condition(self) -> bool:
        """Verifica si la condición usa términos lingüísticos."""
        return isinstance(self.target_value, str) or (
            isinstance(self.target_value, list) and all(isinstance(v, str) for v in self.target_value)
        )

    def get_numeric_values(self) -> List[float]:
        """Compatibilidad: retorna valores numéricos si existieran."""
        if isinstance(self.target_value, (int, float)):
            return [float(self.target_value)]
        elif isinstance(self.target_value, list):
            return [float(v) for v in self.target_value if isinstance(v, (int, float))]
        return []

    def get_linguistic_term(self) -> str | None:
        """Retorna el término lingüístico si aplica (solo para target_value str)."""
        return self.target_value if isinstance(self.target_value, str) else None

    def to_dict(self) -> dict:
        return {
            "condition_id": self.condition_id,
            "sensor_name": self.sensor_name,
            "operator": self.operator.value,
            "target_value": self.target_value,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "RuleCondition":
        return cls(
            condition_id=data["condition_id"],
            sensor_name=data["sensor_name"],
            operator=LogicalOperator(data["operator"]),
            target_value=data["target_value"],
        )

    def __str__(self) -> str:
        return f"{self.sensor_name} {self.operator.value} {self.target_value}"
