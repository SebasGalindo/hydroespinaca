from typing import List
from pydantic import field_validator
from ..Enums import MembershipFunctionType
from ..Common import DomainBaseModel


class MembershipFunction(DomainBaseModel):
    """
    Value object que representa una función de membresía difusa.
    Inmutable y con validaciones de integridad.
    """

    function_type: MembershipFunctionType
    parameters: List[float]
    universe_min: float
    universe_max: float
    
    @field_validator('function_type', mode='after')
    @classmethod
    def validate_function_type(cls, v):
        """Asegurar que function_type sea un enum MembershipFunctionType."""
        if isinstance(v, str):
            return MembershipFunctionType(v)
        elif isinstance(v, MembershipFunctionType):
            return v
        else:
            raise ValueError(f"function_type debe ser string o MembershipFunctionType, recibido: {type(v)}")

    def _validate_parameters(self):
        if not self.parameters:
            raise ValueError("Los parámetros no pueden estar vacíos")

        param_count = len(self.parameters)

        if self.function_type == MembershipFunctionType.TRIANGULAR:
            if param_count != 3:
                raise ValueError("Función triangular requiere exactamente 3 parámetros [a, b, c]")
            a, b, c = self.parameters
            if not (a <= b <= c):
                raise ValueError("Para función triangular: a <= b <= c")

        elif self.function_type == MembershipFunctionType.TRAPEZOIDAL:
            if param_count != 4:
                raise ValueError("Función trapezoidal requiere exactamente 4 parámetros [a, b, c, d]")
            a, b, c, d = self.parameters
            if not (a <= b <= c <= d):
                raise ValueError("Para función trapezoidal: a <= b <= c <= d")

        elif self.function_type == MembershipFunctionType.GAUSSIAN:
            if param_count != 2:
                raise ValueError("Función gaussiana requiere exactamente 2 parámetros [mean, sigma]")
            mean, sigma = self.parameters
            if sigma <= 0:
                raise ValueError("Sigma debe ser mayor que 0 para función gaussiana")

        elif self.function_type == MembershipFunctionType.SIGMOID:
            if param_count != 2:
                raise ValueError("Función sigmoide requiere exactamente 2 parámetros [a, c]")

        elif self.function_type == MembershipFunctionType.BELL:
            if param_count != 3:
                raise ValueError("Función campana requiere exactamente 3 parámetros [a, b, c]")
            a, b, c = self.parameters
            if a <= 0 or b <= 0:
                raise ValueError("Parámetros a y b deben ser mayores que 0 para función campana")

    def _validate_universe(self):
        if self.universe_min >= self.universe_max:
            raise ValueError("universe_min debe ser menor que universe_max")

    def model_post_init(self, __context):
        # Ejecutar validaciones después de construir el modelo
        self._validate_parameters()
        self._validate_universe()

    def get_parameter_names(self) -> List[str]:
        parameter_names = {
            MembershipFunctionType.TRIANGULAR: ["a", "b", "c"],
            MembershipFunctionType.TRAPEZOIDAL: ["a", "b", "c", "d"],
            MembershipFunctionType.GAUSSIAN: ["mean", "sigma"],
            MembershipFunctionType.SIGMOID: ["a", "c"],
            MembershipFunctionType.BELL: ["a", "b", "c"],
        }
        return parameter_names.get(self.function_type, [f"param_{i}" for i in range(len(self.parameters))])

    def to_dict(self) -> dict:
        # Manejar tanto enum como string debido a use_enum_values=True
        function_type_value = self.function_type.value if hasattr(self.function_type, 'value') else self.function_type
        return {
            "function_type": function_type_value,
            "parameters": self.parameters,
            "universe_min": self.universe_min,
            "universe_max": self.universe_max,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "MembershipFunction":
        """Crea una instancia desde un diccionario."""
        function_type_raw = data["function_type"]
        function_type = MembershipFunctionType(function_type_raw)
        
        return cls(
            function_type=function_type,
            parameters=data["parameters"],
            universe_min=data["universe_min"],
            universe_max=data["universe_max"]
        )

    def __str__(self) -> str:
        return f"{self.function_type.value}({', '.join(map(str, self.parameters))})"
