#!/usr/bin/env python3
"""
Circuit Breaker Pattern Implementation for FuzzyEngine

This module provides a comprehensive circuit breaker implementation to handle
failures and provide fault tolerance for FuzzyEngine components.

Features:
- Multiple circuit breaker states (CLOSED, OPEN, HALF_OPEN)
- Configurable failure thresholds and timeouts
- Automatic recovery mechanisms
- Detailed metrics and monitoring
- Component-specific circuit breakers
- Fallback mechanisms
- Health check integration
- Performance monitoring

Author: FuzzyEngine Development Team
Date: 2024
Version: 1.0.0
"""

import time
import threading
import logging
from typing import Dict, Any, Optional, Callable, List, Union, TypeVar, Generic
from enum import Enum
from dataclasses import dataclass, field
from datetime import datetime, timezone, timedelta
from collections import deque
import statistics
from contextlib import contextmanager
from functools import wraps
import asyncio
from concurrent.futures import ThreadPoolExecutor, Future

from .FuzzyEngineExceptions import (
    FuzzyEngineException,
    ValidationException,
    PerformanceException
)
from .FuzzyEngineMetrics import FuzzyEngineMetrics


class CircuitBreakerState(Enum):
    """Circuit breaker states."""
    CLOSED = "CLOSED"      # Normal operation
    OPEN = "OPEN"          # Failing fast
    HALF_OPEN = "HALF_OPEN" # Testing recovery


class FailureType(Enum):
    """Types of failures that can trigger circuit breaker."""
    TIMEOUT = "TIMEOUT"
    EXCEPTION = "EXCEPTION"
    VALIDATION_ERROR = "VALIDATION_ERROR"
    PERFORMANCE_DEGRADATION = "PERFORMANCE_DEGRADATION"
    RESOURCE_EXHAUSTION = "RESOURCE_EXHAUSTION"
    DEPENDENCY_FAILURE = "DEPENDENCY_FAILURE"


class RecoveryStrategy(Enum):
    """Recovery strategies for circuit breaker."""
    IMMEDIATE = "IMMEDIATE"           # Try recovery immediately
    EXPONENTIAL_BACKOFF = "EXPONENTIAL_BACKOFF"  # Exponential backoff
    LINEAR_BACKOFF = "LINEAR_BACKOFF"   # Linear backoff
    FIXED_INTERVAL = "FIXED_INTERVAL"   # Fixed interval
    ADAPTIVE = "ADAPTIVE"               # Adaptive based on failure patterns


@dataclass
class CircuitBreakerConfig:
    """Configuration for circuit breaker."""
    failure_threshold: int = 5
    timeout_seconds: float = 30.0
    recovery_timeout_seconds: float = 60.0
    half_open_max_calls: int = 3
    success_threshold: int = 2
    failure_rate_threshold: float = 0.5  # 50% failure rate
    slow_call_duration_threshold: float = 5.0  # seconds
    slow_call_rate_threshold: float = 0.5  # 50% slow calls
    minimum_number_of_calls: int = 10
    sliding_window_size: int = 100
    recovery_strategy: RecoveryStrategy = RecoveryStrategy.EXPONENTIAL_BACKOFF
    max_recovery_attempts: int = 5
    enable_fallback: bool = True
    enable_metrics: bool = True
    
    def validate(self) -> None:
        """Validate configuration parameters."""
        if self.failure_threshold <= 0:
            raise ValidationException("failure_threshold must be positive")
        if self.timeout_seconds <= 0:
            raise ValidationException("timeout_seconds must be positive")
        if self.recovery_timeout_seconds <= 0:
            raise ValidationException("recovery_timeout_seconds must be positive")
        if not 0 < self.failure_rate_threshold <= 1:
            raise ValidationException("failure_rate_threshold must be between 0 and 1")
        if not 0 < self.slow_call_rate_threshold <= 1:
            raise ValidationException("slow_call_rate_threshold must be between 0 and 1")


@dataclass
class CallResult:
    """Result of a circuit breaker call."""
    success: bool
    duration: float
    timestamp: datetime
    failure_type: Optional[FailureType] = None
    exception: Optional[Exception] = None
    slow_call_threshold: float = 5.0  # Default threshold
    
    @property
    def is_slow(self) -> bool:
        """Check if call was slow based on duration."""
        return self.duration > self.slow_call_threshold


