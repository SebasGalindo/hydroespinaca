"""Repository interface for fuzzy evaluations."""

from abc import ABC, abstractmethod
from typing import List, Optional, Dict, Any
from datetime import datetime

from ..Entities.fuzzy_evaluation import FuzzyEvaluation
from ..ValueObjects.DomainId import FuzzyEvaluationId, FuzzySystemId, FuzzyRuleId


class IFuzzyEvaluationRepository(ABC):
    """Repository interface for FuzzyEvaluation entities (evaluation logs)."""

    # Basic CRUD methods
    @abstractmethod
    async def create(self, evaluation: FuzzyEvaluation) -> FuzzyEvaluation:
        """Creates a new evaluation record.
        
        Args:
            evaluation: The evaluation to create
            
        Returns:
            The created evaluation with assigned ID
            
        Raises:
            ValidationError: If the evaluation data is invalid
        """
        pass

    @abstractmethod
    async def get_by_id(self, evaluation_id: FuzzyEvaluationId) -> Optional[FuzzyEvaluation]:
        """Gets an evaluation by its ID.
        
        Args:
            evaluation_id: ID of the evaluation
            
        Returns:
            The evaluation if exists, None otherwise
        """
        pass

    @abstractmethod
    async def get_all(self, skip: int = 0, limit: int = 100) -> List[FuzzyEvaluation]:
        """Gets all evaluations with pagination.
        
        Args:
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of evaluations
        """
        pass

    # Update and delete may be restricted for audit logs, but we include them for completeness
    @abstractmethod
    async def update(self, evaluation: FuzzyEvaluation) -> FuzzyEvaluation:
        """Updates an existing evaluation record.
        
        Args:
            evaluation: The evaluation with updated data
            
        Returns:
            The updated evaluation
        """
        pass

    @abstractmethod
    async def delete(self, evaluation_id: FuzzyEvaluationId) -> bool:
        """Deletes an evaluation record.
        
        Args:
            evaluation_id: ID of the evaluation to delete
            
        Returns:
            True if deleted successfully, False if it didn't exist
        """
        pass

    # System and rule specific queries
    @abstractmethod
    async def get_by_system_id(self, system_id: FuzzySystemId, skip: int = 0, limit: int = 100) -> List[FuzzyEvaluation]:
        """Gets evaluations by system ID.
        
        Args:
            system_id: ID of the fuzzy system
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of evaluations belonging to the system
        """
        pass

    @abstractmethod
    async def get_by_rule_id(self, rule_id: FuzzyRuleId, skip: int = 0, limit: int = 100) -> List[FuzzyEvaluation]:
        """Gets evaluations that activated a specific rule.
        
        Args:
            rule_id: ID of the fuzzy rule
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of evaluations that include the rule activation
        """
        pass

    # Date-based queries
    @abstractmethod
    async def get_by_date_range(self, start_date: datetime, end_date: datetime, 
                               skip: int = 0, limit: int = 100) -> List[FuzzyEvaluation]:
        """Gets evaluations performed in a date range.
        
        Args:
            start_date: Start date of the range
            end_date: End date of the range
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of evaluations performed in the specified range
        """
        pass

    # Filtering and counting
    @abstractmethod
    async def filter_evaluations(self, filters: Dict[str, Any], skip: int = 0, limit: int = 100) -> List[FuzzyEvaluation]:
        """Filters evaluations using multiple criteria.
        
        Args:
            filters: Dictionary with filtering criteria
                    Examples: {'system_id': '123', 'rule_id': 'abc', 'min_firing_strength': 0.5}
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of evaluations matching the criteria
        """
        pass

    @abstractmethod
    async def count_filtered_evaluations(self, filters: Dict[str, Any]) -> int:
        """Counts evaluations matching the same filters as `filter_evaluations`.

        Args:
            filters: Same filter dictionary accepted by `filter_evaluations`.

        Returns:
            Number of evaluations matching the criteria.
        """
        pass

    @abstractmethod
    async def compute_summary_stats(self, filters: Dict[str, Any]) -> Dict[str, Any]:
        """Aggregates daily and per-system counts for the given filter.

        Returns:
            Dict with keys: `total` (int), `daily_stats` (dict[str, int]),
            `systems_stats` (dict[str, int]).
        """
        pass

    @abstractmethod
    async def count_by_system(self, system_id: FuzzySystemId) -> int:
        """Counts evaluations by system.
        
        Args:
            system_id: ID of the fuzzy system
            
        Returns:
            Number of evaluations in the system
        """
        pass

    @abstractmethod
    async def count_total(self) -> int:
        """Counts total number of evaluations.
        
        Returns:
            Total number of evaluations
        """
        pass
