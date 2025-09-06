"""Fuzzification engine for converting crisp sensor values to fuzzy values.

Provides comprehensive fuzzification with validation, error handling,
and detailed logging for monitoring and debugging.
"""

from typing import Dict, Any, List, Optional, Tuple
import numpy as np
import logging
from dataclasses import dataclass
from datetime import datetime, timezone
from .FuzzyEngineExceptions import FuzzificationException, ValidationException
from .FuzzyEngineConfiguration import FuzzyEngineConfiguration
from .FuzzyEngineMetrics import FuzzyEngineMetrics
from .MembershipFunctionConverter import MembershipFunctionConverter


@dataclass
class FuzzyVariable:
    """Represents a fuzzy variable with its terms."""
    variable_id: str
    name: str
    universe_range: Tuple[float, float]
    terms: Dict[str, 'FuzzyTerm']
    sensor_mapping: Optional[str] = None  # Maps to sensor ID
    
    def validate(self) -> None:
        """Validate fuzzy variable configuration."""
        if not self.variable_id:
            raise ValidationException(
                "Variable ID cannot be empty",
                field_name="variable_id",
                field_value=self.variable_id
            )
        
        if not self.name:
            raise ValidationException(
                "Variable name cannot be empty",
                field_name="name",
                field_value=self.name
            )
        
        if len(self.universe_range) != 2:
            raise ValidationException(
                "Universe range must have exactly 2 elements",
                field_name="universe_range",
                field_value=self.universe_range
            )
        
        min_val, max_val = self.universe_range
        if min_val >= max_val:
            raise ValidationException(
                "Universe range minimum must be less than maximum",
                field_name="universe_range",
                field_value=self.universe_range
            )
        
        if not self.terms:
            raise ValidationException(
                "Variable must have at least one term",
                field_name="terms",
                field_value=self.terms
            )
        
        # Validate all terms
        for term in self.terms.values():
            term.validate()


@dataclass
class FuzzyTerm:
    """Represents a fuzzy term with membership function."""
    term_id: str
    name: str
    function_type: str
    parameters: Dict[str, Any]
    universe: Optional[np.ndarray] = None
    membership_values: Optional[np.ndarray] = None
    
    def validate(self) -> None:
        """Validate fuzzy term configuration."""
        if not self.term_id:
            raise ValidationException(
                "Term ID cannot be empty",
                field_name="term_id",
                field_value=self.term_id
            )
        
        if not self.name:
            raise ValidationException(
                "Term name cannot be empty",
                field_name="name",
                field_value=self.name
            )
        
        if not self.function_type:
            raise ValidationException(
                "Function type cannot be empty",
                field_name="function_type",
                field_value=self.function_type
            )
        
        if not isinstance(self.parameters, dict):
            raise ValidationException(
                "Parameters must be a dictionary",
                field_name="parameters",
                field_value=type(self.parameters).__name__,
                expected_type="dict"
            )


@dataclass
class FuzzificationResult:
    """Result of fuzzification process."""
    variable_id: str
    sensor_id: str
    crisp_value: float
    membership_degrees: Dict[str, float]  # term_id -> membership degree
    processing_time: float
    timestamp: datetime
    warnings: List[str] = None
    
    def __post_init__(self):
        if self.warnings is None:
            self.warnings = []
    
    def get_dominant_term(self) -> Optional[Tuple[str, float]]:
        """Get the term with highest membership degree."""
        if not self.membership_degrees:
            return None
        
        max_term = max(self.membership_degrees.items(), key=lambda x: x[1])
        return max_term if max_term[1] > 0 else None
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert result to dictionary for logging."""
        dominant_term = self.get_dominant_term()
        return {
            "variable_id": self.variable_id,
            "sensor_id": self.sensor_id,
            "crisp_value": self.crisp_value,
            "membership_degrees": self.membership_degrees,
            "dominant_term": {
                "term_id": dominant_term[0] if dominant_term else None,
                "membership_degree": dominant_term[1] if dominant_term else 0.0
            },
            "processing_time": self.processing_time,
            "timestamp": self.timestamp.isoformat(),
            "warnings": self.warnings
        }


@dataclass
class BatchFuzzificationResult:
    """Result of batch fuzzification process."""
    fuzzified_values: Dict[str, Dict[str, float]]  # variable_id -> {term_id -> membership_degree}
    processing_time: float
    timestamp: datetime
    success: bool = True
    warnings: List[str] = None
    missing_sensors: List[str] = None
    
    def __post_init__(self):
        if self.warnings is None:
            self.warnings = []
        if self.missing_sensors is None:
            self.missing_sensors = []
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert result to dictionary for logging."""
        return {
            "fuzzified_values": self.fuzzified_values,
            "processing_time": self.processing_time,
            "timestamp": self.timestamp.isoformat(),
            "success": self.success,
            "warnings": self.warnings,
            "missing_sensors": self.missing_sensors,
            "variables_processed": len(self.fuzzified_values)
        }


