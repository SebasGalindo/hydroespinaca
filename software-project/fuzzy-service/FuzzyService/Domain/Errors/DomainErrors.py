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


class InvalidReferenceException(DomainError):
    """Raised when a reference_id does not exist in the external service (sensor-service or actuator-service)."""

    def __init__(self, reference_id: str, service: str, variable_type: str):
        self.reference_id = reference_id
        self.service = service
        self.variable_type = variable_type
        message = (
            f"Reference validation failed: reference_id='{reference_id}' "
            f"not found in {service} for variable_type='{variable_type}'"
        )
        super().__init__(message)