@dataclass
class CircuitBreakerMetrics:
    """Metrics for circuit breaker monitoring."""
    total_calls: int = 0
    successful_calls: int = 0
    failed_calls: int = 0
    slow_calls: int = 0
    circuit_breaker_opens: int = 0
    circuit_breaker_closes: int = 0
    fallback_calls: int = 0
    average_response_time: float = 0.0
    failure_rate: float = 0.0
    slow_call_rate: float = 0.0
    current_state: CircuitBreakerState = CircuitBreakerState.CLOSED
    last_state_change: Optional[datetime] = None
    recovery_attempts: int = 0
    
    def update_rates(self, call_history: deque) -> None:
        """Update failure and slow call rates based on call history."""
        if not call_history:
            return
            
        total = len(call_history)
        failed = sum(1 for call in call_history if not call.success)
        slow = sum(1 for call in call_history if call.is_slow)
        
        self.failure_rate = failed / total if total > 0 else 0.0
        self.slow_call_rate = slow / total if total > 0 else 0.0
        
        # Update average response time
        durations = [call.duration for call in call_history]
        self.average_response_time = statistics.mean(durations) if durations else 0.0


T = TypeVar('T')


class CircuitBreaker(Generic[T]):
    """Circuit breaker implementation for fault tolerance."""
    
    def __init__(
        self,
        name: str,
        config: CircuitBreakerConfig,
        fallback_function: Optional[Callable[..., T]] = None,
        metrics: Optional[FuzzyEngineMetrics] = None
    ):
        self.name = name
        self.config = config
        self.fallback_function = fallback_function
        self.metrics = metrics
        
        # Validate configuration
        self.config.validate()
        
        # State management
        self._state = CircuitBreakerState.CLOSED
        self._failure_count = 0
        self._success_count = 0
        self._last_failure_time: Optional[datetime] = None
        self._next_attempt_time: Optional[datetime] = None
        self._recovery_attempts = 0
        
        # Call history for sliding window
        self._call_history: deque = deque(maxlen=config.sliding_window_size)
        
        # Metrics
        self._circuit_metrics = CircuitBreakerMetrics()
        
        # Thread safety
        self._lock = threading.RLock()
        
        # Logger
        self.logger = logging.getLogger(f"CircuitBreaker.{name}")
        
        self.logger.info(f"Circuit breaker '{name}' initialized with config: {config}")
    
    @property
    def state(self) -> CircuitBreakerState:
        """Get current circuit breaker state."""
        with self._lock:
            return self._state
    
    @property
    def is_closed(self) -> bool:
        """Check if circuit breaker is closed (normal operation)."""
        return self.state == CircuitBreakerState.CLOSED
    
    @property
    def is_open(self) -> bool:
        """Check if circuit breaker is open (failing fast)."""
        return self.state == CircuitBreakerState.OPEN
    
    @property
    def is_half_open(self) -> bool:
        """Check if circuit breaker is half-open (testing recovery)."""
        return self.state == CircuitBreakerState.HALF_OPEN
    
    def call(self, func: Callable[..., T], *args, **kwargs) -> T:
        """Execute function with circuit breaker protection."""
        with self._lock:
            # Check if we can make the call
            if not self._can_execute():
                self._circuit_metrics.fallback_calls += 1
                if self.fallback_function:
                    self.logger.warning(f"Circuit breaker '{self.name}' is open, using fallback")
                    return self.fallback_function(*args, **kwargs)
                else:
                    raise CircuitBreakerOpenException(
                        f"Circuit breaker '{self.name}' is open and no fallback available"
                    )
            
            # Execute the function
            start_time = time.time()
            call_result = None
            
            try:
                result = func(*args, **kwargs)
                duration = time.time() - start_time
                
                call_result = CallResult(
                    success=True,
                    duration=duration,
                    timestamp=datetime.now(timezone.utc),
                    slow_call_threshold=self.config.slow_call_duration_threshold
                )
                
                self._record_call(call_result)
                self._on_success(call_result)
                return result
                
            except Exception as e:
                duration = time.time() - start_time
                failure_type = self._classify_failure(e)
                
                call_result = CallResult(
                    success=False,
                    duration=duration,
                    timestamp=datetime.now(timezone.utc),
                    failure_type=failure_type,
                    exception=e,
                    slow_call_threshold=self.config.slow_call_duration_threshold
                )
                
                self._record_call(call_result)
                self._on_failure(call_result)
                raise
    
    async def call_async(self, func: Callable[..., T], *args, **kwargs) -> T:
        """Execute async function with circuit breaker protection."""
        with self._lock:
            if not self._can_execute():
                self._circuit_metrics.fallback_calls += 1
                if self.fallback_function:
                    self.logger.warning(f"Circuit breaker '{self.name}' is open, using fallback")
                    if asyncio.iscoroutinefunction(self.fallback_function):
                        return await self.fallback_function(*args, **kwargs)
                    else:
                        return self.fallback_function(*args, **kwargs)
                else:
                    raise CircuitBreakerOpenException(
                        f"Circuit breaker '{self.name}' is open and no fallback available"
                    )
        
        start_time = time.time()
        call_result = None
        
        try:
            result = await func(*args, **kwargs)
            duration = time.time() - start_time
            
            call_result = CallResult(
                success=True,
                duration=duration,
                timestamp=datetime.now(timezone.utc),
                slow_call_threshold=self.config.slow_call_duration_threshold
            )
            
            with self._lock:
                self._record_call(call_result)
                self._on_success(call_result)
            return result
            
        except Exception as e:
            duration = time.time() - start_time
            failure_type = self._classify_failure(e)
            
            call_result = CallResult(
                success=False,
                duration=duration,
                timestamp=datetime.now(timezone.utc),
                failure_type=failure_type,
                exception=e,
                slow_call_threshold=self.config.slow_call_duration_threshold
            )
            
            with self._lock:
                self._record_call(call_result)
                self._on_failure(call_result)
            raise
    
    def _can_execute(self) -> bool:
        """Check if function can be executed based on circuit breaker state."""
        current_time = datetime.now(timezone.utc)
        
        if self._state == CircuitBreakerState.CLOSED:
            return True
        
        elif self._state == CircuitBreakerState.OPEN:
            # Check if we should transition to half-open
            if (self._next_attempt_time and 
                current_time >= self._next_attempt_time):
                self._transition_to_half_open()
                return True
            return False
        
        elif self._state == CircuitBreakerState.HALF_OPEN:
            # Allow limited calls in half-open state
            # Count calls since entering half-open state
            if self._circuit_metrics.last_state_change:
                calls_since_half_open = sum(
                    1 for call in self._call_history 
                    if call.timestamp >= self._circuit_metrics.last_state_change
                )
                return calls_since_half_open < self.config.half_open_max_calls
            else:
                # If no state change timestamp, allow the call
                return True
        
        return False
    
    def _on_success(self, call_result: CallResult) -> None:
        """Handle successful call."""
        self._circuit_metrics.successful_calls += 1
        
        if self._state == CircuitBreakerState.HALF_OPEN:
            self._success_count += 1
            if self._success_count >= self.config.success_threshold:
                self._transition_to_closed()
                return  # Exit early after transition to closed
        
        # Check for slow calls using CallResult's is_slow property
        if call_result.is_slow:
            self._circuit_metrics.slow_calls += 1
            
        # Check if we should open due to slow call rate (only in CLOSED state)
        if self._state == CircuitBreakerState.CLOSED:
            if self._should_open():
                self._transition_to_open()
    
    def _on_failure(self, call_result: CallResult) -> None:
        """Handle failed call."""
        self._circuit_metrics.failed_calls += 1
        self._failure_count += 1
        self._last_failure_time = call_result.timestamp
        
        if self._state == CircuitBreakerState.HALF_OPEN:
            # Any failure in half-open state transitions back to open
            self._transition_to_open()
        elif self._state == CircuitBreakerState.CLOSED:
            # Check if we should transition to open
            if self._should_open():
                self._transition_to_open()
    
    def _should_open(self) -> bool:
        """Determine if circuit breaker should open."""
        # Check failure count threshold
        if self._failure_count >= self.config.failure_threshold:
            return True
        
        # Check failure rate if we have enough calls
        if len(self._call_history) >= self.config.minimum_number_of_calls:
            self._circuit_metrics.update_rates(self._call_history)
            
            # Check failure rate threshold
            if self._circuit_metrics.failure_rate >= self.config.failure_rate_threshold:
                return True
            
            # Check slow call rate threshold
            if self._circuit_metrics.slow_call_rate >= self.config.slow_call_rate_threshold:
                return True
        
        return False
    
    def _transition_to_open(self) -> None:
        """Transition circuit breaker to open state."""
        self._state = CircuitBreakerState.OPEN
        self._circuit_metrics.circuit_breaker_opens += 1
        self._circuit_metrics.current_state = CircuitBreakerState.OPEN
        self._circuit_metrics.last_state_change = datetime.now(timezone.utc)
        
        # Calculate next attempt time based on recovery strategy
        self._next_attempt_time = self._calculate_next_attempt_time()
        
        self.logger.warning(
            f"Circuit breaker '{self.name}' opened. "
            f"Failure count: {self._failure_count}, "
            f"Failure rate: {self._circuit_metrics.failure_rate:.2%}, "
            f"Next attempt at: {self._next_attempt_time}"
        )
        
        # Record metrics
        if self.metrics:
            self.metrics.record_circuit_breaker_open(self.name)
    
    def _transition_to_half_open(self) -> None:
        """Transition circuit breaker to half-open state."""
        self._state = CircuitBreakerState.HALF_OPEN
        self._success_count = 0
        self._recovery_attempts += 1
        self._circuit_metrics.recovery_attempts += 1
        self._circuit_metrics.current_state = CircuitBreakerState.HALF_OPEN
        self._circuit_metrics.last_state_change = datetime.now(timezone.utc)
        
        self.logger.info(
            f"Circuit breaker '{self.name}' transitioned to half-open. "
            f"Recovery attempt: {self._recovery_attempts}"
        )
    
    def _transition_to_closed(self) -> None:
        """Transition circuit breaker to closed state."""
        self._state = CircuitBreakerState.CLOSED
        self._failure_count = 0
        self._success_count = 0
        self._recovery_attempts = 0
        self._last_failure_time = None
        self._next_attempt_time = None
        self._circuit_metrics.circuit_breaker_closes += 1
        self._circuit_metrics.current_state = CircuitBreakerState.CLOSED
        self._circuit_metrics.last_state_change = datetime.now(timezone.utc)
        
        self.logger.info(f"Circuit breaker '{self.name}' closed (recovered)")
        
        # Record metrics
        if self.metrics:
            self.metrics.record_circuit_breaker_close(self.name)
    
    def _calculate_next_attempt_time(self) -> datetime:
        """Calculate next attempt time based on recovery strategy."""
        base_timeout = self.config.recovery_timeout_seconds
        current_time = datetime.now(timezone.utc)
        
        if self.config.recovery_strategy == RecoveryStrategy.IMMEDIATE:
            return current_time
        
        elif self.config.recovery_strategy == RecoveryStrategy.FIXED_INTERVAL:
            return current_time + timedelta(seconds=base_timeout)
        
        elif self.config.recovery_strategy == RecoveryStrategy.LINEAR_BACKOFF:
            timeout = base_timeout * (1 + self._recovery_attempts)
            return current_time + timedelta(seconds=timeout)
        
        elif self.config.recovery_strategy == RecoveryStrategy.EXPONENTIAL_BACKOFF:
            timeout = base_timeout * (2 ** min(self._recovery_attempts, 10))
            return current_time + timedelta(seconds=timeout)
        
        elif self.config.recovery_strategy == RecoveryStrategy.ADAPTIVE:
            # Adaptive strategy based on failure patterns
            if self._circuit_metrics.failure_rate > 0.8:
                timeout = base_timeout * 4  # High failure rate, wait longer
            elif self._circuit_metrics.failure_rate > 0.6:
                timeout = base_timeout * 2
            else:
                timeout = base_timeout
            return current_time + timedelta(seconds=timeout)
        
        else:
            return current_time + timedelta(seconds=base_timeout)
    
    def _classify_failure(self, exception: Exception) -> FailureType:
        """Classify the type of failure."""
        if isinstance(exception, (TimeoutError, asyncio.TimeoutError)) or "timeout" in str(exception).lower():
            return FailureType.TIMEOUT
        elif isinstance(exception, ValidationException):
            return FailureType.VALIDATION_ERROR
        elif isinstance(exception, PerformanceException):
            return FailureType.PERFORMANCE_DEGRADATION
        elif isinstance(exception, (MemoryError, OSError)):
            return FailureType.RESOURCE_EXHAUSTION
        else:
            return FailureType.EXCEPTION
    
    def _record_call(self, call_result: CallResult) -> None:
        """Record call result in history."""
        self._call_history.append(call_result)
        self._circuit_metrics.total_calls += 1
        
        # Update metrics
        if len(self._call_history) >= self.config.minimum_number_of_calls:
            self._circuit_metrics.update_rates(self._call_history)
        
        # Record in FuzzyEngine metrics if available
        if self.metrics:
            if call_result.success:
                self.metrics.record_circuit_breaker_success(self.name)
            else:
                self.metrics.record_circuit_breaker_failure(self.name, str(call_result.failure_type))
    
    def reset(self) -> None:
        """Manually reset circuit breaker to closed state."""
        with self._lock:
            self.logger.info(f"Manually resetting circuit breaker '{self.name}'")
            self._transition_to_closed()
    
    def force_open(self) -> None:
        """Manually force circuit breaker to open state."""
        with self._lock:
            self.logger.warning(f"Manually forcing circuit breaker '{self.name}' to open")
            self._transition_to_open()
    
    def get_metrics(self) -> CircuitBreakerMetrics:
        """Get current circuit breaker metrics."""
        with self._lock:
            # Update rates before returning metrics
            if len(self._call_history) >= self.config.minimum_number_of_calls:
                self._circuit_metrics.update_rates(self._call_history)
            return self._circuit_metrics
    
    def get_health_status(self) -> Dict[str, Any]:
        """Get health status of circuit breaker."""
        metrics = self.get_metrics()
        
        return {
            "name": self.name,
            "state": self.state.value,
            "healthy": self.state != CircuitBreakerState.OPEN,
            "failure_rate": metrics.failure_rate,
            "slow_call_rate": metrics.slow_call_rate,
            "total_calls": metrics.total_calls,
            "successful_calls": metrics.successful_calls,
            "failed_calls": metrics.failed_calls,
            "recovery_attempts": metrics.recovery_attempts,
            "last_state_change": metrics.last_state_change.isoformat() if metrics.last_state_change else None,
            "next_attempt_time": self._next_attempt_time.isoformat() if self._next_attempt_time else None
        }


