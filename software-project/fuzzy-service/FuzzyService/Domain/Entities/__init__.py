"""Entidades del dominio del servicio difuso."""

from .fuzzy_system import FuzzySystem
from .fuzzy_variable import FuzzyVariable
from .fuzzy_term import FuzzyTerm
from .fuzzy_rule import FuzzyRule
from .fuzzy_evaluation import FuzzyEvaluation, InputValue, OutputValue, RuleActivation
from .rule_consequent import RuleConsequent

__all__ = [
    # Entidades principales
    "FuzzySystem",
    "FuzzyVariable",
    "FuzzyTerm",
    "FuzzyRule",
    "FuzzyEvaluation",
    "RuleConsequent",

    # Clases de datos auxiliares
    "InputValue",
    "OutputValue",
    "RuleActivation",
]
