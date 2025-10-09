"""Módulo de motor fuzzy basado en scikit-fuzzy.

Este módulo contiene la implementación del motor fuzzy que maneja
la fuzzificación de lecturas de sensores usando scikit-fuzzy.
"""

from .ScikitFuzzyEngine import ScikitFuzzyEngine
from .FuzzyResultTypes import FuzzificationResult, RuleActivationResult, BatchRuleEvaluationResult

__all__ = [
    "ScikitFuzzyEngine",
    "FuzzificationResult",
    "RuleActivationResult",
    "BatchRuleEvaluationResult"
]