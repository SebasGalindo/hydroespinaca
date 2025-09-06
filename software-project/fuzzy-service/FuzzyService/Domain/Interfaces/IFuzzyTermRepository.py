"""Repository interface for fuzzy terms (linguistic labels)."""

from abc import ABC, abstractmethod
from typing import List, Optional, Dict, Any
from datetime import datetime

from ..Entities.fuzzy_term import FuzzyTerm
from ..ValueObjects.DomainId import FuzzyTermId, FuzzyVariableId


class IFuzzyTermRepository(ABC):
    """Repository interface for FuzzyTerm entities."""

    # Basic CRUD methods
    @abstractmethod
    async def create(self, fuzzy_term: FuzzyTerm) -> FuzzyTerm:
        """Creates a new fuzzy term.
        
        Args:
            fuzzy_term: The fuzzy term to create
            
        Returns:
            The created fuzzy term with assigned ID
            
        Raises:
            DuplicateEntityError: If a term with the same label already exists for the variable
            ValidationError: If the term data is invalid
        """
        pass

    @abstractmethod
    async def get_by_id(self, term_id: FuzzyTermId) -> Optional[FuzzyTerm]:
        """Gets a fuzzy term by its ID.
        
        Args:
            term_id: ID of the fuzzy term
            
        Returns:
            The fuzzy term if exists, None otherwise
        """
        pass

    @abstractmethod
    async def get_all(self, skip: int = 0, limit: int = 100) -> List[FuzzyTerm]:
        """Gets all fuzzy terms with pagination.
        
        Args:
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of fuzzy terms
        """
        pass

    @abstractmethod
    async def update(self, fuzzy_term: FuzzyTerm) -> FuzzyTerm:
        """Updates an existing fuzzy term.
        
        Args:
            fuzzy_term: The fuzzy term with updated data
            
        Returns:
            The updated fuzzy term
            
        Raises:
            EntityNotFoundError: If the term does not exist
            ValidationError: If the updated data is invalid
        """
        pass

    @abstractmethod
    async def delete(self, term_id: FuzzyTermId) -> bool:
        """Deletes a fuzzy term.
        
        Args:
            term_id: ID of the fuzzy term to delete
            
        Returns:
            True if deleted successfully, False if it didn't exist
        """
        pass

    @abstractmethod
    async def exists(self, term_id: FuzzyTermId) -> bool:
        """Checks if a fuzzy term exists.
        
        Args:
            term_id: ID of the fuzzy term
            
        Returns:
            True if exists, False otherwise
        """
        pass

    # Variable-specific queries
    @abstractmethod
    async def get_by_variable_id(self, variable_id: FuzzyVariableId, skip: int = 0, limit: int = 100) -> List[FuzzyTerm]:
        """Gets fuzzy terms by variable ID.
        
        Args:
            variable_id: ID of the fuzzy variable
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of fuzzy terms belonging to the variable
        """
        pass

    @abstractmethod
    async def get_by_label(self, variable_id: FuzzyVariableId, label: str) -> Optional[FuzzyTerm]:
        """Gets a fuzzy term by its label within a variable.
        
        Args:
            variable_id: ID of the fuzzy variable
            label: Label of the term
            
        Returns:
            The fuzzy term if exists, None otherwise
        """
        pass

    # Search and filtering
    @abstractmethod
    async def search_by_label(self, variable_id: FuzzyVariableId, label_pattern: str, skip: int = 0, limit: int = 100) -> List[FuzzyTerm]:
        """Searches terms by label pattern for a variable.
        
        Args:
            variable_id: ID of the fuzzy variable
            label_pattern: Search pattern for the label
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of terms matching the label pattern
        """
        pass

    @abstractmethod
    async def filter_terms(self, filters: Dict[str, Any], skip: int = 0, limit: int = 100) -> List[FuzzyTerm]:
        """Filters terms using multiple criteria.
        
        Args:
            filters: Dictionary with filtering criteria
                    Examples: {'variable_id': '123', 'label_contains': 'high'}
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of terms matching the criteria
        """
        pass

    # Counting methods
    @abstractmethod
    async def count_by_variable(self, variable_id: FuzzyVariableId) -> int:
        """Counts terms by variable.
        
        Args:
            variable_id: ID of the fuzzy variable
            
        Returns:
            Number of terms in the variable
        """
        pass

    @abstractmethod
    async def count_total(self) -> int:
        """Counts total number of terms.
        
        Returns:
            Total number of fuzzy terms
        """
        pass
