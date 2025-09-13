"""Application Mappers Package

Contiene mapeadores entre entidades de dominio y estructuras utilizadas por servicios.

Nota:
- Evitar importar mappers que dependan de Infrastructure.* para no forzar dicha dependencia.
- Los mappers deprecados que dependían del motor de infraestructura han sido mantenidos en sus archivos
  para referencia histórica pero no se exponen en __all__.
"""

from .FuzzyEvaluationMapper import FuzzyEvaluationMapper
from .FuzzyVariableMapper import FuzzyVariableMapper
from .FuzzySystemMapper import FuzzySystemMapper
from .FuzzyRuleMapper import FuzzyRuleMapper

__all__ = [
    "FuzzyEvaluationMapper",
    "FuzzyVariableMapper",
    "FuzzySystemMapper",
    "FuzzyRuleMapper",
]