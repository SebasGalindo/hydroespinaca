"""Configuration settings for FuzzyEngine operations.

Provides comprehensive configuration management with validation,
performance tuning, and monitoring settings for fuzzy logic operations.

Features:
- JSON Schema validation
- Cross-component dependency validation
- Intelligent default value management
- Configuration profiles for different environments
- Runtime configuration updates with validation
- Configuration inheritance and composition
"""

from dataclasses import dataclass, field
from typing import Dict, Any, Optional, List, Union, Callable, Type
from enum import Enum
import logging
import json
import jsonschema
from pathlib import Path
import os
from copy import deepcopy
from .FuzzyEngineExceptions import ValidationException


class DefuzzificationMethod(Enum):
    """Available defuzzification methods."""
    CENTROID = "centroid"
    BISECTOR = "bisector"
    MOM = "mom"  # Mean of Maximum
    SOM = "som"  # Smallest of Maximum
    LOM = "lom"  # Largest of Maximum
    
    @classmethod
    def get_performance_ranking(cls) -> Dict[str, int]:
        """Get performance ranking of methods (1=fastest, 5=slowest)"""
        return {
            cls.CENTROID.value: 3,
            cls.BISECTOR.value: 4,
            cls.MOM.value: 1,
            cls.SOM.value: 2,
            cls.LOM.value: 2
        }
    
    @classmethod
    def get_accuracy_ranking(cls) -> Dict[str, int]:
        """Get accuracy ranking of methods (1=most accurate, 5=least accurate)"""
        return {
            cls.CENTROID.value: 1,
            cls.BISECTOR.value: 2,
            cls.MOM.value: 4,
            cls.SOM.value: 5,
            cls.LOM.value: 5
        }


class AggregationMethod(Enum):
    """Available aggregation methods for combining rule outputs."""
    MAX = "max"
    SUM = "sum"
    AVERAGE = "average"
    WEIGHTED_AVERAGE = "weighted_average"
    PROBABILISTIC_OR = "probor"
    
    @classmethod
    def get_compatibility_matrix(cls) -> Dict[str, List[str]]:
        """Get compatibility matrix with defuzzification methods"""
        return {
            cls.MAX.value: ["centroid", "bisector", "mom", "som", "lom"],
            cls.SUM.value: ["centroid", "bisector"],
            cls.AVERAGE.value: ["centroid", "bisector"],
            cls.WEIGHTED_AVERAGE.value: ["centroid", "bisector"],
            cls.PROBABILISTIC_OR.value: ["centroid", "bisector", "mom"]
        }


class LogicalOperator(Enum):
    """Available logical operators for rule conditions."""
    AND = "and"
    OR = "or"
    NOT = "not"
    
    @classmethod
    def get_precedence(cls) -> Dict[str, int]:
        """Get operator precedence (higher number = higher precedence)"""
        return {
            cls.NOT.value: 3,
            cls.AND.value: 2,
            cls.OR.value: 1
        }


class ConfigurationProfile(Enum):
    """Predefined configuration profiles for different environments"""
    DEVELOPMENT = "development"
    TESTING = "testing"
    STAGING = "staging"
    PRODUCTION = "production"
    HIGH_PERFORMANCE = "high_performance"
    LOW_MEMORY = "low_memory"
    DEBUG = "debug"


class ValidationLevel(Enum):
    """Validation strictness levels"""
    STRICT = "strict"
    NORMAL = "normal"
    RELAXED = "relaxed"
    DISABLED = "disabled"


