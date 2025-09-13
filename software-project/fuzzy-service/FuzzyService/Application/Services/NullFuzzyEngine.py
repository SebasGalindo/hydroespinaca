"""Null (no-op) implementation of IFuzzyEngine for safe bootstrapping.

This engine returns an empty FuzzyEvaluation without touching infrastructure.
"""
from __future__ import annotations

from typing import List, Optional, Dict, Any
from datetime import datetime, timezone

from FuzzyService.Domain.Interfaces.IFuzzyEngine import IFuzzyEngine
from FuzzyService.Domain.Entities.fuzzy_evaluation import FuzzyEvaluation, InputValue
from FuzzyService.Domain.ValueObjects import FuzzySystemId
from FuzzyService.Domain.Enums import DefuzzificationMethod
from FuzzyService.Domain.Entities.fuzzy_variable import FuzzyVariable
from FuzzyService.Domain.Entities.fuzzy_term import FuzzyTerm
from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule
from FuzzyService.Domain.Entities.fuzzy_system import FuzzySystem

# Import types for method signatures
try:
    from FuzzyService.Infrastructure.ExternalServices.FuzzyEngine.FuzzificationTypes import FuzzificationResult
    from FuzzyService.Infrastructure.ExternalServices.FuzzyEngine.RuleEvaluationEngine import BatchRuleEvaluationResult
except ImportError:
    # Fallback types if infrastructure not available
    FuzzificationResult = Any
    BatchRuleEvaluationResult = Any


class NullFuzzyEngine(IFuzzyEngine):
    """A minimal IFuzzyEngine that produces an empty evaluation and supports no methods."""

    async def evaluate(
        self,
        system_id: FuzzySystemId,
        inputs: List[InputValue],
        *,
        at: Optional[datetime] = None,
        defuzz_method: Optional[DefuzzificationMethod] = None,
    ) -> FuzzyEvaluation:
        # Create a neutral evaluation record with provided inputs and no rule activations
        ts = at or datetime.now(timezone.utc)
        # Ensure inputs are InputValue instances (the interface already enforces it)
        normalized_inputs = [
            iv if isinstance(iv, InputValue) else InputValue(sensor_id=str(iv[0]), value=float(iv[1]))
            for iv in inputs
        ]
        return FuzzyEvaluation(
            system_id=system_id,
            timestamp=ts,
            inputs=normalized_inputs,
            activated_rules=[],
        )

    async def supported_defuzz_methods(self) -> List[DefuzzificationMethod]:
        # No defuzzification supported by the null engine
        return []

    async def warm_up(self) -> None:
        # Nothing to warm up in the null engine
        return None

    async def fuzzify_sensor_readings(
        self, 
        variables: List[FuzzyVariable], 
        terms: List[FuzzyTerm], 
        sensor_readings: Dict[str, float]
    ) -> List[FuzzificationResult]:
        """No-op fuzzification that returns empty results."""
        return []



    async def complete_fuzzy_evaluation(
        self,
        system: FuzzySystem,
        variables: List[FuzzyVariable],
        terms: List[FuzzyTerm],
        rules: List[FuzzyRule],
        sensor_readings: Dict[str, float]
    ) -> Dict[str, Any]:
        """No-op complete evaluation that returns minimal results."""
        return {
            "fuzzification_results": [],
            "rule_evaluation_results": {"rule_activations": [], "firing_strengths": {}},
            "defuzzification_results": {},
            "output_values": {}
        }