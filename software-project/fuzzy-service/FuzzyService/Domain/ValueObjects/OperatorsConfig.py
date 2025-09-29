from ..Enums import AndOperatorMethod, OrOperatorMethod, NotOperatorMethod
from ..Common import DomainBaseModel


class OperatorsConfig(DomainBaseModel):
    """
    Value Object para la configuración de operadores lógicos del sistema difuso.
    Corresponde al campo "operators" del JSON del plan.
    """

    and_method: AndOperatorMethod = AndOperatorMethod.MIN
    or_method: OrOperatorMethod = OrOperatorMethod.MAX
    not_method: NotOperatorMethod = NotOperatorMethod.COMPLEMENT

    def to_dict(self) -> dict:
        return {
            "and": self.and_method.value if hasattr(self.and_method, 'value') else str(self.and_method),
            "or": self.or_method.value if hasattr(self.or_method, 'value') else str(self.or_method),
            "not": self.not_method.value if hasattr(self.not_method, 'value') else str(self.not_method),
        }

    @classmethod
    def from_dict(cls, data: dict) -> "OperatorsConfig":
        return cls(
            and_method=AndOperatorMethod(data.get("and", AndOperatorMethod.MIN.value)),
            or_method=OrOperatorMethod(data.get("or", OrOperatorMethod.MAX.value)),
            not_method=NotOperatorMethod(data.get("not", NotOperatorMethod.COMPLEMENT.value)),
        )

    def __str__(self) -> str:
        return f"operators(and={self.and_method.value}, or={self.or_method.value}, not={self.not_method.value})"
