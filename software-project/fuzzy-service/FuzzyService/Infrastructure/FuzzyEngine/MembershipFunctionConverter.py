"""Membership function converter for scikit-fuzzy integration.

Provides conversion from database membership function definitions to scikit-fuzzy
format with comprehensive validation, caching, and optimization.
"""

from typing import Dict, Any, Tuple, Optional, List
import numpy as np
import skfuzzy as fuzz
from dataclasses import dataclass
from enum import Enum
import logging
import hashlib
import json
from datetime import datetime, timedelta, timezone
from .FuzzyEngineExceptions import MembershipFunctionException, ValidationException
from .FuzzyEngineConfiguration import FuzzyEngineConfiguration
from .FuzzyEngineMetrics import FuzzyEngineMetrics


class MembershipFunctionType(Enum):
    """Supported membership function types."""
    TRIANGULAR = "triangular"
    TRAPEZOIDAL = "trapezoidal"
    GAUSSIAN = "gaussian"
    SIGMOID = "sigmoid"
    BELL = "bell"
    PI = "pi"
    Z = "z"
    S = "s"


@dataclass
class CachedMembershipFunction:
    """Cached membership function with metadata."""
    universe: np.ndarray
    membership_values: np.ndarray
    function_type: str
    parameters: Dict[str, Any]
    created_at: datetime
    access_count: int = 0
    last_accessed: Optional[datetime] = None
    
    def mark_accessed(self) -> None:
        """Mark function as accessed for cache management."""
        self.access_count += 1
        self.last_accessed = datetime.now(timezone.utc)
    
    def is_expired(self, ttl_seconds: int) -> bool:
        """Check if cached function is expired."""
        if not self.last_accessed:
            return False
        return (datetime.now(timezone.utc) - self.last_accessed).total_seconds() > ttl_seconds


