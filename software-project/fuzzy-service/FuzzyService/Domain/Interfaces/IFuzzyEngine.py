"""Domain service interface for the fuzzy inference engine.

This engine encapsulates the fuzzy logic evaluation independent from infrastructure.
"""

from abc import ABC, abstractmethod
from typing import List, Optional
from datetime import datetime

from ..Entities.fuzzy_evaluation import FuzzyEvaluation, InputValue, OutputValue
from ..ValueObjects import FuzzySystemId
from ..Enums import DefuzzificationMethod


class IFuzzyEngine(ABC):
    """Domain service responsible for evaluating a fuzzy system with given inputs."""

    @abstractmethod
    async def evaluate(self, system_id: FuzzySystemId, inputs: List[InputValue], *,
                       at: Optional[datetime] = None,
                       defuzz_method: Optional[DefuzzificationMethod] = None) -> FuzzyEvaluation:
        """Evaluates the fuzzy system for the provided inputs and returns an evaluation record.
        
        The engine should:
        - Load the system, variables, terms, and rules from repositories (through application layer orchestration)
        - Fuzzify inputs, apply rule base, aggregate outputs, and defuzzify according to the configured method
        - Produce OutputValues for actuators and include rule activations with firing strengths
        
        Args:
            system_id: Target fuzzy system identifier
            inputs: List of crisp inputs with sensor identifiers
            at: Optional evaluation time (defaults to now UTC)
            defuzz_method: Optional override for the defuzzification method
        
        Returns:
            A FuzzyEvaluation domain entity capturing the evaluation outcome
        
        Raises:
            EntityNotFoundError: If the system does not exist
            ValidationError: If inputs are invalid or incomplete for the system
        """
        pass

    @abstractmethod
    async def supported_defuzz_methods(self) -> List[DefuzzificationMethod]:
        """Returns the list of supported defuzzification methods by the engine implementation."""
        pass

    @abstractmethod
    async def warm_up(self) -> None:
        """Prepares internal caches or precomputations if the engine supports it."""
        pass
