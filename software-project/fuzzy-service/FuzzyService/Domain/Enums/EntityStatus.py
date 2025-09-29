from enum import Enum

class FuzzySystemStatus(Enum):
    """
    Estados posibles de un sistema difuso.
    """
    
    DRAFT = "DRAFT"  # En desarrollo/borrador
    ACTIVE = "ACTIVE"  # Activo y funcionando
    INACTIVE = "INACTIVE"  # Inactivo temporalmente
    TESTING = "TESTING"  # En modo de pruebas
    
    @classmethod
    def get_operational_statuses(cls):
        """Retorna estados en los que el sistema puede operar."""
        return [cls.ACTIVE, cls.TESTING]
    
    @classmethod
    def get_editable_statuses(cls):
        """Retorna estados en los que el sistema puede ser editado."""
        return [cls.DRAFT, cls.INACTIVE]
    
    def __str__(self):
        return self.value

class FuzzyRuleStatus(Enum): #borrar porque regla no tiene estado
    """
    Estados posibles de una regla difusa.
    """
    
    ACTIVE = "ACTIVE"  # Regla activa
    INACTIVE = "INACTIVE"  # Regla inactiva
    TESTING = "TESTING"  # En modo de pruebas
    INVALID = "INVALID"  # Configuración inválida
    
    def __str__(self):
        return self.value

class FuzzyVariableType(Enum):
    """
    Tipos de variables difusas según su uso en el sistema.
    """
    
    INPUT = "INPUT"  # Variable de entrada (sensores)
    OUTPUT = "OUTPUT"  # Variable de salida (actuadores)
    
    def __str__(self):
        return self.value

class EvaluationStatus(Enum):
    """
    Estados de una evaluación difusa.
    """
    
    PENDING = "PENDING"  # Pendiente de evaluación
    PROCESSING = "PROCESSING"  # En proceso
    COMPLETED = "COMPLETED"  # Completada exitosamente
    FAILED = "FAILED"  # Falló la evaluación
    CANCELLED = "CANCELLED"  # Cancelada
    
    def __str__(self):
        return self.value
