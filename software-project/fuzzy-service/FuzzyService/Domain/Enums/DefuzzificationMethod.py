from enum import Enum

class DefuzzificationMethod(Enum):
    """
    Métodos de defuzzificación soportados por el sistema.
    Basado en los métodos disponibles en scikit-fuzzy.
    """
    
    # Métodos de área
    CENTROID = "centroid"  # Centro de gravedad (más común)
    BISECTOR = "bisector"  # Bisector del área
    
    # Métodos de máximo
    MOM = "mom"  # Mean of Maximum
    SOM = "som"  # Smallest of Maximum
    LOM = "lom"  # Largest of Maximum
    
    # Métodos de altura
    WEIGHTED_AVERAGE = "weighted_average"  # Promedio ponderado
    
    @classmethod
    def get_area_methods(cls):
        """Retorna métodos basados en área."""
        return [cls.CENTROID, cls.BISECTOR]
    
    @classmethod
    def get_maximum_methods(cls):
        """Retorna métodos basados en máximo."""
        return [cls.MOM, cls.SOM, cls.LOM]
    
    @classmethod
    def get_default_method(cls):
        """Retorna el método por defecto recomendado."""
        return cls.CENTROID
    
    @classmethod
    def is_valid_method(cls, method: str) -> bool:
        """Verifica si un método de defuzzificación es válido."""
        return method in [member.value for member in cls]
    
    def __str__(self):
        return self.value

class AggregationMethod(Enum):
    """
    Métodos de agregación para combinar múltiples reglas.
    """
    
    MAX = "max"  # Máximo (OR lógico)
    SUM = "sum"  # Suma (más común para múltiples salidas)
    PROBOR = "probor"  # OR probabilístico
    
    @classmethod
    def get_default_method(cls):
        """Retorna el método por defecto."""
        return cls.MAX
    
    def __str__(self):
        return self.value