class ConfigurationSchema:
    """JSON Schema definitions for configuration validation"""
    
    @staticmethod
    def get_performance_limits_schema() -> Dict[str, Any]:
        """Get JSON schema for PerformanceLimits"""
        return {
            "type": "object",
            "properties": {
                "max_execution_time_seconds": {
                    "type": "number",
                    "minimum": 0.1,
                    "maximum": 300.0,
                    "description": "Maximum execution time in seconds"
                },
                "max_memory_usage_mb": {
                    "type": "integer",
                    "minimum": 10,
                    "maximum": 10240,
                    "description": "Maximum memory usage in MB"
                },
                "max_rules_per_evaluation": {
                    "type": "integer",
                    "minimum": 1,
                    "maximum": 100000,
                    "description": "Maximum number of rules per evaluation"
                },
                "max_variables_per_system": {
                    "type": "integer",
                    "minimum": 1,
                    "maximum": 1000,
                    "description": "Maximum number of variables per system"
                },
                "max_terms_per_variable": {
                    "type": "integer",
                    "minimum": 1,
                    "maximum": 100,
                    "description": "Maximum number of terms per variable"
                },
                "max_conditions_per_rule": {
                    "type": "integer",
                    "minimum": 1,
                    "maximum": 50,
                    "description": "Maximum number of conditions per rule"
                },
                "cache_size_limit": {
                    "type": "integer",
                    "minimum": 10,
                    "maximum": 100000,
                    "description": "Maximum cache size limit"
                }
            },
            "required": ["max_execution_time_seconds", "max_memory_usage_mb", "max_rules_per_evaluation"],
            "additionalProperties": False
        }
    
    @staticmethod
    def get_validation_settings_schema() -> Dict[str, Any]:
        """Get JSON schema for ValidationSettings"""
        return {
            "type": "object",
            "properties": {
                "strict_range_validation": {"type": "boolean"},
                "allow_zero_membership": {"type": "boolean"},
                "require_normalized_membership": {"type": "boolean"},
                "validate_universe_continuity": {"type": "boolean"},
                "min_universe_points": {
                    "type": "integer",
                    "minimum": 10,
                    "maximum": 10000
                },
                "max_universe_points": {
                    "type": "integer",
                    "minimum": 100,
                    "maximum": 100000
                },
                "membership_tolerance": {
                    "type": "number",
                    "minimum": 1e-10,
                    "maximum": 1e-3
                }
            },
            "additionalProperties": False
        }
    
    @staticmethod
    def get_full_configuration_schema() -> Dict[str, Any]:
        """Get complete JSON schema for FuzzyEngineConfiguration"""
        return {
            "type": "object",
            "properties": {
                "default_defuzzification_method": {
                    "type": "string",
                    "enum": [method.value for method in DefuzzificationMethod]
                },
                "default_aggregation_method": {
                    "type": "string",
                    "enum": [method.value for method in AggregationMethod]
                },
                "default_logical_operator": {
                    "type": "string",
                    "enum": [op.value for op in LogicalOperator]
                },
                "default_universe_resolution": {
                    "type": "integer",
                    "minimum": 50,
                    "maximum": 10000
                },
                "auto_expand_universe": {"type": "boolean"},
                "universe_expansion_factor": {
                    "type": "number",
                    "minimum": 0.01,
                    "maximum": 0.5
                },
                "performance_limits": ConfigurationSchema.get_performance_limits_schema(),
                "validation_settings": ConfigurationSchema.get_validation_settings_schema(),
                "enable_circuit_breaker": {"type": "boolean"},
                "circuit_breaker_failure_threshold": {
                    "type": "integer",
                    "minimum": 1,
                    "maximum": 100
                },
                "circuit_breaker_timeout_seconds": {
                    "type": "integer",
                    "minimum": 1,
                    "maximum": 3600
                }
            },
            "additionalProperties": False
        }


class ConfigurationValidator:
    """Advanced configuration validator with schema and dependency validation"""
    
    def __init__(self):
        self.logger = logging.getLogger(f"{__name__}.{self.__class__.__name__}")
        self.schema = ConfigurationSchema()
    
    def validate_schema(self, config_dict: Dict[str, Any]) -> List[str]:
        """Validate configuration against JSON schema"""
        errors = []
        
        try:
            schema = self.schema.get_full_configuration_schema()
            jsonschema.validate(config_dict, schema)
        except jsonschema.ValidationError as e:
            errors.append(f"Schema validation error: {e.message} at path {'.'.join(str(p) for p in e.absolute_path)}")
        except jsonschema.SchemaError as e:
            errors.append(f"Schema definition error: {e.message}")
        
        return errors
    
    def validate_dependencies(self, config: 'FuzzyEngineConfiguration') -> List[str]:
        """Validate cross-component dependencies"""
        errors = []
        
        # Validate aggregation-defuzzification compatibility
        agg_method = config.default_aggregation_method.value
        defuzz_method = config.default_defuzzification_method.value
        
        compatibility_matrix = AggregationMethod.get_compatibility_matrix()
        if agg_method in compatibility_matrix:
            compatible_methods = compatibility_matrix[agg_method]
            if defuzz_method not in compatible_methods:
                errors.append(
                    f"Aggregation method '{agg_method}' is not compatible with "
                    f"defuzzification method '{defuzz_method}'. "
                    f"Compatible methods: {compatible_methods}"
                )
        
        # Validate performance vs accuracy trade-offs
        if config.performance_limits.max_execution_time_seconds < 1.0:
            perf_ranking = DefuzzificationMethod.get_performance_ranking()
            if perf_ranking.get(defuzz_method, 3) > 2:
                errors.append(
                    f"Defuzzification method '{defuzz_method}' may be too slow for "
                    f"execution time limit of {config.performance_limits.max_execution_time_seconds}s. "
                    f"Consider using faster methods like 'mom' or 'som'."
                )
        
        # Validate memory constraints
        if config.performance_limits.max_memory_usage_mb < 50:
            if config.cache_settings.max_cache_entries > 100:
                errors.append(
                    f"Cache size ({config.cache_settings.max_cache_entries}) may exceed "
                    f"memory limit ({config.performance_limits.max_memory_usage_mb}MB). "
                    f"Consider reducing cache size for low-memory environments."
                )
        
        # Validate universe resolution vs performance
        if (config.default_universe_resolution > 1000 and 
            config.performance_limits.max_execution_time_seconds < 2.0):
            errors.append(
                f"High universe resolution ({config.default_universe_resolution}) "
                f"may cause performance issues with strict time limit "
                f"({config.performance_limits.max_execution_time_seconds}s)."
            )
        
        # Validate validation settings consistency
        if (not config.validation_settings.strict_range_validation and 
            config.validation_settings.require_normalized_membership):
            errors.append(
                "Requiring normalized membership without strict range validation "
                "may lead to inconsistent behavior."
            )
        
        return errors
    
    def validate_profile_consistency(self, config: 'FuzzyEngineConfiguration', 
                                   profile: ConfigurationProfile) -> List[str]:
        """Validate configuration consistency with selected profile"""
        errors = []
        
        if profile == ConfigurationProfile.PRODUCTION:
            if config.logging_settings.log_level == "DEBUG":
                errors.append("DEBUG logging is not recommended for production")
            
            if config.logging_settings.log_membership_calculations:
                errors.append("Detailed membership logging should be disabled in production")
            
            if config.performance_limits.max_execution_time_seconds > 10.0:
                errors.append("Production environments should have stricter execution time limits")
        
        elif profile == ConfigurationProfile.HIGH_PERFORMANCE:
            if config.validation_settings.strict_range_validation:
                errors.append("Strict validation may impact performance in high-performance mode")
            
            if config.cache_settings.cache_ttl_seconds < 60:
                errors.append("Short cache TTL may reduce performance benefits")
        
        elif profile == ConfigurationProfile.LOW_MEMORY:
            if config.performance_limits.max_memory_usage_mb > 100:
                errors.append("Memory limit too high for low-memory profile")
            
            if config.cache_settings.max_cache_entries > 500:
                errors.append("Cache size too large for low-memory profile")
        
        return errors


