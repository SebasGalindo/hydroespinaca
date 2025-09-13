"""Domain service interface for the fuzzy inference engine.

This engine encapsulates the fuzzy logic evaluation independent from infrastructure.
"""

from abc import ABC, abstractmethod
from typing import List, Dict, Any, Optional
from datetime import datetime

from ..Entities.fuzzy_variable import FuzzyVariable
from ..Entities.fuzzy_term import FuzzyTerm
from ..Entities.fuzzy_rule import FuzzyRule
from ..Entities.fuzzy_system import FuzzySystem

# Import types from Infrastructure for results
try:
    from FuzzyService.Infrastructure.ExternalServices.FuzzyEngine.FuzzificationTypes import FuzzificationResult
    from FuzzyService.Infrastructure.ExternalServices.FuzzyEngine.RuleEvaluationEngine import BatchRuleEvaluationResult
except ImportError:
    # Fallback types if infrastructure not available
    FuzzificationResult = Any
    BatchRuleEvaluationResult = Any


class IFuzzyEngine(ABC):
    """Domain service responsible for fuzzy logic operations.
    
    This interface defines the contract for fuzzy engines that can:
    - Fuzzify sensor readings into membership degrees
    - Evaluate fuzzy rules and calculate firing strengths
    - Perform complete fuzzy inference workflows
    """

    @abstractmethod
    async def fuzzify_sensor_readings(
        self, 
        variables: List[FuzzyVariable], 
        terms: List[FuzzyTerm], 
        sensor_readings: Dict[str, float]
    ) -> List[FuzzificationResult]:
        """Realiza la fuzzificación de las lecturas de sensores.
        
        Args:
            variables: Lista de variables fuzzy del sistema
            terms: Lista de términos fuzzy asociados a las variables
            sensor_readings: Diccionario {sensor_id: valor_crisp}
            
        Returns:
            Lista de resultados de fuzzificación por variable
            
        Raises:
            ValidationError: Si hay problemas con los datos de entrada
        """
        pass



    @abstractmethod
    async def complete_fuzzy_evaluation(
        self,
        system: FuzzySystem,
        variables: List[FuzzyVariable],
        terms: List[FuzzyTerm],
        rules: List[FuzzyRule],
        sensor_readings: Dict[str, float]
    ) -> Dict[str, Any]:
        """Realiza una evaluación fuzzy completa del sistema.
        
        Este método orquesta todo el flujo:
        1. Fuzzificación de entradas
        2. Evaluación de reglas
        3. Defuzzificación de salidas
        
        Args:
            system: Sistema fuzzy a evaluar
            variables: Variables del sistema (entrada y salida)
            terms: Términos fuzzy de todas las variables
            rules: Reglas del sistema
            sensor_readings: Lecturas de sensores {sensor_id: value}
            
        Returns:
            Diccionario con resultados completos de la evaluación
            
        Raises:
            ValidationError: Si hay problemas en cualquier paso
        """
        pass