"""Repository interface for fuzzy systems."""

from abc import ABC, abstractmethod
from typing import List, Optional, Dict, Any
from datetime import datetime

from ..Entities.fuzzy_system import FuzzySystem
from ..ValueObjects.DomainId import FuzzySystemId
from ..Enums.EntityStatus import FuzzySystemStatus


class IFuzzySystemRepository(ABC):
    """Repository interface for FuzzySystem entities."""
    
    # Basic CRUD methods
    @abstractmethod
    async def create(self, fuzzy_system: FuzzySystem) -> FuzzySystem:
        """Creates a new fuzzy system.
        
        Args:
            fuzzy_system: The fuzzy system to create
            
        Returns:
            The created fuzzy system with assigned ID
            
        Raises:
            DuplicateEntityError: If a system with the same name already exists
            ValidationError: If the system data is invalid
        """
        pass
    
    @abstractmethod
    async def get_by_id(self, system_id: FuzzySystemId) -> Optional[FuzzySystem]:
        """Gets a fuzzy system by its ID.
        
        Args:
            system_id: ID of the fuzzy system
            
        Returns:
            The fuzzy system if exists, None otherwise
        """
        pass
    
    @abstractmethod
    async def get_by_name(self, name: str) -> Optional[FuzzySystem]:
        """Gets a fuzzy system by its name.
        
        Args:
            name: Name of the fuzzy system
            
        Returns:
            The fuzzy system if exists, None otherwise
        """
        pass
    
    @abstractmethod
    async def get_all(self, skip: int = 0, limit: int = 100) -> List[FuzzySystem]:
        """Gets all fuzzy systems with pagination.
        
        Args:
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of fuzzy systems
        """
        pass
    
    @abstractmethod
    async def update(self, fuzzy_system: FuzzySystem) -> FuzzySystem:
        """Updates an existing fuzzy system.
        
        Args:
            fuzzy_system: The fuzzy system with updated data
            
        Returns:
            The updated fuzzy system
            
        Raises:
            EntityNotFoundError: If the system does not exist
            ValidationError: If the updated data is invalid
        """
        pass
    
    @abstractmethod
    async def update_status(self, system_id: FuzzySystemId, status: FuzzySystemStatus) -> FuzzySystem:
        """Updates only the status of a fuzzy system.
        
        Args:
            system_id: ID of the fuzzy system to update
            status: New status for the system
            
        Returns:
            The updated fuzzy system
            
        Raises:
            EntityNotFoundError: If the system does not exist
            ValidationError: If the status is invalid
        """
        pass
    
    @abstractmethod
    async def delete(self, system_id: FuzzySystemId) -> bool:
        """Deletes a fuzzy system.
        
        Args:
            system_id: ID of the fuzzy system to delete
            
        Returns:
            True if deleted successfully, False if it didn't exist
        """
        pass
    
    @abstractmethod
    async def exists(self, system_id: FuzzySystemId) -> bool:
        """Checks if a fuzzy system exists.
        
        Args:
            system_id: ID of the fuzzy system
            
        Returns:
            True if exists, False otherwise
        """
        pass
    
    # Specific query methods
    @abstractmethod
    async def get_by_status(self, status: FuzzySystemStatus, skip: int = 0, limit: int = 100) -> List[FuzzySystem]:
        """Gets fuzzy systems by status.
        
        Args:
            status: Status to filter systems by
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of fuzzy systems with the specified status
        """
        pass
    
    @abstractmethod
    async def get_active_systems(self, skip: int = 0, limit: int = 100) -> List[FuzzySystem]:
        """Gets active fuzzy systems.
        
        Args:
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of active fuzzy systems
        """
        pass
    
    @abstractmethod
    async def search_by_name(self, name_pattern: str, skip: int = 0, limit: int = 100) -> List[FuzzySystem]:
        """Searches fuzzy systems by name pattern.
        
        Args:
            name_pattern: Search pattern for the system name
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of fuzzy systems matching the pattern
        """
        pass
    
    @abstractmethod
    async def get_by_date_range(self, start_date: datetime, end_date: datetime, 
                               skip: int = 0, limit: int = 100) -> List[FuzzySystem]:
        """Gets fuzzy systems created in a date range.
        
        Args:
            start_date: Start date of the range
            end_date: End date of the range
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of fuzzy systems created in the specified range
        """
        pass
    
    @abstractmethod
    async def count_by_status(self, status: FuzzySystemStatus) -> int:
        """Counts fuzzy systems by status.
        
        Args:
            status: Status of the systems to count
            
        Returns:
            Number of systems with the specified status
        """
        pass
    
    @abstractmethod
    async def count_total(self) -> int:
        """Counts total number of fuzzy systems.
        
        Returns:
            Total number of fuzzy systems
        """
        pass
    
    # Advanced filtering methods
    @abstractmethod
    async def filter_systems(self, filters: Dict[str, Any], skip: int = 0, limit: int = 100) -> List[FuzzySystem]:
        """Filters fuzzy systems using multiple criteria.
        
        Args:
            filters: Dictionary with filtering criteria
                    Examples: {'status': 'active', 'name_contains': 'temp', 
                             'created_after': datetime, 'has_variables': True}
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of fuzzy systems that match the criteria
        """
        pass
    
    @abstractmethod
    async def get_systems_with_variable_count(self, min_variables: int = 0, 
                                            max_variables: Optional[int] = None) -> List[FuzzySystem]:
        """Gets fuzzy systems by number of variables.
        
        Args:
            min_variables: Minimum number of variables
            max_variables: Maximum number of variables (optional)
            
        Returns:
            List of fuzzy systems with the specified variable count range
        """
        pass
    
    @abstractmethod
    async def get_systems_with_rule_count(self, min_rules: int = 0, 
                                         max_rules: Optional[int] = None) -> List[FuzzySystem]:
        """Gets fuzzy systems by number of rules.
        
        Args:
            min_rules: Minimum number of rules
            max_rules: Maximum number of rules (optional)
            
        Returns:
            List of fuzzy systems with the specified rule count range
        """
        pass

    # Variable-specific queries
    @abstractmethod
    async def get_systems_with_input_variable(self, variable_id: "FuzzyVariableId") -> List[FuzzySystem]:
        """Gets fuzzy systems that contain a specific input variable.
        
        Args:
            variable_id: ID of the fuzzy variable
            
        Returns:
            List of fuzzy systems containing the variable as input
        """
        pass

    @abstractmethod
    async def get_systems_with_output_variable(self, variable_id: "FuzzyVariableId") -> List[FuzzySystem]:
        """Gets fuzzy systems that contain a specific output variable.
        
        Args:
            variable_id: ID of the fuzzy variable
            
        Returns:
            List of fuzzy systems containing the variable as output
        """
        pass