@dataclass
class PerformanceLimits:
    """Performance limits and thresholds with advanced validation."""
    max_execution_time_seconds: float = 5.0
    max_memory_usage_mb: int = 100
    max_rules_per_evaluation: int = 1000
    max_variables_per_system: int = 50
    max_terms_per_variable: int = 20
    max_conditions_per_rule: int = 10
    cache_size_limit: int = 1000
    max_concurrent_evaluations: int = 10
    parallel_threshold: int = 5
    max_total_processing_time_ms: float = 5000.0
    max_workers: int = 4
    max_evaluation_time_ms: float = 1000.0
    max_aggregation_time_ms: float = 500.0
    max_defuzzification_time_ms: float = 500.0
    defuzzification_resolution: int = 200
    max_sensors_per_evaluation: int = 100
    max_consequents_per_aggregation: int = 1000
    min_available_memory_mb: int = 100
    max_processing_time_seconds: float = 30.0
    enable_async_processing: bool = True
    
    def validate(self) -> None:
        """Validate performance limits with enhanced checks."""
        validator = ConfigurationValidator()
        
        # Convert to dict for schema validation
        config_dict = self.__dict__
        schema_errors = validator.validate_schema({"performance_limits": config_dict})
        
        if schema_errors:
            raise ValidationException(
                f"Performance limits validation failed: {'; '.join(schema_errors)}",
                field_name="performance_limits",
                field_value=config_dict
            )
        
        # Additional business logic validation
        if self.max_execution_time_seconds <= 0:
            raise ValidationException(
                "max_execution_time_seconds must be positive",
                field_name="max_execution_time_seconds",
                field_value=self.max_execution_time_seconds
            )
        
        if self.max_memory_usage_mb <= 0:
            raise ValidationException(
                "max_memory_usage_mb must be positive",
                field_name="max_memory_usage_mb",
                field_value=self.max_memory_usage_mb
            )
        
        if self.max_rules_per_evaluation <= 0:
            raise ValidationException(
                "max_rules_per_evaluation must be positive",
                field_name="max_rules_per_evaluation",
                field_value=self.max_rules_per_evaluation
            )
        
        # Cross-field validation
        estimated_memory_per_rule = 0.1  # MB per rule (rough estimate)
        estimated_total_memory = self.max_rules_per_evaluation * estimated_memory_per_rule
        
        if estimated_total_memory > self.max_memory_usage_mb * 0.8:
            raise ValidationException(
                f"Maximum rules ({self.max_rules_per_evaluation}) may exceed "
                f"memory limit ({self.max_memory_usage_mb}MB). "
                f"Estimated memory usage: {estimated_total_memory:.1f}MB",
                field_name="max_rules_per_evaluation",
                field_value=self.max_rules_per_evaluation,
                constraints={"max_memory_mb": self.max_memory_usage_mb}
            )


