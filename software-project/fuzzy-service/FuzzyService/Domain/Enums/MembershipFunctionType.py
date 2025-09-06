from enum import Enum

class MembershipFunctionType(Enum):
    """
    Tipos de funciones de membresía soportadas por el sistema difuso.
    Basado en las funciones disponibles en scikit-fuzzy.
    """
    
    # Funciones básicas
    TRIANGULAR = "triangular"
    TRAPEZOIDAL = "trapezoidal"
    GAUSSIAN = "gaussian"
    
    # Funciones avanzadas
    SIGMOID = "sigmoid"
    BELL = "bell"
    PI_SHAPED = "pi_shaped"
    S_SHAPED = "s_shaped"
    Z_SHAPED = "z_shaped"
    
    # Funciones lineales
    LINEAR = "linear"
    CONSTANT = "constant"
    
    @classmethod
    def get_supported_types(cls):
        """Retorna una lista de todos los tipos soportados."""
        return [member.value for member in cls]
    
    @classmethod
    def is_valid_type(cls, function_type: str) -> bool:
        """Verifica si un tipo de función es válido."""
        return function_type in cls.get_supported_types()
    
    def __str__(self):
        return self.value
