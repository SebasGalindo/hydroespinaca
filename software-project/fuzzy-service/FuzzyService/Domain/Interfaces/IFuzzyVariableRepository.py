"""Repository interface for fuzzy variables."""

from abc import ABC, abstractmethod
from typing import List, Optional, Dict, Any
from datetime import datetime

from ..Entities.fuzzy_variable import FuzzyVariable
from ..ValueObjects.DomainId import FuzzyVariableId, FuzzySystemId, FuzzyTermId
from ..Enums.EntityStatus import FuzzyVariableType


class IFuzzyVariableRepository(ABC):
    """Repository interface for FuzzyVariable entities."""
    
    # Basic CRUD methods
    @abstractmethod
    async def create(self, fuzzy_variable: FuzzyVariable) -> FuzzyVariable:
        """Creates a new fuzzy variable.
        
        Args:
            fuzzy_variable: The fuzzy variable to create
            
        Returns:
            The created fuzzy variable with assigned ID
            
        Raises:
            DuplicateEntityError: If a variable with the same name already exists for the system
            ValidationError: If the variable data is invalid
        """
        pass
    
    @abstractmethod
    async def get_by_id(self, variable_id: FuzzyVariableId) -> Optional[FuzzyVariable]:
        """Gets a fuzzy variable by its ID.
        
        Args:
            variable_id: ID of the fuzzy variable
            
        Returns:
            The fuzzy variable if exists, None otherwise
        """
        pass
    
    @abstractmethod
    async def get_by_name(self, name: str) -> Optional[FuzzyVariable]:
        """Gets a fuzzy variable by its name.
        
        Args:
            name: Name of the fuzzy variable
            
        Returns:
            The fuzzy variable if exists, None otherwise
        """
        pass
    
    @abstractmethod
    async def get_all(self, skip: int = 0, limit: int = 100) -> List[FuzzyVariable]:
        """Gets all fuzzy variables with pagination.
        
        Args:
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of fuzzy variables
        """
        pass
    
    @abstractmethod
    async def update(self, fuzzy_variable: FuzzyVariable) -> FuzzyVariable:
        """Updates an existing fuzzy variable.
        
        Args:
            fuzzy_variable: The fuzzy variable with updated data
            
        Returns:
            The updated fuzzy variable
            
        Raises:
            EntityNotFoundError: If the variable does not exist
            ValidationError: If the updated data is invalid
        """
        pass
    
    @abstractmethod
    async def delete(self, variable_id: FuzzyVariableId) -> bool:
        """Deletes a fuzzy variable.
        
        Args:
            variable_id: ID of the fuzzy variable to delete
            
        Returns:
            True if deleted successfully, False if it didn't exist
        """
        pass
    
    @abstractmethod
    async def exists(self, variable_id: FuzzyVariableId) -> bool:
        """Checks if a fuzzy variable exists.
        
        Args:
            variable_id: ID of the fuzzy variable
            
        Returns:
            True if exists, False otherwise
        """
        pass
    
    # System-specific queries
    @abstractmethod
    async def get_by_system_id(self, system_id: FuzzySystemId, skip: int = 0, limit: int = 100) -> List[FuzzyVariable]:
        """Gets fuzzy variables by system ID.
        
        Args:
            system_id: ID of the fuzzy system
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of fuzzy variables belonging to the system
        """
        pass
    
    @abstractmethod
    async def get_by_type(self, variable_type: FuzzyVariableType, skip: int = 0, limit: int = 100) -> List[FuzzyVariable]:
        """Gets fuzzy variables by type.
        
        Args:
            variable_type: Type of variables to retrieve (input/output)
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of fuzzy variables of the specified type
        """
        pass
    
    @abstractmethod
    async def get_input_variables_by_system(self, system_id: FuzzySystemId) -> List[FuzzyVariable]:
        """Gets input variables for a specific system.
        
        Args:
            system_id: ID of the fuzzy system
            
        Returns:
            List of input variables for the system
        """
        pass
    
    @abstractmethod
    async def get_output_variables_by_system(self, system_id: FuzzySystemId) -> List[FuzzyVariable]:
        """Gets output variables for a specific system.
        
        Args:
            system_id: ID of the fuzzy system
            
        Returns:
            List of output variables for the system
        """
        pass
    
    # Term-related queries
    @abstractmethod
    async def get_variables_with_term(self, term_id: FuzzyTermId) -> List[FuzzyVariable]:
        """Gets variables that contain a specific term.
        
        Args:
            term_id: ID of the fuzzy term
            
        Returns:
            List of variables containing the term
        """
        pass
    
    @abstractmethod
    async def get_variables_with_term_count(self, min_terms: int = 0, 
                                          max_terms: Optional[int] = None) -> List[FuzzyVariable]:
        """Gets variables by term count.
        
        Args:
            min_terms: Minimum number of terms
            max_terms: Maximum number of terms (optional)
            
        Returns:
            List of variables with the specified term count range
        """
        pass
    
    # Search and filtering
    @abstractmethod
    async def search_by_name(self, name_pattern: str, skip: int = 0, limit: int = 100) -> List[FuzzyVariable]:
        """Searches variables by name pattern.
        
        Args:
            name_pattern: Search pattern for the name
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of variables matching the name pattern
        """
        pass
    
    @abstractmethod
    async def filter_variables(self, filters: Dict[str, Any], skip: int = 0, limit: int = 100) -> List[FuzzyVariable]:
        """Filters variables using multiple criteria.
        
        Args:
            filters: Dictionary with filtering criteria
                    Examples: {'type': 'input', 'system_id': '123', 
                             'has_device': True, 'min_terms': 3}
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of variables matching the criteria
        """
        pass
    
    # Counting methods
    @abstractmethod
    async def count_by_system(self, system_id: FuzzySystemId) -> int:
        """Counts variables by system.
        
        Args:
            system_id: ID of the fuzzy system
            
        Returns:
            Number of variables in the system
        """
        pass
    
    @abstractmethod
    async def count_by_type(self, variable_type: FuzzyVariableType) -> int:
        """Counts variables by type.
        
        Args:
            variable_type: Type of variables to count
            
        Returns:
            Number of variables of the specified type
        """
        pass
    
    @abstractmethod
    async def count_total(self) -> int:
        """Counts total number of variables.
        
        Returns:
            Total number of fuzzy variables
        """
        pass
    
    # Date-based queries
    @abstractmethod
    async def get_by_date_range(self, start_date: datetime, end_date: datetime, 
                               skip: int = 0, limit: int = 100) -> List[FuzzyVariable]:
        """Gets variables created in a date range.
        
        Args:
            start_date: Start date of the range
            end_date: End date of the range
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of variables created in the specified range
        """
        pass
    
    # ---- Bulk operations (for clone / import) ----
    @abstractmethod
    async def create_many(self, variables: List[FuzzyVariable]) -> List[FuzzyVariable]:
        """Bulk-inserts variables without per-entity validation. Returns entities with assigned IDs."""
        pass

    @abstractmethod
    async def update_many_terms(self, updates: List[tuple]) -> None:
        """Bulk-updates the terms array for multiple variables.
        
        Args:
            updates: List of (variable_id, term_id_list) tuples.
        """
        pass

    # Validation methods removed - device_id is deprecated, use reference_id validation in handlers