@dataclass
class ValidationSettings:
    """Validation settings for fuzzy operations with enhanced validation."""
    strict_range_validation: bool = True
    allow_zero_membership: bool = True
    require_normalized_membership: bool = False
    validate_universe_continuity: bool = True
    min_universe_points: int = 100
    max_universe_points: int = 10000
    membership_tolerance: float = 1e-6
    
    def validate(self) -> None:
        """Validate validation settings with enhanced checks."""
        validator = ConfigurationValidator()
        
        # Convert to dict for schema validation
        config_dict = self.__dict__
        schema_errors = validator.validate_schema({"validation_settings": config_dict})
        
        if schema_errors:
            raise ValidationException(
                f"Validation settings validation failed: {'; '.join(schema_errors)}",
                field_name="validation_settings",
                field_value=config_dict
            )
        
        # Additional business logic validation
        if self.min_universe_points <= 0:
            raise ValidationException(
                "min_universe_points must be positive",
                field_name="min_universe_points",
                field_value=self.min_universe_points
            )
        
        if self.max_universe_points <= self.min_universe_points:
            raise ValidationException(
                "max_universe_points must be greater than min_universe_points",
                field_name="max_universe_points",
                field_value=self.max_universe_points,
                constraints={"min_universe_points": self.min_universe_points}
            )
        
        if self.membership_tolerance <= 0 or self.membership_tolerance >= 1:
            raise ValidationException(
                "membership_tolerance must be between 0 and 1",
                field_name="membership_tolerance",
                field_value=self.membership_tolerance
            )
        
        # Cross-field validation
        if self.require_normalized_membership and not self.strict_range_validation:
            raise ValidationException(
                "Normalized membership requires strict range validation to be enabled",
                field_name="require_normalized_membership",
                field_value=self.require_normalized_membership,
                constraints={"strict_range_validation": self.strict_range_validation}
            )


@dataclass
class CacheSettings:
    """Cache configuration for performance optimization with advanced validation."""
    enable_membership_cache: bool = True
    enable_rule_cache: bool = True
    enable_defuzzification_cache: bool = True
    cache_ttl_seconds: int = 300  # 5 minutes
    max_cache_entries: int = 1000
    cache_cleanup_interval_seconds: int = 60
    cache_hit_ratio_threshold: float = 0.7
    cache_memory_limit_mb: int = 50
    
    def validate(self) -> None:
        """Validate cache settings with enhanced checks."""
        if self.cache_ttl_seconds <= 0:
            raise ValidationException(
                "cache_ttl_seconds must be positive",
                field_name="cache_ttl_seconds",
                field_value=self.cache_ttl_seconds
            )
        
        if self.max_cache_entries <= 0:
            raise ValidationException(
                "max_cache_entries must be positive",
                field_name="max_cache_entries",
                field_value=self.max_cache_entries
            )
        
        if self.cache_cleanup_interval_seconds <= 0:
            raise ValidationException(
                "cache_cleanup_interval_seconds must be positive",
                field_name="cache_cleanup_interval_seconds",
                field_value=self.cache_cleanup_interval_seconds
            )
        
        if not (0.0 <= self.cache_hit_ratio_threshold <= 1.0):
            raise ValidationException(
                "cache_hit_ratio_threshold must be between 0.0 and 1.0",
                field_name="cache_hit_ratio_threshold",
                field_value=self.cache_hit_ratio_threshold
            )
        
        if self.cache_memory_limit_mb <= 0:
            raise ValidationException(
                "cache_memory_limit_mb must be positive",
                field_name="cache_memory_limit_mb",
                field_value=self.cache_memory_limit_mb
            )
        
        # Cross-field validation
        if self.cache_cleanup_interval_seconds > self.cache_ttl_seconds:
            raise ValidationException(
                "cache_cleanup_interval_seconds should not exceed cache_ttl_seconds",
                field_name="cache_cleanup_interval_seconds",
                field_value=self.cache_cleanup_interval_seconds,
                constraints={"cache_ttl_seconds": self.cache_ttl_seconds}
            )
        
        # Estimate memory usage
        estimated_memory_per_entry = 0.05  # MB per cache entry (rough estimate)
        estimated_total_memory = self.max_cache_entries * estimated_memory_per_entry
        
        if estimated_total_memory > self.cache_memory_limit_mb:
            raise ValidationException(
                f"Maximum cache entries ({self.max_cache_entries}) may exceed "
                f"memory limit ({self.cache_memory_limit_mb}MB). "
                f"Estimated memory usage: {estimated_total_memory:.1f}MB",
                field_name="max_cache_entries",
                field_value=self.max_cache_entries,
                constraints={"cache_memory_limit_mb": self.cache_memory_limit_mb}
            )


class _CacheCompat:
    """Backward-compatibility wrapper to expose config.cache with max_cache_size alias.
    Maps max_cache_size <-> cache_settings.max_cache_entries and proxies other attributes.
    """
    def __init__(self, settings: 'CacheSettings'):
        # Avoid recursion by setting internal attribute directly
        super().__setattr__('_settings', settings)
    
    def __getattr__(self, name: str):
        if name == 'max_cache_size':
            return self._settings.max_cache_entries
        # Proxy any other attribute to underlying settings
        return getattr(self._settings, name)
    
    def __setattr__(self, name: str, value):
        if name == 'max_cache_size':
            # Keep as int, ensure non-negative
            try:
                ivalue = int(value)
            except Exception:
                ivalue = value
            self._settings.max_cache_entries = ivalue
        elif name == '_settings':
            super().__setattr__(name, value)
        else:
            setattr(self._settings, name, value)


