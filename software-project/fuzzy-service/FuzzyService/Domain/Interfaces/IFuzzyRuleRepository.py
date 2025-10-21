"""Repository interface for fuzzy rules."""

from abc import ABC, abstractmethod
from typing import List, Optional, Dict, Any
from datetime import datetime

from ..Entities.fuzzy_rule import FuzzyRule
from ..ValueObjects.DomainId import FuzzyRuleId, FuzzySystemId, FuzzyVariableId, FuzzyRoutineId
from ..Enums import RuleConnector


class IFuzzyRuleRepository(ABC):
    """Repository interface for FuzzyRule entities."""

    # Basic CRUD methods
    @abstractmethod
    async def create(self, fuzzy_rule: FuzzyRule) -> FuzzyRule:
        """Creates a new fuzzy rule.
        
        Args:
            fuzzy_rule: The fuzzy rule to create
            
        Returns:
            The created fuzzy rule with assigned ID
            
        Raises:
            DuplicateEntityError: If a rule with the same name already exists for the system
            ValidationError: If the rule data is invalid
        """
        pass

    @abstractmethod
    async def get_by_id(self, rule_id: FuzzyRuleId) -> Optional[FuzzyRule]:
        """Gets a fuzzy rule by its ID.
        
        Args:
            rule_id: ID of the fuzzy rule
            
        Returns:
            The fuzzy rule if exists, None otherwise
        """
        pass

    @abstractmethod
    async def get_all(self, skip: int = 0, limit: int = 100) -> List[FuzzyRule]:
        """Gets all fuzzy rules with pagination.
        
        Args:
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of fuzzy rules
        """
        pass

    @abstractmethod
    async def update(self, fuzzy_rule: FuzzyRule) -> FuzzyRule:
        """Updates an existing fuzzy rule.
        
        Args:
            fuzzy_rule: The fuzzy rule with updated data
            
        Returns:
            The updated fuzzy rule
            
        Raises:
            EntityNotFoundError: If the rule does not exist
            ValidationError: If the updated data is invalid
        """
        pass

    @abstractmethod
    async def delete(self, rule_id: FuzzyRuleId) -> bool:
        """Deletes a fuzzy rule.
        
        Args:
            rule_id: ID of the fuzzy rule to delete
            
        Returns:
            True if deleted successfully, False if it didn't exist
        """
        pass

    @abstractmethod
    async def exists(self, rule_id: FuzzyRuleId) -> bool:
        """Checks if a fuzzy rule exists.
        
        Args:
            rule_id: ID of the fuzzy rule
            
        Returns:
            True if exists, False otherwise
        """
        pass

    # System-specific queries
    @abstractmethod
    async def get_by_system_id(self, system_id: FuzzySystemId, skip: int = 0, limit: int = 100) -> List[FuzzyRule]:
        """Gets fuzzy rules by system ID.
        
        Args:
            system_id: ID of the fuzzy system
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of fuzzy rules belonging to the system
        """
        pass

    @abstractmethod
    async def get_by_name(self, system_id: FuzzySystemId, name: str) -> Optional[FuzzyRule]:
        """Gets a fuzzy rule by its name within a system.
        
        Args:
            system_id: ID of the fuzzy system
            name: Name of the rule
            
        Returns:
            The fuzzy rule if exists, None otherwise
        """
        pass

    # Conditions/connectors queries
    @abstractmethod
    async def get_rules_using_variable(self, variable_id: FuzzyVariableId, skip: int = 0, limit: int = 100) -> List[FuzzyRule]:
        """Gets rules that reference a given variable in their conditions.
        
        Args:
            variable_id: ID of the fuzzy variable
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of fuzzy rules using the variable
        """
        pass

    @abstractmethod
    async def get_rules_with_connector(self, connector: RuleConnector, skip: int = 0, limit: int = 100) -> List[FuzzyRule]:
        """Gets rules that use the specified connector (AND/OR) in their conditions.
        
        Args:
            connector: Rule connector to filter by
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of fuzzy rules that contain the connector
        """
        pass

    # Consequent queries
    @abstractmethod
    async def get_rules_by_consequent(self, routine_id: FuzzyRoutineId, skip: int = 0, limit: int = 100) -> List[FuzzyRule]:
        """Gets rules by consequent routine ID.
        
        Args:
            routine_id: ID of the consequent routine
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of fuzzy rules pointing to the routine
        """
        pass

    # Search and filtering
    @abstractmethod
    async def search_by_name(self, system_id: FuzzySystemId, name_pattern: str, skip: int = 0, limit: int = 100) -> List[FuzzyRule]:
        """Searches rules by name pattern within a system.
        
        Args:
            system_id: ID of the fuzzy system
            name_pattern: Search pattern for the name
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of rules matching the name pattern
        """
        pass

    @abstractmethod
    async def filter_rules(self, filters: Dict[str, Any], skip: int = 0, limit: int = 100) -> List[FuzzyRule]:
        """Filters rules using multiple criteria.
        
        Args:
            filters: Dictionary with filtering criteria
                    Examples: {'system_id': '123', 'uses_connector': 'AND', 'has_consequent': True}
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of rules matching the criteria
        """
        pass

    # Counting methods
    @abstractmethod
    async def count_by_system(self, system_id: FuzzySystemId) -> int:
        """Counts rules by system.
        
        Args:
            system_id: ID of the fuzzy system
            
        Returns:
            Number of rules in the system
        """
        pass

    @abstractmethod
    async def count_total(self) -> int:
        """Counts total number of rules.
        
        Returns:
            Total number of fuzzy rules
        """
        pass

    # Date-based queries
    @abstractmethod
    async def get_by_date_range(self, start_date: datetime, end_date: datetime, 
                               skip: int = 0, limit: int = 100) -> List[FuzzyRule]:
        """Gets rules created in a date range.
        
        Args:
            start_date: Start date of the range
            end_date: End date of the range
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of rules created in the specified range
        """
        pass

    # Lightweight queries
    @abstractmethod
    async def get_all_rules_name_description(self, skip: int = 0, limit: int = 100) -> List[Dict[str, Any]]:
        """Gets all rules with only id, name and description fields.
        
        Args:
            skip: Number of records to skip
            limit: Maximum number of records to return
            
        Returns:
            List of dictionaries with id, name and description
        """
        pass