class FuzzificationEngine:
    """Engine for fuzzifying crisp sensor values."""
    
    def __init__(
        self,
        config: FuzzyEngineConfiguration,
        metrics: FuzzyEngineMetrics,
        membership_converter: MembershipFunctionConverter
    ):
        self.config = config
        self.metrics = metrics
        self.membership_converter = membership_converter
        self.logger = logging.getLogger(__name__)
        
        # Cache for prepared fuzzy variables
        self._variable_cache: Dict[str, FuzzyVariable] = {}
        
        self.logger.info("FuzzificationEngine initialized", extra={
            "strict_range_validation": config.validation_settings.strict_range_validation,
            "allow_zero_membership": config.validation_settings.allow_zero_membership
        })
    
    def fuzzify_sensor_readings(
        self,
        sensor_readings: Dict[str, float],
        fuzzy_variables: List[Dict[str, Any]],
        system_id: str
    ) -> BatchFuzzificationResult:
        """Fuzzify multiple sensor readings.
        
        Args:
            sensor_readings: Dictionary of sensor_id -> value
            fuzzy_variables: List of fuzzy variable definitions from database
            system_id: ID of the fuzzy system for logging
            
        Returns:
            BatchFuzzificationResult containing fuzzified values and metadata
            
        Raises:
            FuzzificationException: If fuzzification fails
        """
        with self.metrics.monitor_operation("fuzzification_batch") as monitor:
            try:
                self.logger.info("Starting batch fuzzification", extra={
                    "system_id": system_id,
                    "sensor_count": len(sensor_readings),
                    "variable_count": len(fuzzy_variables),
                    "sensors": list(sensor_readings.keys())
                })
                
                # Validate inputs
                self._validate_batch_inputs(sensor_readings, fuzzy_variables, system_id)
                
                # Prepare fuzzy variables
                prepared_variables = self._prepare_fuzzy_variables(fuzzy_variables)
                
                # Fuzzify each variable
                results = {}
                warnings = []
                
                for variable in prepared_variables.values():
                    try:
                        result = self._fuzzify_variable(
                            variable,
                            sensor_readings,
                            system_id
                        )
                        
                        if result:
                            results[variable.variable_id] = result
                            warnings.extend(result.warnings)
                        else:
                            warnings.append(f"No sensor data for variable {variable.variable_id}")
                    
                    except Exception as e:
                        error_msg = f"Failed to fuzzify variable {variable.variable_id}: {str(e)}"
                        warnings.append(error_msg)
                        self.logger.warning(error_msg, extra={
                            "variable_id": variable.variable_id,
                            "system_id": system_id,
                            "error": str(e)
                        })
                        
                        # Continue with other variables unless it's a critical error
                        if isinstance(e, (ValidationException, FuzzificationException)):
                            continue
                        else:
                            monitor.mark_error()
                            raise
                
                # Calculate total processing time
                processing_time = sum(result.processing_time for result in results.values())
                
                # Convert results to the format expected by BatchFuzzificationResult
                fuzzified_values = {}
                missing_sensors = []
                
                for variable in prepared_variables.values():
                    if variable.variable_id in results:
                        fuzzified_values[variable.variable_id] = results[variable.variable_id].membership_degrees
                    else:
                        missing_sensors.append(variable.variable_id)
                
                # Create batch result
                batch_result = BatchFuzzificationResult(
                    fuzzified_values=fuzzified_values,
                    processing_time=processing_time,
                    timestamp=datetime.now(timezone.utc),
                    success=len(results) > 0,
                    warnings=warnings,
                    missing_sensors=missing_sensors
                )
                
                # Log summary
                self.logger.info("Batch fuzzification completed", extra={
                    "system_id": system_id,
                    "successful_variables": len(results),
                    "total_variables": len(prepared_variables),
                    "warnings_count": len(warnings),
                    "results": {var_id: result.to_dict() for var_id, result in results.items()}
                })
                
                if warnings:
                    self.logger.warning("Fuzzification warnings", extra={
                        "system_id": system_id,
                        "warnings": warnings
                    })
                
                return batch_result
                
            except Exception as e:
                monitor.mark_error()
                self.logger.error("Batch fuzzification failed", extra={
                    "system_id": system_id,
                    "sensor_readings": sensor_readings,
                    "error": str(e)
                })
                
                if isinstance(e, (FuzzificationException, ValidationException)):
                    raise
                
                raise FuzzificationException(
                    f"Unexpected error during batch fuzzification: {str(e)}",
                    original_exception=e
                )
    
    def fuzzify_single_value(
        self,
        crisp_value: float,
        variable_definition: Dict[str, Any],
        variable_id: str,
        sensor_id: str = "unknown"
    ) -> FuzzificationResult:
        """Fuzzify a single crisp value.
        
        Args:
            crisp_value: The crisp value to fuzzify
            variable_definition: Fuzzy variable definition from database
            variable_id: ID of the variable
            sensor_id: ID of the sensor (for logging)
            
        Returns:
            FuzzificationResult
            
        Raises:
            FuzzificationException: If fuzzification fails
        """
        with self.metrics.monitor_operation("fuzzification_single") as monitor:
            try:
                start_time = datetime.now(timezone.utc)
                
                # Validate inputs
                self._validate_single_inputs(crisp_value, variable_definition, variable_id)
                
                # Prepare fuzzy variable
                fuzzy_variable = self._prepare_single_fuzzy_variable(variable_definition, variable_id)
                
                # Perform fuzzification
                membership_degrees = self._calculate_membership_degrees(
                    crisp_value,
                    fuzzy_variable
                )
                
                # Create result
                processing_time = (datetime.now(timezone.utc) - start_time).total_seconds()
                warnings = []
                
                # Check for warnings
                if self._is_value_out_of_range(crisp_value, fuzzy_variable.universe_range):
                    warnings.append(f"Value {crisp_value} is outside universe range {fuzzy_variable.universe_range}")
                
                if all(degree == 0.0 for degree in membership_degrees.values()):
                    warnings.append(f"All membership degrees are zero for value {crisp_value}")
                
                result = FuzzificationResult(
                    variable_id=variable_id,
                    sensor_id=sensor_id,
                    crisp_value=crisp_value,
                    membership_degrees=membership_degrees,
                    processing_time=processing_time,
                    timestamp=start_time,
                    warnings=warnings
                )
                
                self.logger.debug("Single value fuzzified", extra=result.to_dict())
                
                return result
                
            except Exception as e:
                monitor.mark_error()
                self.logger.error("Single value fuzzification failed", extra={
                    "variable_id": variable_id,
                    "sensor_id": sensor_id,
                    "crisp_value": crisp_value,
                    "error": str(e)
                })
                
                if isinstance(e, (FuzzificationException, ValidationException)):
                    raise
                
                raise FuzzificationException(
                    f"Unexpected error during single value fuzzification: {str(e)}",
                    sensor_id=sensor_id,
                    crisp_value=crisp_value,
                    variable_id=variable_id,
                    original_exception=e
                )
    
    def _validate_batch_inputs(
        self,
        sensor_readings: Dict[str, float],
        fuzzy_variables: List[Dict[str, Any]],
        system_id: str
    ) -> None:
        """Validate inputs for batch fuzzification."""
        if not isinstance(sensor_readings, dict):
            raise ValidationException(
                "sensor_readings must be a dictionary",
                field_name="sensor_readings",
                field_value=type(sensor_readings).__name__,
                expected_type="dict"
            )
        
        if not isinstance(fuzzy_variables, list):
            raise ValidationException(
                "fuzzy_variables must be a list",
                field_name="fuzzy_variables",
                field_value=type(fuzzy_variables).__name__,
                expected_type="list"
            )
        
        if not system_id or not isinstance(system_id, str):
            raise ValidationException(
                "system_id must be a non-empty string",
                field_name="system_id",
                field_value=system_id,
                expected_type="str"
            )
        
        # Validate empty sensor readings if strict validation is enabled
        if self.config.validation_settings.strict_range_validation and len(sensor_readings) == 0:
            raise ValidationException(
                "sensor_readings cannot be empty when strict validation is enabled",
                field_name="sensor_readings",
                field_value=sensor_readings,
                expected_type="non-empty dict"
            )
        
        # Validate sensor readings
        for sensor_id, value in sensor_readings.items():
            if not isinstance(sensor_id, str) or not sensor_id:
                raise ValidationException(
                    "Sensor ID must be a non-empty string",
                    field_name="sensor_id",
                    field_value=sensor_id,
                    expected_type="str"
                )
            
            if not isinstance(value, (int, float)) or np.isnan(value) or np.isinf(value):
                raise ValidationException(
                    "Sensor value must be a finite number",
                    field_name=f"sensor_readings[{sensor_id}]",
                    field_value=value,
                    expected_type="float"
                )
        
        # Check variable count limits
        if len(fuzzy_variables) > self.config.performance_limits.max_variables_per_system:
            raise ValidationException(
                f"Too many variables: {len(fuzzy_variables)} > {self.config.performance_limits.max_variables_per_system}",
                field_name="fuzzy_variables",
                field_value=len(fuzzy_variables),
                constraints={"max_variables": self.config.performance_limits.max_variables_per_system}
            )
    
    def _validate_single_inputs(
        self,
        crisp_value: float,
        variable_definition: Dict[str, Any],
        variable_id: str
    ) -> None:
        """Validate inputs for single value fuzzification."""
        if not isinstance(crisp_value, (int, float)) or np.isnan(crisp_value) or np.isinf(crisp_value):
            raise ValidationException(
                "Crisp value must be a finite number",
                field_name="crisp_value",
                field_value=crisp_value,
                expected_type="float"
            )
        
        if not isinstance(variable_definition, dict):
            raise ValidationException(
                "variable_definition must be a dictionary",
                field_name="variable_definition",
                field_value=type(variable_definition).__name__,
                expected_type="dict"
            )
        
        if not variable_id or not isinstance(variable_id, str):
            raise ValidationException(
                "variable_id must be a non-empty string",
                field_name="variable_id",
                field_value=variable_id,
                expected_type="str"
            )
    
    def _prepare_fuzzy_variables(self, variable_definitions: List[Dict[str, Any]]) -> Dict[str, FuzzyVariable]:
        """Prepare fuzzy variables from database definitions."""
        prepared_variables = {}
        
        for var_def in variable_definitions:
            try:
                variable_id = var_def.get('id') or var_def.get('variable_id')
                if not variable_id:
                    self.logger.warning("Variable definition missing ID", extra={"definition": var_def})
                    continue
                
                # Check cache first
                cache_key = f"{variable_id}_{hash(str(var_def))}"
                if cache_key in self._variable_cache:
                    prepared_variables[variable_id] = self._variable_cache[cache_key]
                    continue
                
                # Prepare new variable
                fuzzy_variable = self._prepare_single_fuzzy_variable(var_def, variable_id)
                
                # Cache it
                self._variable_cache[cache_key] = fuzzy_variable
                prepared_variables[variable_id] = fuzzy_variable
                
            except Exception as e:
                self.logger.error("Failed to prepare fuzzy variable", extra={
                    "variable_definition": var_def,
                    "error": str(e)
                })
                # Continue with other variables
                continue
        
        return prepared_variables
    
    def _prepare_single_fuzzy_variable(self, var_def: Dict[str, Any], variable_id: str) -> FuzzyVariable:
        """Prepare a single fuzzy variable."""
        # Extract variable information
        name = var_def.get('name', variable_id)
        min_val = var_def.get('min_value', 0.0)
        max_val = var_def.get('max_value', 100.0)
        sensor_mapping = var_def.get('sensor_id')
        
        universe_range = (float(min_val), float(max_val))
        
        # Prepare terms
        terms = {}
        terms_data = var_def.get('terms', [])
        
        if len(terms_data) > self.config.performance_limits.max_terms_per_variable:
            raise ValidationException(
                f"Too many terms: {len(terms_data)} > {self.config.performance_limits.max_terms_per_variable}",
                field_name="terms",
                field_value=len(terms_data),
                constraints={"max_terms": self.config.performance_limits.max_terms_per_variable}
            )
        
        for term_data in terms_data:
            term_id = term_data.get('id') or term_data.get('term_id')
            if not term_id:
                continue
            
            term = FuzzyTerm(
                term_id=term_id,
                name=term_data.get('name', term_id),
                function_type=term_data.get('function_type', 'triangular'),
                parameters=term_data.get('parameters', {})
            )
            
            # Convert membership function
            try:
                universe, membership_values = self.membership_converter.convert_membership_function(
                    term.function_type,
                    term.parameters,
                    universe_range
                )
                
                term.universe = universe
                term.membership_values = membership_values
                
            except Exception as e:
                self.logger.error("Failed to convert membership function", extra={
                    "variable_id": variable_id,
                    "term_id": term_id,
                    "function_type": term.function_type,
                    "parameters": term.parameters,
                    "error": str(e)
                })
                raise FuzzificationException(
                    f"Failed to prepare term {term_id} for variable {variable_id}: {str(e)}",
                    variable_id=variable_id,
                    original_exception=e
                )
            
            terms[term_id] = term
        
        # Create and validate fuzzy variable
        fuzzy_variable = FuzzyVariable(
            variable_id=variable_id,
            name=name,
            universe_range=universe_range,
            terms=terms,
            sensor_mapping=sensor_mapping
        )
        
        fuzzy_variable.validate()
        
        return fuzzy_variable
    
    def _fuzzify_variable(
        self,
        fuzzy_variable: FuzzyVariable,
        sensor_readings: Dict[str, float],
        system_id: str
    ) -> Optional[FuzzificationResult]:
        """Fuzzify a single variable."""
        # Find sensor value
        sensor_id = fuzzy_variable.sensor_mapping
        if not sensor_id or sensor_id not in sensor_readings:
            self.logger.debug("No sensor data for variable", extra={
                "variable_id": fuzzy_variable.variable_id,
                "sensor_mapping": sensor_id,
                "available_sensors": list(sensor_readings.keys()),
                "system_id": system_id
            })
            return None
        
        crisp_value = sensor_readings[sensor_id]
        
        return self.fuzzify_single_value(
            crisp_value,
            {  # Convert back to dict format for compatibility
                'id': fuzzy_variable.variable_id,
                'name': fuzzy_variable.name,
                'min_value': fuzzy_variable.universe_range[0],
                'max_value': fuzzy_variable.universe_range[1],
                'sensor_id': fuzzy_variable.sensor_mapping,
                'terms': [
                    {
                        'id': term.term_id,
                        'name': term.name,
                        'function_type': term.function_type,
                        'parameters': term.parameters
                    }
                    for term in fuzzy_variable.terms.values()
                ]
            },
            fuzzy_variable.variable_id,
            sensor_id
        )
    
    def _calculate_membership_degrees(
        self,
        crisp_value: float,
        fuzzy_variable: FuzzyVariable
    ) -> Dict[str, float]:
        """Calculate membership degrees for all terms."""
        membership_degrees = {}
        
        for term_id, term in fuzzy_variable.terms.items():
            try:
                # Interpolate membership value
                membership_degree = float(np.interp(
                    crisp_value,
                    term.universe,
                    term.membership_values
                ))
                
                # Ensure valid range
                membership_degree = max(0.0, min(1.0, membership_degree))
                
                membership_degrees[term_id] = membership_degree
                
            except Exception as e:
                self.logger.error("Failed to calculate membership degree", extra={
                    "variable_id": fuzzy_variable.variable_id,
                    "term_id": term_id,
                    "crisp_value": crisp_value,
                    "error": str(e)
                })
                
                # Set to zero on error
                membership_degrees[term_id] = 0.0
        
        return membership_degrees
    
    def _is_value_out_of_range(self, value: float, universe_range: Tuple[float, float]) -> bool:
        """Check if value is outside universe range."""
        min_val, max_val = universe_range
        return value < min_val or value > max_val
    
    def clear_cache(self) -> None:
        """Clear variable cache."""
        self._variable_cache.clear()
        self.logger.info("Fuzzification variable cache cleared")
    
    def get_cache_stats(self) -> Dict[str, Any]:
        """Get cache statistics."""
        return {
            "variable_cache_size": len(self._variable_cache),
            "cached_variables": list(self._variable_cache.keys())
        }