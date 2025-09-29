from enum import Enum

class LogicalOperator(Enum):
    """
    Operadores lógicos utilizados en las condiciones de las reglas difusas.
    """
    
    # Operadores de comparación
    IS = "IS"
    IS_NOT = "IS_NOT"
    NOT = "NOT"
    
    # Operadores de rango
    GREATER_THAN = "GREATER_THAN"
    LESS_THAN = "LESS_THAN"
    GREATER_EQUAL = "GREATER_EQUAL"
    LESS_EQUAL = "LESS_EQUAL"
    BETWEEN = "BETWEEN"
    
    # Operadores de conjunto
    IN = "IN"
    NOT_IN = "NOT_IN"
    
    @classmethod
    def get_comparison_operators(cls):
        """Retorna operadores de comparación básicos."""
        return [cls.IS, cls.IS_NOT, cls.NOT]
    
    @classmethod
    def get_range_operators(cls):
        """Retorna operadores de rango numérico."""
        return [cls.GREATER_THAN, cls.LESS_THAN, cls.GREATER_EQUAL, cls.LESS_EQUAL, cls.BETWEEN]
    
    @classmethod
    def get_set_operators(cls):
        """Retorna operadores de conjunto."""
        return [cls.IN, cls.NOT_IN]
    
    # Nota: En sistemas fuzzy solo se usan etiquetas lingüísticas
    # Los operadores numéricos están comentados ya que no se utilizan
    # @staticmethod
    # def requires_numeric_value(operator: 'LogicalOperator') -> bool:
    #     """Verifica si el operador requiere un valor numérico."""
    #     return operator in [LogicalOperator.GREATER_THAN, LogicalOperator.LESS_THAN,
    #                       LogicalOperator.GREATER_EQUAL, LogicalOperator.LESS_EQUAL]
    # 
    # @staticmethod
    # def validate_numeric_operation(operator: 'LogicalOperator', value: Any) -> bool:
    #     """Valida si el valor es apropiado para operaciones numéricas."""
    #     if not LogicalOperator.requires_numeric_value(operator):
    #         return True
    #     
    #     try:
    #         float(value)
    #         return True
    #     except (ValueError, TypeError):
    #         return False
    
    def __str__(self):
        return self.value

class RuleConnector(Enum):
    """
    Conectores lógicos para unir múltiples condiciones en una regla.
    """
    
    AND = "AND"
    OR = "OR"
    
    def __str__(self):
        return self.value