@dataclass
class LoggingSettings:
    """Logging configuration for monitoring and debugging with advanced validation."""
    log_level: str = "INFO"
    log_performance_metrics: bool = True
    log_rule_evaluations: bool = False  # Detailed logging, can be verbose
    log_membership_calculations: bool = False  # Very detailed, use for debugging
    log_cache_operations: bool = False
    log_validation_errors: bool = True
    structured_logging: bool = True
    include_context_in_logs: bool = True
    max_log_file_size_mb: int = 100
    max_log_files: int = 5
    log_rotation_enabled: bool = True
    
    def validate(self) -> None:
        """Validate logging settings with enhanced checks."""
        valid_levels = ["DEBUG", "INFO", "WARNING", "ERROR", "CRITICAL"]
        if self.log_level not in valid_levels:
            raise ValidationException(
                f"log_level must be one of {valid_levels}",
                field_name="log_level",
                field_value=self.log_level,
                constraints={"valid_values": valid_levels}
            )
        
        if self.max_log_file_size_mb <= 0:
            raise ValidationException(
                "max_log_file_size_mb must be positive",
                field_name="max_log_file_size_mb",
                field_value=self.max_log_file_size_mb
            )
        
        if self.max_log_files <= 0:
            raise ValidationException(
                "max_log_files must be positive",
                field_name="max_log_files",
                field_value=self.max_log_files
            )
        
        # Performance impact validation
        verbose_logging_count = sum([
            self.log_rule_evaluations,
            self.log_membership_calculations,
            self.log_cache_operations
        ])
        
        if verbose_logging_count > 1 and self.log_level == "DEBUG":
            raise ValidationException(
                "Multiple verbose logging options with DEBUG level may impact performance",
                field_name="log_level",
                field_value=self.log_level,
                constraints={"verbose_options_enabled": verbose_logging_count}
            )