class CircuitBreakerOpenException(FuzzyEngineException):
    """Exception raised when circuit breaker is open."""
    
    def __init__(self, message: str, circuit_breaker_name: str = ""):
        super().__init__(message, error_code="CIRCUIT_BREAKER_OPEN")
        self.circuit_breaker_name = circuit_breaker_name


class CircuitBreakerManager:
    """Manager for multiple circuit breakers."""
    
    def __init__(self, metrics: Optional[FuzzyEngineMetrics] = None):
        self.metrics = metrics
        self._circuit_breakers: Dict[str, CircuitBreaker] = {}
        self._lock = threading.RLock()
        self.logger = logging.getLogger("CircuitBreakerManager")
    
    def create_circuit_breaker(
        self,
        name: str,
        config: CircuitBreakerConfig,
        fallback_function: Optional[Callable] = None
    ) -> CircuitBreaker:
        """Create and register a new circuit breaker."""
        with self._lock:
            if name in self._circuit_breakers:
                raise ValueError(f"Circuit breaker '{name}' already exists")
            
            circuit_breaker = CircuitBreaker(
                name=name,
                config=config,
                fallback_function=fallback_function,
                metrics=self.metrics
            )
            
            self._circuit_breakers[name] = circuit_breaker
            self.logger.info(f"Created circuit breaker '{name}'")
            
            return circuit_breaker
    
    def get_circuit_breaker(self, name: str) -> Optional[CircuitBreaker]:
        """Get circuit breaker by name."""
        with self._lock:
            return self._circuit_breakers.get(name)
    
    def remove_circuit_breaker(self, name: str) -> bool:
        """Remove circuit breaker by name."""
        with self._lock:
            if name in self._circuit_breakers:
                del self._circuit_breakers[name]
                self.logger.info(f"Removed circuit breaker '{name}'")
                return True
            return False
    
    def reset_all(self) -> None:
        """Reset all circuit breakers."""
        with self._lock:
            for cb in self._circuit_breakers.values():
                cb.reset()
            self.logger.info("Reset all circuit breakers")
    
    def get_all_metrics(self) -> Dict[str, CircuitBreakerMetrics]:
        """Get metrics for all circuit breakers."""
        with self._lock:
            return {
                name: cb.get_metrics() 
                for name, cb in self._circuit_breakers.items()
            }
    
    def get_health_status(self) -> Dict[str, Any]:
        """Get health status of all circuit breakers."""
        with self._lock:
            circuit_breakers_status = {
                name: cb.get_health_status()
                for name, cb in self._circuit_breakers.items()
            }
            
            # Calculate overall health
            total_breakers = len(self._circuit_breakers)
            healthy_breakers = sum(
                1 for status in circuit_breakers_status.values()
                if status["healthy"]
            )
            
            return {
                "total_circuit_breakers": total_breakers,
                "healthy_circuit_breakers": healthy_breakers,
                "overall_health": healthy_breakers / total_breakers if total_breakers > 0 else 1.0,
                "circuit_breakers": circuit_breakers_status
            }


