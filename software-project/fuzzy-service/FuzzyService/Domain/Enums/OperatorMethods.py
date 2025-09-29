from enum import Enum

class AndOperatorMethod(Enum):
    """
    Métodos para el operador lógico AND (t-norma).
    """
    MIN = "min"              # Intersección mínima
    PROD = "prod"            # Producto algebraico

    def __str__(self):
        return self.value

class OrOperatorMethod(Enum):
    """
    Métodos para el operador lógico OR (s-norma).
    """
    MAX = "max"              # Unión máxima
    SUM = "sum"              # Suma algebraica (acotada internamente por el motor)
    PROBOR = "probor"        # OR probabilístico

    def __str__(self):
        return self.value

class NotOperatorMethod(Enum):
    """
    Métodos para el operador lógico NOT (complemento).
    """
    COMPLEMENT = "complement"  # Complemento estándar: 1 - x

    def __str__(self):
        return self.value
