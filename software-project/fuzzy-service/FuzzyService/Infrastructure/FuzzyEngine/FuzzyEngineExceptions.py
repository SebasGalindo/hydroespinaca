"""Custom exceptions for FuzzyEngine operations.

Provides a comprehensive hierarchy of exceptions for different fuzzy logic operations
with detailed error messages and context information for debugging and monitoring.
"""

from typing import Any, Dict, Optional
import traceback
from datetime import datetime, timezone


class FuzzyEngineException(Exception):
    """Base exception for all FuzzyEngine operations.
    
    Provides common functionality for error tracking, context preservation,
    and structured error reporting across all fuzzy logic components.
    """
    
    def __init__(
        self,
        message: str,
        error_code: str = "FUZZY_ENGINE_ERROR",
        context: Optional[Dict[str, Any]] = None,
        original_exception: Optional[Exception] = None
    ):
        super().__init__(message)
        self.message = message
        self.error_code = error_code
        self.context = context or {}
        self.original_exception = original_exception
        self.timestamp = datetime.now(timezone.utc)
        self.stack_trace = traceback.format_exc() if original_exception else None
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert exception to dictionary for logging and monitoring."""
        return {
            "error_type": self.__class__.__name__,
            "error_code": self.error_code,
            "message": self.message,
            "context": self.context,
            "timestamp": self.timestamp.isoformat(),
            "original_exception": str(self.original_exception) if self.original_exception else None,
            "stack_trace": self.stack_trace
        }


class ValidationException(FuzzyEngineException):
    """Exception for data validation errors.
    
    Raised when input data doesn't meet required constraints or formats.
    """
    
    def __init__(
        self,
        message: str,
        field_name: Optional[str] = None,
        field_value: Any = None,
        expected_type: Optional[str] = None,
        constraints: Optional[Dict[str, Any]] = None,
        **kwargs
    ):
        context = {
            "field_name": field_name,
            "field_value": field_value,
            "expected_type": expected_type,
            "constraints": constraints
        }
        super().__init__(
            message,
            error_code="VALIDATION_ERROR",
            context=context,
            **kwargs
        )


class MembershipFunctionException(FuzzyEngineException):
    """Exception for membership function operations.
    
    Raised during membership function creation, conversion, or evaluation.
    """
    
    def __init__(
        self,
        message: str,
        function_type: Optional[str] = None,
        parameters: Optional[Dict[str, Any]] = None,
        universe_range: Optional[tuple] = None,
        **kwargs
    ):
        context = {
            "function_type": function_type,
            "parameters": parameters,
            "universe_range": universe_range
        }
        super().__init__(
            message,
            error_code="MEMBERSHIP_FUNCTION_ERROR",
            context=context,
            **kwargs
        )


class FuzzificationException(FuzzyEngineException):
    """Exception for fuzzification process errors.
    
    Raised during conversion of crisp values to fuzzy values.
    """
    
    def __init__(
        self,
        message: str,
        sensor_id: Optional[str] = None,
        crisp_value: Optional[float] = None,
        variable_id: Optional[str] = None,
        terms_count: Optional[int] = None,
        original_exception: Optional[Exception] = None
    ):
        context = {
            "sensor_id": sensor_id,
            "crisp_value": crisp_value,
            "variable_id": variable_id,
            "terms_count": terms_count
        }
        super().__init__(
            message,
            error_code="FUZZIFICATION_ERROR",
            context=context,
            original_exception=original_exception
        )


class RuleEvaluationException(FuzzyEngineException):
    """Exception for rule evaluation errors.
    
    Raised during fuzzy rule processing and firing strength calculation.
    """
    
    def __init__(
        self,
        message: str,
        rule_id: Optional[str] = None,
        conditions_count: Optional[int] = None,
        operator: Optional[str] = None,
        firing_strength: Optional[float] = None,
        **kwargs
    ):
        context = {
            "rule_id": rule_id,
            "conditions_count": conditions_count,
            "operator": operator,
            "firing_strength": firing_strength
        }
        super().__init__(
            message,
            error_code="RULE_EVALUATION_ERROR",
            context=context,
            **kwargs
        )


class AggregationException(FuzzyEngineException):
    """Exception for aggregation process errors.
    
    Raised during combination of multiple rule outputs.
    """
    
    def __init__(
        self,
        message: str,
        aggregation_method: Optional[str] = None,
        rules_count: Optional[int] = None,
        output_term_id: Optional[str] = None,
        **kwargs
    ):
        context = {
            "aggregation_method": aggregation_method,
            "rules_count": rules_count,
            "output_term_id": output_term_id
        }
        super().__init__(
            message,
            error_code="AGGREGATION_ERROR",
            context=context,
            **kwargs
        )


class DefuzzificationException(FuzzyEngineException):
    """Exception for defuzzification process errors.
    
    Raised during conversion of fuzzy values to crisp outputs.
    """
    
    def __init__(
        self,
        message: str,
        defuzz_method: Optional[str] = None,
        term_id: Optional[str] = None,
        aggregated_membership: Optional[float] = None,
        universe_range: Optional[tuple] = None,
        **kwargs
    ):
        context = {
            "defuzz_method": defuzz_method,
            "term_id": term_id,
            "aggregated_membership": aggregated_membership,
            "universe_range": universe_range
        }
        super().__init__(
            message,
            error_code="DEFUZZIFICATION_ERROR",
            context=context,
            **kwargs
        )


class PerformanceException(FuzzyEngineException):
    """Exception for performance-related issues.
    
    Raised when operations exceed time or memory limits.
    """
    
    def __init__(
        self,
        message: str,
        operation: Optional[str] = None,
        execution_time: Optional[float] = None,
        memory_usage: Optional[int] = None,
        limit_exceeded: Optional[str] = None,
        **kwargs
    ):
        context = {
            "operation": operation,
            "execution_time": execution_time,
            "memory_usage": memory_usage,
            "limit_exceeded": limit_exceeded
        }
        super().__init__(
            message,
            error_code="PERFORMANCE_ERROR",
            context=context,
            **kwargs
        )