@dataclass
class FuzzyEngineConfiguration:
    """Main configuration class for FuzzyEngine with advanced validation and intelligent defaults."""
    
    # Default methods
    default_defuzzification_method: DefuzzificationMethod = DefuzzificationMethod.CENTROID
    default_aggregation_method: AggregationMethod = AggregationMethod.MAX
    default_logical_operator: LogicalOperator = LogicalOperator.AND
    
    # Universe settings
    default_universe_resolution: int = 200
    auto_expand_universe: bool = True
    universe_expansion_factor: float = 0.1  # 10% expansion
    
    # Component settings
    performance_limits: PerformanceLimits = field(default_factory=PerformanceLimits)
    validation_settings: ValidationSettings = field(default_factory=ValidationSettings)
    cache_settings: CacheSettings = field(default_factory=CacheSettings)
    logging_settings: LoggingSettings = field(default_factory=LoggingSettings)
    
    # Circuit breaker settings
    enable_circuit_breaker: bool = True
    circuit_breaker_failure_threshold: int = 5
    circuit_breaker_timeout_seconds: int = 30
    
    # Health check settings
    enable_health_checks: bool = True
    health_check_interval_seconds: int = 60
    
    # Advanced configuration features
    configuration_profile: ConfigurationProfile = ConfigurationProfile.PRODUCTION
    validation_level: ValidationLevel = ValidationLevel.NORMAL
    auto_optimize_settings: bool = True
    runtime_config_updates_enabled: bool = False
    
    # Backward-compatibility aliases (accepted as kwargs in tests)
    # If provided, these will be mapped in __post_init__ to the canonical fields above
    defuzzification_method: Optional[DefuzzificationMethod] = None
    aggregation_method: Optional[AggregationMethod] = None
    performance: Optional[PerformanceLimits] = None
    
    def __post_init__(self):
        """Apply intelligent defaults based on configuration profile and init compat wrappers."""
        if self.auto_optimize_settings:
            self._apply_profile_optimizations()
        # Initialize cache compatibility wrapper to expose config.cache
        self._cache_compat = _CacheCompat(self.cache_settings)
        
        # Backward-compatibility: map legacy alias fields to canonical ones if provided
        # Only override if aliases are not None to avoid clobbering explicit canonical values
        if self.defuzzification_method is not None:
            self.default_defuzzification_method = self.defuzzification_method
        if self.aggregation_method is not None:
            self.default_aggregation_method = self.aggregation_method
        if self.performance is not None:
            self.performance_limits = self.performance

    @property
    def cache(self) -> _CacheCompat:
        """Backward-compatibility accessor that mimics historical config.cache usage.
        Provides at least .max_cache_size mapped to cache_settings.max_cache_entries and
        proxies any other attributes to cache_settings.
        """
        # Ensure wrapper always references current cache_settings instance
        if getattr(self, '_cache_compat', None) is None or self._cache_compat._settings is not self.cache_settings:
            self._cache_compat = _CacheCompat(self.cache_settings)
        return self._cache_compat
    
    def _apply_profile_optimizations(self) -> None:
        """Apply optimizations based on the selected configuration profile."""
        if self.configuration_profile == ConfigurationProfile.PRODUCTION:
            # Production optimizations
            self.logging_settings.log_level = "WARNING"
            self.logging_settings.log_membership_calculations = False
            self.logging_settings.log_rule_evaluations = False
            self.validation_settings.strict_range_validation = True
            self.performance_limits.max_execution_time_seconds = 2.0
            self.cache_settings.enable_membership_cache = True
            self.cache_settings.max_cache_entries = 2000
            
        elif self.configuration_profile == ConfigurationProfile.DEVELOPMENT:
            # Development optimizations
            self.logging_settings.log_level = "DEBUG"
            self.logging_settings.log_membership_calculations = True
            self.logging_settings.log_rule_evaluations = True
            self.validation_settings.strict_range_validation = True
            self.performance_limits.max_execution_time_seconds = 10.0
            
        elif self.configuration_profile == ConfigurationProfile.HIGH_PERFORMANCE:
            # High performance optimizations
            self.default_defuzzification_method = DefuzzificationMethod.MOM
            self.default_aggregation_method = AggregationMethod.MAX
            self.validation_settings.strict_range_validation = False
            self.cache_settings.enable_membership_cache = True
            self.cache_settings.max_cache_entries = 5000
            self.performance_limits.max_execution_time_seconds = 0.5
            
        elif self.configuration_profile == ConfigurationProfile.LOW_MEMORY:
            # Low memory optimizations
            self.cache_settings.enable_membership_cache = False
            self.performance_limits.max_memory_usage_mb = 50
            self.performance_limits.max_rules_per_evaluation = 500
            self.default_universe_resolution = 100
    
    def validate(self, validation_level: Optional[ValidationLevel] = None) -> None:
        """Validate all configuration settings with specified validation level."""
        level = validation_level or self.validation_level
        validator = ConfigurationValidator()
        
        # Schema validation for STRICT level
        if level == ValidationLevel.STRICT:
            config_dict = self.to_dict()
            schema_errors = validator.validate_schema(config_dict)
            if schema_errors:
                raise ValidationException(
                    f"Configuration schema validation failed: {'; '.join(schema_errors)}",
                    field_name="configuration",
                    field_value=config_dict
                )
        
        # Validate universe settings
        if self.default_universe_resolution <= 0:
            raise ValidationException(
                "default_universe_resolution must be positive",
                field_name="default_universe_resolution",
                field_value=self.default_universe_resolution
            )
        
        if not (0 < self.universe_expansion_factor < 1):
            raise ValidationException(
                "universe_expansion_factor must be between 0 and 1",
                field_name="universe_expansion_factor",
                field_value=self.universe_expansion_factor
            )
        
        # Validate circuit breaker settings
        if self.circuit_breaker_failure_threshold <= 0:
            raise ValidationException(
                "circuit_breaker_failure_threshold must be positive",
                field_name="circuit_breaker_failure_threshold",
                field_value=self.circuit_breaker_failure_threshold
            )
        
        if self.circuit_breaker_timeout_seconds <= 0:
            raise ValidationException(
                "circuit_breaker_timeout_seconds must be positive",
                field_name="circuit_breaker_timeout_seconds",
                field_value=self.circuit_breaker_timeout_seconds
            )
        
        # Validate component settings
        self.performance_limits.validate()
        self.validation_settings.validate()
        self.cache_settings.validate()
        self.logging_settings.validate()
        
        # Cross-component validation
        dependency_errors = validator.validate_dependencies(self)
        if dependency_errors and level in [ValidationLevel.NORMAL, ValidationLevel.STRICT]:
            raise ValidationException(
                f"Configuration dependency validation failed: {'; '.join(dependency_errors)}",
                field_name="configuration_dependencies",
                field_value=dependency_errors
            )
        
        # Profile consistency validation
        profile_errors = validator.validate_profile_consistency(self, self.configuration_profile)
        if profile_errors and level == ValidationLevel.STRICT:
            raise ValidationException(
                f"Configuration profile consistency validation failed: {'; '.join(profile_errors)}",
                field_name="configuration_profile",
                field_value=profile_errors
            )
    
    def update_runtime_config(self, updates: Dict[str, Any], 
                            validate_updates: bool = True) -> 'FuzzyEngineConfiguration':
        """Update configuration at runtime with validation."""
        if not self.runtime_config_updates_enabled:
            raise ValidationException(
                "Runtime configuration updates are disabled",
                field_name="runtime_config_updates_enabled",
                field_value=False
            )
        
        # Create a copy for validation
        new_config = self.from_dict({**self.to_dict(), **updates})
        
        if validate_updates:
            new_config.validate()
        
        return new_config
    
    def get_optimization_recommendations(self) -> List[str]:
        """Get optimization recommendations based on current configuration."""
        recommendations = []
        
        # Performance recommendations
        if (self.performance_limits.max_execution_time_seconds < 1.0 and 
            self.default_defuzzification_method == DefuzzificationMethod.CENTROID):
            recommendations.append(
                "Consider using faster defuzzification methods (MOM, SOM) for strict time limits"
            )
        
        # Memory recommendations
        if (self.performance_limits.max_memory_usage_mb < 100 and 
            self.cache_settings.max_cache_entries > 1000):
            recommendations.append(
                "Reduce cache size for low-memory environments"
            )
        
        # Accuracy recommendations
        if (self.default_universe_resolution < 500 and 
            self.configuration_profile == ConfigurationProfile.PRODUCTION):
            recommendations.append(
                "Increase universe resolution for better accuracy"
            )
        
        # Logging recommendations
        if (self.logging_settings.log_level == "DEBUG" and 
            self.configuration_profile == ConfigurationProfile.PRODUCTION):
            recommendations.append(
                "Use WARNING or ERROR log level in production environments"
            )
        
        return recommendations
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert configuration to dictionary for logging and serialization."""
        return {
            "default_defuzzification_method": self.default_defuzzification_method.value,
            "default_aggregation_method": self.default_aggregation_method.value,
            "default_logical_operator": self.default_logical_operator.value,
            "default_universe_resolution": self.default_universe_resolution,
            "auto_expand_universe": self.auto_expand_universe,
            "universe_expansion_factor": self.universe_expansion_factor,
            "performance_limits": self.performance_limits.__dict__,
            "validation_settings": self.validation_settings.__dict__,
            "cache_settings": self.cache_settings.__dict__,
            "logging_settings": self.logging_settings.__dict__,
            "enable_circuit_breaker": self.enable_circuit_breaker,
            "circuit_breaker_failure_threshold": self.circuit_breaker_failure_threshold,
            "circuit_breaker_timeout_seconds": self.circuit_breaker_timeout_seconds,
            "enable_health_checks": self.enable_health_checks,
            "health_check_interval_seconds": self.health_check_interval_seconds,
            "configuration_profile": self.configuration_profile.value,
            "validation_level": self.validation_level.value,
            "auto_optimize_settings": self.auto_optimize_settings,
            "runtime_config_updates_enabled": self.runtime_config_updates_enabled
        }
    
    @classmethod
    def from_dict(cls, config_dict: Dict[str, Any]) -> 'FuzzyEngineConfiguration':
        """Create configuration from dictionary with advanced validation."""
        # Validate input dictionary against schema first
        validator = ConfigurationValidator()
        schema_errors = validator.validate_schema(config_dict)
        if schema_errors:
            raise ValidationException(
                f"Input dictionary validation failed: {'; '.join(schema_errors)}",
                field_name="config_dict",
                field_value=config_dict
            )
        
        # Extract nested configurations with enhanced error handling
        try:
            performance_limits = PerformanceLimits(**config_dict.get("performance_limits", {}))
            validation_settings = ValidationSettings(**config_dict.get("validation_settings", {}))
            cache_settings = CacheSettings(**config_dict.get("cache_settings", {}))
            logging_settings = LoggingSettings(**config_dict.get("logging_settings", {}))
        except TypeError as e:
            raise ValidationException(
                f"Failed to create nested configuration objects: {str(e)}",
                field_name="nested_configurations",
                field_value=config_dict
            )
        
        # Convert enum strings back to enums with error handling
        try:
            defuzz_method = DefuzzificationMethod(config_dict.get("default_defuzzification_method", "centroid"))
            agg_method = AggregationMethod(config_dict.get("default_aggregation_method", "max"))
            logical_op = LogicalOperator(config_dict.get("default_logical_operator", "and"))
            config_profile = ConfigurationProfile(config_dict.get("configuration_profile", "production"))
            validation_level = ValidationLevel(config_dict.get("validation_level", "normal"))
        except ValueError as e:
            raise ValidationException(
                f"Invalid enum value in configuration: {str(e)}",
                field_name="enum_values",
                field_value=config_dict
            )
        
        # Create main configuration with all advanced features
        config = cls(
            default_defuzzification_method=defuzz_method,
            default_aggregation_method=agg_method,
            default_logical_operator=logical_op,
            default_universe_resolution=config_dict.get("default_universe_resolution", 200),
            auto_expand_universe=config_dict.get("auto_expand_universe", True),
            universe_expansion_factor=config_dict.get("universe_expansion_factor", 0.1),
            performance_limits=performance_limits,
            validation_settings=validation_settings,
            cache_settings=cache_settings,
            logging_settings=logging_settings,
            enable_circuit_breaker=config_dict.get("enable_circuit_breaker", True),
            circuit_breaker_failure_threshold=config_dict.get("circuit_breaker_failure_threshold", 5),
            circuit_breaker_timeout_seconds=config_dict.get("circuit_breaker_timeout_seconds", 30),
            enable_health_checks=config_dict.get("enable_health_checks", True),
            health_check_interval_seconds=config_dict.get("health_check_interval_seconds", 60),
            configuration_profile=config_profile,
            validation_level=validation_level,
            auto_optimize_settings=config_dict.get("auto_optimize_settings", True),
            runtime_config_updates_enabled=config_dict.get("runtime_config_updates_enabled", False)
        )
        
        # Validate the created configuration
        config.validate()
        
        return config
    
    @classmethod
    def create_for_profile(cls, profile: ConfigurationProfile, 
                          custom_overrides: Optional[Dict[str, Any]] = None) -> 'FuzzyEngineConfiguration':
        """Create optimized configuration for specific profile."""
        base_config = cls(configuration_profile=profile)
        
        if custom_overrides:
            # Apply custom overrides while maintaining profile optimizations
            config_dict = base_config.to_dict()
            config_dict.update(custom_overrides)
            return cls.from_dict(config_dict)
        
        return base_config
    
    @classmethod
    def load_from_file(cls, file_path: str) -> 'FuzzyEngineConfiguration':
        """Load configuration from JSON file with validation."""
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                config_dict = json.load(f)
            return cls.from_dict(config_dict)
        except FileNotFoundError:
            raise ValidationException(
                f"Configuration file not found: {file_path}",
                field_name="file_path",
                field_value=file_path
            )
        except json.JSONDecodeError as e:
            raise ValidationException(
                f"Invalid JSON in configuration file: {str(e)}",
                field_name="json_content",
                field_value=file_path
            )
        except Exception as e:
            raise ValidationException(
                f"Failed to load configuration from file: {str(e)}",
                field_name="file_loading",
                field_value=file_path
            )
    
    def save_to_file(self, file_path: str, validate_before_save: bool = True) -> None:
        """Save configuration to JSON file with validation."""
        if validate_before_save:
            self.validate(ValidationLevel.STRICT)
        
        try:
            with open(file_path, 'w', encoding='utf-8') as f:
                json.dump(self.to_dict(), f, indent=2, ensure_ascii=False)
        except Exception as e:
            raise ValidationException(
                f"Failed to save configuration to file: {str(e)}",
                field_name="file_saving",
                field_value=file_path
            )
    
    def clone(self, **overrides) -> 'FuzzyEngineConfiguration':
        """Create a copy of the configuration with optional overrides."""
        config_dict = self.to_dict()
        config_dict.update(overrides)
        return self.from_dict(config_dict)
    
    def get_performance_profile(self) -> Dict[str, Any]:
        """Get performance characteristics of current configuration."""
        defuzz_ranking = DefuzzificationMethod.get_performance_ranking()
        
        return {
            "defuzzification_performance": defuzz_ranking.get(self.default_defuzzification_method.value, 3),
            "estimated_memory_usage_mb": self._estimate_memory_usage(),
            "estimated_execution_time_factor": self._estimate_execution_time_factor(),
            "cache_efficiency_score": self._calculate_cache_efficiency_score(),
            "validation_overhead_factor": self._calculate_validation_overhead()
        }
    
    def _estimate_memory_usage(self) -> float:
        """Estimate total memory usage based on configuration."""
        base_memory = 10.0  # Base engine memory
        cache_memory = self.cache_settings.cache_memory_limit_mb if self.cache_settings.enable_membership_cache else 0
        universe_memory = (self.default_universe_resolution * 0.001)  # Rough estimate
        
        return base_memory + cache_memory + universe_memory
    
    def _estimate_execution_time_factor(self) -> float:
        """Estimate execution time factor based on configuration."""
        defuzz_factor = DefuzzificationMethod.get_performance_ranking().get(
            self.default_defuzzification_method.value, 3
        ) / 5.0
        
        validation_factor = 1.2 if self.validation_settings.strict_range_validation else 1.0
        universe_factor = self.default_universe_resolution / 1000.0
        
        return defuzz_factor * validation_factor * universe_factor
    
    def _calculate_cache_efficiency_score(self) -> float:
        """Calculate cache efficiency score (0-1)."""
        if not self.cache_settings.enable_membership_cache:
            return 0.0
        
        # Higher cache size and longer TTL = better efficiency
        size_score = min(self.cache_settings.max_cache_entries / 5000.0, 1.0)
        ttl_score = min(self.cache_settings.cache_ttl_seconds / 600.0, 1.0)
        
        return (size_score + ttl_score) / 2.0
    
    def _calculate_validation_overhead(self) -> float:
        """Calculate validation overhead factor."""
        if self.validation_level == ValidationLevel.RELAXED:
            return 1.0
        elif self.validation_level == ValidationLevel.NORMAL:
            return 1.1
        else:  # STRICT
            return 1.3