def circuit_breaker(
    name: str,
    config: Optional[CircuitBreakerConfig] = None,
    fallback_function: Optional[Callable] = None,
    manager: Optional[CircuitBreakerManager] = None
):
    """Decorator for applying circuit breaker to functions."""
    if config is None:
        config = CircuitBreakerConfig()
    
    if manager is None:
        manager = CircuitBreakerManager()
    
    def decorator(func: Callable[..., T]) -> Callable[..., T]:
        # Create circuit breaker for this function
        cb = manager.create_circuit_breaker(
            name=name or func.__name__,
            config=config,
            fallback_function=fallback_function
        )
        
        @wraps(func)
        def wrapper(*args, **kwargs) -> T:
            return cb.call(func, *args, **kwargs)
        
        @wraps(func)
        async def async_wrapper(*args, **kwargs) -> T:
            return await cb.call_async(func, *args, **kwargs)
        
        # Return appropriate wrapper based on function type
        if asyncio.iscoroutinefunction(func):
            return async_wrapper
        else:
            return wrapper
    
    return decorator


# Utility functions for creating common circuit breaker configurations

def create_fast_fail_config() -> CircuitBreakerConfig:
    """Create configuration for fast-failing circuit breaker."""
    return CircuitBreakerConfig(
        failure_threshold=3,
        timeout_seconds=10.0,
        recovery_timeout_seconds=30.0,
        failure_rate_threshold=0.3,
        recovery_strategy=RecoveryStrategy.EXPONENTIAL_BACKOFF
    )


def create_resilient_config() -> CircuitBreakerConfig:
    """Create configuration for resilient circuit breaker."""
    return CircuitBreakerConfig(
        failure_threshold=10,
        timeout_seconds=60.0,
        recovery_timeout_seconds=120.0,
        failure_rate_threshold=0.7,
        recovery_strategy=RecoveryStrategy.ADAPTIVE,
        max_recovery_attempts=10
    )


def create_performance_config() -> CircuitBreakerConfig:
    """Create configuration for performance-sensitive circuit breaker."""
    return CircuitBreakerConfig(
        failure_threshold=5,
        timeout_seconds=30.0,
        recovery_timeout_seconds=60.0,
        slow_call_duration_threshold=2.0,
        slow_call_rate_threshold=0.3,
        failure_rate_threshold=0.5,
        recovery_strategy=RecoveryStrategy.LINEAR_BACKOFF
    )