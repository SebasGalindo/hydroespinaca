"""Mappers para conversiones entre entidades de dominio e infraestructura.

Este módulo contiene mappers específicos para cada entidad, siguiendo el patrón
de clases con métodos estáticos to_infra y to_domain.
"""

from .FuzzyRuleMapper import FuzzyRuleMapper
from .FuzzyVariableMapper import FuzzyVariableMapper
from .FuzzyTermMapper import FuzzyTermMapper
from .FuzzySystemMapper import FuzzySystemMapper
from .FuzzyEvaluationMapper import FuzzyEvaluationMapper
from .FuzzyRoutineMapper import FuzzyRoutineMapper

# Mantener compatibilidad con mappers antiguos (deprecados)
from .DomainToInfrastructureMapper import DomainToInfrastructureMapper
from .InfrastructureToDomainMapper import InfrastructureToDomainMapper

__all__ = [
    # Nuevos mappers específicos
    "FuzzyRuleMapper",
    "FuzzyVariableMapper", 
    "FuzzyTermMapper",
    "FuzzySystemMapper",
    "FuzzyEvaluationMapper",
    "FuzzyRoutineMapper",
    
    # Mappers antiguos (deprecados)
    "DomainToInfrastructureMapper",
    "InfrastructureToDomainMapper"
]