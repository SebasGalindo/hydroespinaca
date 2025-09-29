class DomainError(Exception):
    """Base class for domain-specific exceptions."""


class ValidationError(DomainError):
    """Raised when input data or entity state is invalid for a domain operation."""


class EntityNotFoundError(DomainError):
    """Raised when a requested entity does not exist."""


class DuplicateEntityError(DomainError):
    """Raised when attempting to create or update an entity that violates uniqueness constraints."""


class BusinessRuleViolationError(DomainError):
    """Raised when a business rule is violated during domain operations."""
