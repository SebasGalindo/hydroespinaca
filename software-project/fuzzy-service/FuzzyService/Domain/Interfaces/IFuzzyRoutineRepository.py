"""Repository interface for fuzzy routines (actuation routines)."""

from abc import ABC, abstractmethod
from typing import List, Optional, Dict, Any
from datetime import datetime

from ..Entities.fuzzy_routine import FuzzyRoutine, RoutineStep
from ..ValueObjects.DomainId import FuzzyRoutineId


class IFuzzyRoutineRepository(ABC):
    """Repository interface for FuzzyRoutine entities."""

    # Basic CRUD methods
    @abstractmethod
    async def create(self, routine: FuzzyRoutine) -> FuzzyRoutine:
        """Creates a new routine.
        
        Args:
            routine: The fuzzy routine to create
            
        Returns:
            The created routine with assigned ID
            
        Raises:
            DuplicateEntityError: If a routine with the same name already exists
            ValidationError: If the routine data is invalid
        """
        pass

    @abstractmethod
    async def get_by_id(self, routine_id: FuzzyRoutineId) -> Optional[FuzzyRoutine]:
        """Gets a routine by its ID.
        
        Args:
            routine_id: ID of the routine
            
        Returns:
            The routine if exists, None otherwise
        """
        pass

    @abstractmethod
    async def get_all(self, skip: int = 0, limit: int = 100) -> List[FuzzyRoutine]:
        """Gets all routines with pagination.
        
        Args:
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of routines
        """
        pass

    @abstractmethod
    async def update(self, routine: FuzzyRoutine) -> FuzzyRoutine:
        """Updates an existing routine.
        
        Args:
            routine: The routine with updated data
            
        Returns:
            The updated routine
            
        Raises:
            EntityNotFoundError: If the routine does not exist
            ValidationError: If the updated data is invalid
        """
        pass

    @abstractmethod
    async def delete(self, routine_id: FuzzyRoutineId) -> bool:
        """Deletes a routine.
        
        Args:
            routine_id: ID of the routine to delete
            
        Returns:
            True if deleted successfully, False if it didn't exist
        """
        pass

    @abstractmethod
    async def exists(self, routine_id: FuzzyRoutineId) -> bool:
        """Checks if a routine exists.
        
        Args:
            routine_id: ID of the routine
            
        Returns:
            True if exists, False otherwise
        """
        pass

    # Search and filtering
    @abstractmethod
    async def search_by_name(self, name_pattern: str, skip: int = 0, limit: int = 100) -> List[FuzzyRoutine]:
        """Searches routines by name pattern.
        
        Args:
            name_pattern: Search pattern for the routine name
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of routines matching the name pattern
        """
        pass

    @abstractmethod
    async def filter_routines(self, filters: Dict[str, Any], skip: int = 0, limit: int = 100) -> List[FuzzyRoutine]:
        """Filters routines using multiple criteria.
        
        Args:
            filters: Dictionary with filtering criteria
                    Examples: {'name_contains': 'heat', 'min_steps': 1}
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of routines matching the criteria
        """
        pass

    # Steps related queries
    @abstractmethod
    async def get_routines_with_step_count(self, min_steps: int = 0, max_steps: Optional[int] = None) -> List[FuzzyRoutine]:
        """Gets routines by number of steps.
        
        Args:
            min_steps: Minimum number of steps
            max_steps: Maximum number of steps (optional)
            
        Returns:
            List of routines with the specified step count range
        """
        pass

    # Counting methods
    @abstractmethod
    async def count_total(self) -> int:
        """Counts total number of routines.
        
        Returns:
            Total number of routines
        """
        pass

    # Date-based queries
    @abstractmethod
    async def get_by_date_range(self, start_date: datetime, end_date: datetime, 
                               skip: int = 0, limit: int = 100) -> List[FuzzyRoutine]:
        """Gets routines created in a date range.
        
        Args:
            start_date: Start date of the range
            end_date: End date of the range
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of routines created in the specified range
        """
        pass