class MembershipFunctionConverter:
    """Converts database membership functions to scikit-fuzzy format."""
    
    def __init__(
        self,
        config: FuzzyEngineConfiguration,
        metrics: FuzzyEngineMetrics
    ):
        self.config = config
        self.metrics = metrics
        self.logger = logging.getLogger(__name__)
        self._cache: Dict[str, CachedMembershipFunction] = {}
        self._cache_stats = {"hits": 0, "misses": 0, "evictions": 0}
        
        # Validation settings
        self.min_universe_points = config.validation_settings.min_universe_points
        self.max_universe_points = config.validation_settings.max_universe_points
        self.membership_tolerance = config.validation_settings.membership_tolerance
        
        self.logger.info("MembershipFunctionConverter initialized", extra={
            "cache_enabled": config.cache_settings.enable_membership_cache,
            "max_cache_entries": config.cache_settings.max_cache_entries,
            "universe_resolution": config.default_universe_resolution
        })
    
    def convert_membership_function(
        self,
        function_type: str,
        parameters: Dict[str, Any],
        universe_range: Tuple[float, float],
        resolution: Optional[int] = None
    ) -> Tuple[np.ndarray, np.ndarray]:
        """Convert membership function to scikit-fuzzy format.
        
        Args:
            function_type: Type of membership function
            parameters: Function parameters from database
            universe_range: (min, max) range for universe
            resolution: Number of points in universe (optional)
            
        Returns:
            Tuple of (universe, membership_values)
            
        Raises:
            MembershipFunctionException: If conversion fails
            ValidationException: If parameters are invalid
        """
        with self.metrics.monitor_operation("membership_function_conversion"):
            try:
                # Validate inputs
                self._validate_inputs(function_type, parameters, universe_range, resolution)
                
                # Check cache first
                cache_key = self._generate_cache_key(function_type, parameters, universe_range, resolution)
                if self.config.cache_settings.enable_membership_cache:
                    cached_function = self._get_from_cache(cache_key)
                    if cached_function:
                        self.metrics.record_cache_hit()
                        return cached_function.universe, cached_function.membership_values
                
                self.metrics.record_cache_miss()
                
                # Create universe
                universe = self._create_universe(universe_range, resolution)
                
                # Convert function based on type
                membership_values = self._convert_function_by_type(
                    function_type, parameters, universe
                )
                
                # Validate result
                self._validate_membership_function(universe, membership_values)
                
                # Cache result
                if self.config.cache_settings.enable_membership_cache:
                    self._add_to_cache(cache_key, universe, membership_values, function_type, parameters)
                
                self.logger.debug("Membership function converted successfully", extra={
                    "function_type": function_type,
                    "parameters": parameters,
                    "universe_range": universe_range,
                    "universe_points": len(universe),
                    "membership_range": (float(np.min(membership_values)), float(np.max(membership_values)))
                })
                
                return universe, membership_values
                
            except Exception as e:
                self.logger.error("Failed to convert membership function", extra={
                    "function_type": function_type,
                    "parameters": parameters,
                    "universe_range": universe_range,
                    "error": str(e)
                })
                
                if isinstance(e, (MembershipFunctionException, ValidationException)):
                    raise
                
                raise MembershipFunctionException(
                    f"Unexpected error converting membership function: {str(e)}",
                    function_type=function_type,
                    parameters=parameters,
                    universe_range=universe_range,
                    original_exception=e
                )
    
    def _validate_inputs(
        self,
        function_type: str,
        parameters: Dict[str, Any],
        universe_range: Tuple[float, float],
        resolution: Optional[int]
    ) -> None:
        """Validate input parameters."""
        # Validate function type
        try:
            MembershipFunctionType(function_type)
        except ValueError:
            valid_types = [t.value for t in MembershipFunctionType]
            raise ValidationException(
                f"Invalid function type '{function_type}'. Valid types: {valid_types}",
                field_name="function_type",
                field_value=function_type,
                constraints={"valid_values": valid_types}
            )
        
        # Validate universe range
        if not isinstance(universe_range, (tuple, list)) or len(universe_range) != 2:
            raise ValidationException(
                "universe_range must be a tuple/list of 2 elements",
                field_name="universe_range",
                field_value=universe_range,
                expected_type="tuple[float, float]"
            )
        
        min_val, max_val = universe_range
        if not isinstance(min_val, (int, float)) or not isinstance(max_val, (int, float)):
            raise ValidationException(
                "universe_range values must be numeric",
                field_name="universe_range",
                field_value=universe_range,
                expected_type="tuple[float, float]"
            )
        
        if min_val >= max_val:
            raise ValidationException(
                "universe_range minimum must be less than maximum",
                field_name="universe_range",
                field_value=universe_range,
                constraints={"min < max": True}
            )
        
        # Validate resolution
        if resolution is not None:
            if not isinstance(resolution, int) or resolution <= 0:
                raise ValidationException(
                    "resolution must be a positive integer",
                    field_name="resolution",
                    field_value=resolution,
                    expected_type="int",
                    constraints={"min_value": 1}
                )
            
            if resolution < self.min_universe_points or resolution > self.max_universe_points:
                raise ValidationException(
                    f"resolution must be between {self.min_universe_points} and {self.max_universe_points}",
                    field_name="resolution",
                    field_value=resolution,
                    constraints={
                        "min_value": self.min_universe_points,
                        "max_value": self.max_universe_points
                    }
                )
        
        # Validate parameters
        if not isinstance(parameters, dict):
            raise ValidationException(
                "parameters must be a dictionary",
                field_name="parameters",
                field_value=type(parameters).__name__,
                expected_type="dict"
            )
        
        # Validate function-specific parameters
        self._validate_function_parameters(function_type, parameters)
    
    def _validate_function_parameters(self, function_type: str, parameters: Dict[str, Any]) -> None:
        """Validate parameters for specific function types."""
        func_type = MembershipFunctionType(function_type)
        
        if func_type == MembershipFunctionType.TRIANGULAR:
            required_params = ['a', 'b', 'c']
            self._check_required_params(parameters, required_params, function_type)
            a, b, c = parameters['a'], parameters['b'], parameters['c']
            if not (a <= b <= c):
                raise ValidationException(
                    "Triangular function requires a <= b <= c",
                    field_name="parameters",
                    field_value=parameters,
                    constraints={"order": "a <= b <= c"}
                )
        
        elif func_type == MembershipFunctionType.TRAPEZOIDAL:
            required_params = ['a', 'b', 'c', 'd']
            self._check_required_params(parameters, required_params, function_type)
            a, b, c, d = parameters['a'], parameters['b'], parameters['c'], parameters['d']
            if not (a <= b <= c <= d):
                raise ValidationException(
                    "Trapezoidal function requires a <= b <= c <= d",
                    field_name="parameters",
                    field_value=parameters,
                    constraints={"order": "a <= b <= c <= d"}
                )
        
        elif func_type == MembershipFunctionType.GAUSSIAN:
            required_params = ['mean', 'sigma']
            self._check_required_params(parameters, required_params, function_type)
            if parameters['sigma'] <= 0:
                raise ValidationException(
                    "Gaussian function requires sigma > 0",
                    field_name="sigma",
                    field_value=parameters['sigma'],
                    constraints={"min_value": 0, "exclusive": True}
                )
        
        elif func_type == MembershipFunctionType.SIGMOID:
            required_params = ['a', 'c']
            self._check_required_params(parameters, required_params, function_type)
        
        elif func_type == MembershipFunctionType.BELL:
            required_params = ['a', 'b', 'c']
            self._check_required_params(parameters, required_params, function_type)
            if parameters['a'] <= 0 or parameters['b'] <= 0:
                raise ValidationException(
                    "Bell function requires a > 0 and b > 0",
                    field_name="parameters",
                    field_value=parameters,
                    constraints={"a > 0": True, "b > 0": True}
                )
        
        elif func_type in [MembershipFunctionType.PI, MembershipFunctionType.Z, MembershipFunctionType.S]:
            required_params = ['a', 'b']
            self._check_required_params(parameters, required_params, function_type)
            if parameters['a'] >= parameters['b']:
                raise ValidationException(
                    f"{function_type} function requires a < b",
                    field_name="parameters",
                    field_value=parameters,
                    constraints={"a < b": True}
                )
    
    def _check_required_params(self, parameters: Dict[str, Any], required: List[str], function_type: str) -> None:
        """Check if all required parameters are present."""
        missing = [param for param in required if param not in parameters]
        if missing:
            raise ValidationException(
                f"{function_type} function missing required parameters: {missing}",
                field_name="parameters",
                field_value=parameters,
                constraints={"required_params": required}
            )
        
        # Check if all required parameters are numeric
        for param in required:
            if not isinstance(parameters[param], (int, float)):
                raise ValidationException(
                    f"Parameter '{param}' must be numeric",
                    field_name=param,
                    field_value=parameters[param],
                    expected_type="float"
                )
    
    def _create_universe(self, universe_range: Tuple[float, float], resolution: Optional[int]) -> np.ndarray:
        """Create universe array."""
        if resolution is None:
            resolution = self.config.default_universe_resolution
        
        min_val, max_val = universe_range
        
        # Auto-expand universe if enabled
        if self.config.auto_expand_universe:
            range_size = max_val - min_val
            expansion = range_size * self.config.universe_expansion_factor
            min_val -= expansion
            max_val += expansion
        
        universe = np.linspace(min_val, max_val, resolution)
        
        self.logger.debug("Universe created", extra={
            "original_range": universe_range,
            "expanded_range": (min_val, max_val),
            "resolution": resolution,
            "auto_expand": self.config.auto_expand_universe
        })
        
        return universe
    
    def _convert_function_by_type(
        self,
        function_type: str,
        parameters: Dict[str, Any],
        universe: np.ndarray
    ) -> np.ndarray:
        """Convert function based on its type."""
        func_type = MembershipFunctionType(function_type)
        
        try:
            if func_type == MembershipFunctionType.TRIANGULAR:
                return fuzz.trimf(universe, [parameters['a'], parameters['b'], parameters['c']])
            
            elif func_type == MembershipFunctionType.TRAPEZOIDAL:
                return fuzz.trapmf(universe, [parameters['a'], parameters['b'], parameters['c'], parameters['d']])
            
            elif func_type == MembershipFunctionType.GAUSSIAN:
                return fuzz.gaussmf(universe, parameters['mean'], parameters['sigma'])
            
            elif func_type == MembershipFunctionType.SIGMOID:
                return fuzz.sigmf(universe, parameters['a'], parameters['c'])
            
            elif func_type == MembershipFunctionType.BELL:
                return fuzz.gbellmf(universe, parameters['a'], parameters['b'], parameters['c'])
            
            elif func_type == MembershipFunctionType.PI:
                return fuzz.pimf(universe, parameters['a'], parameters['b'], parameters['c'], parameters['d'])
            
            elif func_type == MembershipFunctionType.Z:
                return fuzz.zmf(universe, parameters['a'], parameters['b'])
            
            elif func_type == MembershipFunctionType.S:
                return fuzz.smf(universe, parameters['a'], parameters['b'])
            
            else:
                raise MembershipFunctionException(
                    f"Unsupported function type: {function_type}",
                    function_type=function_type,
                    parameters=parameters
                )
        
        except Exception as e:
            if isinstance(e, MembershipFunctionException):
                raise
            
            raise MembershipFunctionException(
                f"Error creating {function_type} membership function: {str(e)}",
                function_type=function_type,
                parameters=parameters,
                original_exception=e
            )
    
    def _validate_membership_function(self, universe: np.ndarray, membership_values: np.ndarray) -> None:
        """Validate the generated membership function."""
        # Check for NaN or infinite values
        if np.any(np.isnan(membership_values)) or np.any(np.isinf(membership_values)):
            raise MembershipFunctionException(
                "Membership function contains NaN or infinite values",
                universe_range=(float(np.min(universe)), float(np.max(universe)))
            )
        
        # Check membership range
        min_membership = np.min(membership_values)
        max_membership = np.max(membership_values)
        
        if self.config.validation_settings.require_normalized_membership:
            if min_membership < -self.membership_tolerance or max_membership > 1 + self.membership_tolerance:
                raise MembershipFunctionException(
                    f"Membership values must be in [0, 1], got [{min_membership:.6f}, {max_membership:.6f}]",
                    universe_range=(float(np.min(universe)), float(np.max(universe)))
                )
        
        # Check for zero membership if not allowed
        if not self.config.validation_settings.allow_zero_membership:
            if np.all(membership_values <= self.membership_tolerance):
                raise MembershipFunctionException(
                    "Membership function has all zero values",
                    universe_range=(float(np.min(universe)), float(np.max(universe)))
                )
        
        # Check universe continuity
        if self.config.validation_settings.validate_universe_continuity:
            universe_diff = np.diff(universe)
            if not np.allclose(universe_diff, universe_diff[0], rtol=1e-10):
                raise MembershipFunctionException(
                    "Universe is not uniformly spaced",
                    universe_range=(float(np.min(universe)), float(np.max(universe)))
                )
    
    def _generate_cache_key(
        self,
        function_type: str,
        parameters: Dict[str, Any],
        universe_range: Tuple[float, float],
        resolution: Optional[int]
    ) -> str:
        """Generate cache key for membership function."""
        cache_data = {
            "function_type": function_type,
            "parameters": parameters,
            "universe_range": universe_range,
            "resolution": resolution or self.config.default_universe_resolution,
            "auto_expand": self.config.auto_expand_universe,
            "expansion_factor": self.config.universe_expansion_factor
        }
        
        cache_string = json.dumps(cache_data, sort_keys=True)
        return hashlib.md5(cache_string.encode()).hexdigest()
    
    def _get_from_cache(self, cache_key: str) -> Optional[CachedMembershipFunction]:
        """Get membership function from cache."""
        if cache_key not in self._cache:
            return None
        
        cached_function = self._cache[cache_key]
        
        # Check if expired
        if cached_function.is_expired(self.config.cache_settings.cache_ttl_seconds):
            del self._cache[cache_key]
            self.metrics.record_cache_eviction()
            return None
        
        cached_function.mark_accessed()
        return cached_function
    
    def _add_to_cache(
        self,
        cache_key: str,
        universe: np.ndarray,
        membership_values: np.ndarray,
        function_type: str,
        parameters: Dict[str, Any]
    ) -> None:
        """Add membership function to cache."""
        # Check cache size limit
        if len(self._cache) >= self.config.cache_settings.max_cache_entries:
            self._evict_least_recently_used()
        
        cached_function = CachedMembershipFunction(
            universe=universe.copy(),
            membership_values=membership_values.copy(),
            function_type=function_type,
            parameters=parameters.copy(),
            created_at=datetime.now(timezone.utc)
        )
        
        self._cache[cache_key] = cached_function
        
        # Update cache metrics
        cache_memory_mb = self._estimate_cache_memory_usage()
        self.metrics.update_cache_size(len(self._cache), cache_memory_mb)
    
    def _evict_least_recently_used(self) -> None:
        """Evict least recently used cache entry."""
        if not self._cache:
            return
        
        # Find least recently used entry
        lru_key = min(
            self._cache.keys(),
            key=lambda k: self._cache[k].last_accessed or self._cache[k].created_at
        )
        
        del self._cache[lru_key]
        self.metrics.record_cache_eviction()
        
        self.logger.debug("Cache entry evicted", extra={"cache_key": lru_key})
    
    def _estimate_cache_memory_usage(self) -> float:
        """Estimate cache memory usage in MB."""
        total_bytes = 0
        
        for cached_function in self._cache.values():
            # Estimate numpy array sizes
            total_bytes += cached_function.universe.nbytes
            total_bytes += cached_function.membership_values.nbytes
            
            # Estimate other data (rough approximation)
            total_bytes += 1024  # Metadata overhead
        
        return total_bytes / (1024 * 1024)  # Convert to MB
    
    def clear_cache(self) -> None:
        """Clear all cached membership functions."""
        self._cache.clear()
        self.metrics.update_cache_size(0, 0.0)
        self.logger.info("Membership function cache cleared")
    
    def get_cache_stats(self) -> Dict[str, Any]:
        """Get cache statistics."""
        return {
            "cache_size": len(self._cache),
            "cache_memory_mb": self._estimate_cache_memory_usage(),
            "cache_hit_rate": self.metrics.cache_metrics.hit_rate,
            "cache_miss_rate": self.metrics.cache_metrics.miss_rate,
            "total_evictions": self.metrics.cache_metrics.cache_evictions
        }