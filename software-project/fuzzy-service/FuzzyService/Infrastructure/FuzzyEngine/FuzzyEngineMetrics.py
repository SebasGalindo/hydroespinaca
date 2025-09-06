"""Performance metrics and monitoring for FuzzyEngine operations.

Provides comprehensive metrics collection, performance monitoring,
and alerting capabilities for fuzzy logic operations.
"""

from dataclasses import dataclass, field
from typing import Dict, Any, Optional, List, Callable
from datetime import datetime, timezone, timedelta
from collections import defaultdict, deque
import time
import threading
import statistics
import logging
from .FuzzyEngineExceptions import PerformanceException


@dataclass
class OperationMetrics:
    """Metrics for a specific operation."""
    operation_name: str
    total_executions: int = 0
    total_execution_time: float = 0.0
    min_execution_time: float = float('inf')
    max_execution_time: float = 0.0
    error_count: int = 0
    last_execution_time: Optional[datetime] = None
    recent_execution_times: deque = field(default_factory=lambda: deque(maxlen=100))
    
    def add_execution(self, execution_time: float, success: bool = True) -> None:
        """Add execution metrics."""
        self.total_executions += 1
        self.total_execution_time += execution_time
        self.min_execution_time = min(self.min_execution_time, execution_time)
        self.max_execution_time = max(self.max_execution_time, execution_time)
        self.last_execution_time = datetime.now(timezone.utc)
        self.recent_execution_times.append(execution_time)
        
        if not success:
            self.error_count += 1
    
    @property
    def average_execution_time(self) -> float:
        """Calculate average execution time."""
        return self.total_execution_time / self.total_executions if self.total_executions > 0 else 0.0
    
    @property
    def recent_average_execution_time(self) -> float:
        """Calculate recent average execution time."""
        return statistics.mean(self.recent_execution_times) if self.recent_execution_times else 0.0
    
    @property
    def error_rate(self) -> float:
        """Calculate error rate as percentage."""
        return (self.error_count / self.total_executions * 100) if self.total_executions > 0 else 0.0
    
    @property
    def throughput_per_second(self) -> float:
        """Calculate throughput based on recent executions."""
        if not self.recent_execution_times:
            return 0.0
        
        recent_total_time = sum(self.recent_execution_times)
        return len(self.recent_execution_times) / recent_total_time if recent_total_time > 0 else 0.0
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert metrics to dictionary."""
        return {
            "operation_name": self.operation_name,
            "total_executions": self.total_executions,
            "average_execution_time": self.average_execution_time,
            "recent_average_execution_time": self.recent_average_execution_time,
            "min_execution_time": self.min_execution_time if self.min_execution_time != float('inf') else 0.0,
            "max_execution_time": self.max_execution_time,
            "error_count": self.error_count,
            "error_rate": self.error_rate,
            "throughput_per_second": self.throughput_per_second,
            "last_execution_time": self.last_execution_time.isoformat() if self.last_execution_time else None
        }


@dataclass
class MemoryMetrics:
    """Memory usage metrics."""
    peak_memory_usage_mb: float = 0.0
    current_memory_usage_mb: float = 0.0
    cache_memory_usage_mb: float = 0.0
    memory_allocations: int = 0
    memory_deallocations: int = 0
    
    def update_memory_usage(self, current_usage_mb: float) -> None:
        """Update current memory usage."""
        self.current_memory_usage_mb = current_usage_mb
        self.peak_memory_usage_mb = max(self.peak_memory_usage_mb, current_usage_mb)
    
    def add_allocation(self, size_mb: float) -> None:
        """Record memory allocation."""
        self.memory_allocations += 1
        self.current_memory_usage_mb += size_mb
        self.peak_memory_usage_mb = max(self.peak_memory_usage_mb, self.current_memory_usage_mb)
    
    def add_deallocation(self, size_mb: float) -> None:
        """Record memory deallocation."""
        self.memory_deallocations += 1
        self.current_memory_usage_mb = max(0, self.current_memory_usage_mb - size_mb)
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert metrics to dictionary."""
        return {
            "peak_memory_usage_mb": self.peak_memory_usage_mb,
            "current_memory_usage_mb": self.current_memory_usage_mb,
            "cache_memory_usage_mb": self.cache_memory_usage_mb,
            "memory_allocations": self.memory_allocations,
            "memory_deallocations": self.memory_deallocations,
            "net_allocations": self.memory_allocations - self.memory_deallocations
        }


@dataclass
class CacheMetrics:
    """Cache performance metrics."""
    cache_hits: int = 0
    cache_misses: int = 0
    cache_evictions: int = 0
    cache_size: int = 0
    cache_memory_usage_mb: float = 0.0
    
    def add_hit(self) -> None:
        """Record cache hit."""
        self.cache_hits += 1
    
    def add_miss(self) -> None:
        """Record cache miss."""
        self.cache_misses += 1
    
    def add_eviction(self) -> None:
        """Record cache eviction."""
        self.cache_evictions += 1
    
    @property
    def hit_rate(self) -> float:
        """Calculate cache hit rate as percentage."""
        total_requests = self.cache_hits + self.cache_misses
        return (self.cache_hits / total_requests * 100) if total_requests > 0 else 0.0
    
    @property
    def miss_rate(self) -> float:
        """Calculate cache miss rate as percentage."""
        return 100.0 - self.hit_rate
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert metrics to dictionary."""
        return {
            "cache_hits": self.cache_hits,
            "cache_misses": self.cache_misses,
            "cache_evictions": self.cache_evictions,
            "cache_size": self.cache_size,
            "cache_memory_usage_mb": self.cache_memory_usage_mb,
            "hit_rate": self.hit_rate,
            "miss_rate": self.miss_rate
        }


@dataclass
class CircuitBreakerMetrics:
    """Circuit breaker performance metrics."""
    total_calls: int = 0
    successful_calls: int = 0
    failed_calls: int = 0
    circuit_open_count: int = 0
    circuit_half_open_count: int = 0
    circuit_closed_count: int = 0
    fallback_executions: int = 0
    recovery_attempts: int = 0
    successful_recoveries: int = 0
    total_open_time_seconds: float = 0.0
    last_failure_time: Optional[datetime] = None
    last_recovery_time: Optional[datetime] = None
    
    def record_call(self, success: bool) -> None:
        """Record circuit breaker call."""
        self.total_calls += 1
        if success:
            self.successful_calls += 1
        else:
            self.failed_calls += 1
            self.last_failure_time = datetime.now(timezone.utc)
    
    def record_state_change(self, new_state: str) -> None:
        """Record circuit breaker state change."""
        if new_state == "OPEN":
            self.circuit_open_count += 1
        elif new_state == "HALF_OPEN":
            self.circuit_half_open_count += 1
        elif new_state == "CLOSED":
            self.circuit_closed_count += 1
    
    def record_fallback_execution(self) -> None:
        """Record fallback execution."""
        self.fallback_executions += 1
    
    def record_recovery_attempt(self, success: bool) -> None:
        """Record recovery attempt."""
        self.recovery_attempts += 1
        if success:
            self.successful_recoveries += 1
            self.last_recovery_time = datetime.now(timezone.utc)
    
    def add_open_time(self, duration_seconds: float) -> None:
        """Add time circuit was open."""
        self.total_open_time_seconds += duration_seconds
    
    @property
    def success_rate(self) -> float:
        """Calculate success rate as percentage."""
        return (self.successful_calls / self.total_calls * 100) if self.total_calls > 0 else 0.0
    
    @property
    def failure_rate(self) -> float:
        """Calculate failure rate as percentage."""
        return (self.failed_calls / self.total_calls * 100) if self.total_calls > 0 else 0.0
    
    @property
    def recovery_success_rate(self) -> float:
        """Calculate recovery success rate as percentage."""
        return (self.successful_recoveries / self.recovery_attempts * 100) if self.recovery_attempts > 0 else 0.0
    
    @property
    def average_open_time_seconds(self) -> float:
        """Calculate average time circuit was open."""
        return self.total_open_time_seconds / self.circuit_open_count if self.circuit_open_count > 0 else 0.0
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert metrics to dictionary."""
        return {
            "total_calls": self.total_calls,
            "successful_calls": self.successful_calls,
            "failed_calls": self.failed_calls,
            "success_rate": self.success_rate,
            "failure_rate": self.failure_rate,
            "circuit_open_count": self.circuit_open_count,
            "circuit_half_open_count": self.circuit_half_open_count,
            "circuit_closed_count": self.circuit_closed_count,
            "fallback_executions": self.fallback_executions,
            "recovery_attempts": self.recovery_attempts,
            "successful_recoveries": self.successful_recoveries,
            "recovery_success_rate": self.recovery_success_rate,
            "total_open_time_seconds": self.total_open_time_seconds,
            "average_open_time_seconds": self.average_open_time_seconds,
            "last_failure_time": self.last_failure_time.isoformat() if self.last_failure_time else None,
            "last_recovery_time": self.last_recovery_time.isoformat() if self.last_recovery_time else None
        }


class PerformanceMonitor:
    """Context manager for monitoring operation performance."""
    
    def __init__(self, metrics_collector: 'FuzzyEngineMetrics', operation_name: str):
        self.metrics_collector = metrics_collector
        self.operation_name = operation_name
        self.start_time = None
        self.success = True
    
    def __enter__(self):
        self.start_time = time.perf_counter()
        return self
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        execution_time = time.perf_counter() - self.start_time
        self.success = exc_type is None
        
        self.metrics_collector.record_operation(
            self.operation_name,
            execution_time,
            self.success
        )
        
        # Check performance thresholds
        self.metrics_collector.check_performance_thresholds(
            self.operation_name,
            execution_time
        )
    
    def mark_error(self):
        """Mark operation as failed."""
        self.success = False


class FuzzyEngineMetrics:
    """Comprehensive metrics collection for FuzzyEngine."""
    
    def __init__(self, performance_thresholds: Optional[Dict[str, float]] = None):
        # Accept either a thresholds dict or an arbitrary object (e.g., config) for compatibility
        thresholds: Dict[str, float] = {}
        if isinstance(performance_thresholds, dict):
            thresholds = performance_thresholds
        else:
            thresholds = {}
            # Optionally retain reference to config-like object without relying on it
            self._config_ref = performance_thresholds  # may be None or a config object
        self.operation_metrics: Dict[str, OperationMetrics] = defaultdict(lambda: OperationMetrics(""))
        self.memory_metrics = MemoryMetrics()
        self.cache_metrics = CacheMetrics()
        self.circuit_breaker_metrics: Dict[str, CircuitBreakerMetrics] = defaultdict(CircuitBreakerMetrics)
        self.performance_thresholds = thresholds
        self.alert_callbacks: List[Callable[[str, Dict[str, Any]], None]] = []
        self.start_time = datetime.now(timezone.utc)
        self._lock = threading.Lock()
        self.logger = logging.getLogger(__name__)
    
    def start_tracking(self) -> None:
        """Start metrics tracking (placeholder method for compatibility)."""
        # This method is called by tests but metrics are always active
        # No action needed as metrics collection is always enabled
        pass
    
    def start_tracking(self) -> None:
        """Start metrics tracking (placeholder method for test compatibility)."""
        # This method is called by tests but doesn't need implementation
        # as metrics tracking is always active
        pass
    
    def stop_tracking(self) -> None:
        """Stop metrics tracking (placeholder method for test compatibility)."""
        # This method is called by tests but doesn't need implementation
        # as metrics tracking is always active
        pass
    
    def record_operation(self, operation_name: str, execution_time: float, success: bool = True) -> None:
        """Record operation execution metrics."""
        with self._lock:
            if operation_name not in self.operation_metrics:
                self.operation_metrics[operation_name] = OperationMetrics(operation_name)
            
            self.operation_metrics[operation_name].add_execution(execution_time, success)
    
    def monitor_operation(self, operation_name: str) -> PerformanceMonitor:
        """Create performance monitor for operation."""
        return PerformanceMonitor(self, operation_name)
    
    def update_memory_metrics(self, current_usage_mb: float) -> None:
        """Update memory usage metrics."""
        with self._lock:
            self.memory_metrics.update_memory_usage(current_usage_mb)
    
    def record_cache_hit(self) -> None:
        """Record cache hit."""
        with self._lock:
            self.cache_metrics.add_hit()
    
    def record_cache_miss(self) -> None:
        """Record cache miss."""
        with self._lock:
            self.cache_metrics.add_miss()
    
    def record_cache_eviction(self) -> None:
        """Record cache eviction."""
        with self._lock:
            self.cache_metrics.add_eviction()
    
    def update_cache_size(self, size: int, memory_usage_mb: float) -> None:
        """Update cache size metrics."""
        with self._lock:
            self.cache_metrics.cache_size = size
            self.cache_metrics.cache_memory_usage_mb = memory_usage_mb
    
    def record_circuit_breaker_call(self, circuit_name: str, success: bool) -> None:
        """Record circuit breaker call."""
        with self._lock:
            self.circuit_breaker_metrics[circuit_name].record_call(success)
    
    def record_circuit_breaker_state_change(self, circuit_name: str, new_state: str) -> None:
        """Record circuit breaker state change."""
        with self._lock:
            self.circuit_breaker_metrics[circuit_name].record_state_change(new_state)
    
    def record_circuit_breaker_fallback(self, circuit_name: str) -> None:
        """Record circuit breaker fallback execution."""
        with self._lock:
            self.circuit_breaker_metrics[circuit_name].record_fallback_execution()
    
    def record_circuit_breaker_recovery(self, circuit_name: str, success: bool) -> None:
        """Record circuit breaker recovery attempt."""
        with self._lock:
            self.circuit_breaker_metrics[circuit_name].record_recovery_attempt(success)
    
    def record_circuit_breaker_open_time(self, circuit_name: str, duration_seconds: float) -> None:
        """Record time circuit breaker was open."""
        with self._lock:
            self.circuit_breaker_metrics[circuit_name].add_open_time(duration_seconds)
    
    def check_performance_thresholds(self, operation_name: str, execution_time: float) -> None:
        """Check if operation exceeds performance thresholds."""
        threshold = self.performance_thresholds.get(operation_name)
        if threshold and execution_time > threshold:
            alert_data = {
                "operation": operation_name,
                "execution_time": execution_time,
                "threshold": threshold,
                "timestamp": datetime.now(timezone.utc).isoformat()
            }
            
            self._trigger_alert(f"Performance threshold exceeded for {operation_name}", alert_data)
    
    def add_alert_callback(self, callback: Callable[[str, Dict[str, Any]], None]) -> None:
        """Add callback for performance alerts."""
        self.alert_callbacks.append(callback)
    
    def _trigger_alert(self, message: str, data: Dict[str, Any]) -> None:
        """Trigger performance alert."""
        self.logger.warning(f"Performance Alert: {message}", extra={"alert_data": data})
        
        for callback in self.alert_callbacks:
            try:
                callback(message, data)
            except Exception as e:
                self.logger.error(f"Error in alert callback: {e}")
    
    def get_operation_metrics(self, operation_name: str) -> Optional[OperationMetrics]:
        """Get metrics for specific operation."""
        return self.operation_metrics.get(operation_name)
    
    def get_all_metrics(self) -> Dict[str, Any]:
        """Get all collected metrics."""
        with self._lock:
            uptime = datetime.now(timezone.utc) - self.start_time
            
            return {
                "uptime_seconds": uptime.total_seconds(),
                "operations": {name: metrics.to_dict() for name, metrics in self.operation_metrics.items()},
                "memory": self.memory_metrics.to_dict(),
                "cache": self.cache_metrics.to_dict(),
                "circuit_breakers": {name: metrics.to_dict() for name, metrics in self.circuit_breaker_metrics.items()},
                "timestamp": datetime.now(timezone.utc).isoformat()
            }
    
    def get_summary_metrics(self) -> Dict[str, Any]:
        """Get summary of key metrics."""
        with self._lock:
            total_operations = sum(metrics.total_executions for metrics in self.operation_metrics.values())
            total_errors = sum(metrics.error_count for metrics in self.operation_metrics.values())
            avg_execution_time = statistics.mean([
                metrics.average_execution_time 
                for metrics in self.operation_metrics.values() 
                if metrics.total_executions > 0
            ]) if self.operation_metrics else 0.0
            
            return {
                "total_operations": total_operations,
                "total_errors": total_errors,
                "overall_error_rate": (total_errors / total_operations * 100) if total_operations > 0 else 0.0,
                "average_execution_time": avg_execution_time,
                "peak_memory_usage_mb": self.memory_metrics.peak_memory_usage_mb,
                "current_memory_usage_mb": self.memory_metrics.current_memory_usage_mb,
                "cache_hit_rate": self.cache_metrics.hit_rate,
                "cache_size": self.cache_metrics.cache_size
            }
    
    def reset_metrics(self) -> None:
        """Reset all metrics (use with caution)."""
        with self._lock:
            self.operation_metrics.clear()
            self.memory_metrics = MemoryMetrics()
            self.cache_metrics = CacheMetrics()
            self.start_time = datetime.now(timezone.utc)
    
    # --- Compatibility and convenience methods expected by engine/tests ---
    def get_performance_summary(self) -> Dict[str, Any]:
        """Return summarized performance stats with optional alerts (compatibility shim)."""
        summary = self.get_summary_metrics()
        alerts: List[str] = []
        # Generate simple alerts if any operation exceeds configured threshold
        with self._lock:
            for op_name, metrics in self.operation_metrics.items():
                threshold = self.performance_thresholds.get(op_name)
                if threshold and metrics.recent_average_execution_time > threshold:
                    alerts.append(
                        f"Operación {op_name} excede umbral: {metrics.recent_average_execution_time:.4f}s > {threshold:.4f}s"
                    )
        summary.update({
            "alerts": alerts,
            "timestamp": datetime.now(timezone.utc).isoformat()
        })
        return summary

    def cleanup_old_metrics(self, max_age_seconds: int = 3600) -> None:
        """Remove or compact metrics that are older than the retention window (compatibility shim)."""
        cutoff = datetime.now(timezone.utc) - timedelta(seconds=max_age_seconds)
        with self._lock:
            to_delete: List[str] = []
            for name, metrics in self.operation_metrics.items():
                # If we never executed, skip
                if metrics.last_execution_time is None:
                    continue
                if metrics.last_execution_time < cutoff:
                    # Compact recent deque and mark for possible deletion if empty
                    metrics.recent_execution_times.clear()
                    # If very old and no executions recently, drop to free memory
                    to_delete.append(name)
            for name in to_delete:
                # Keep at least a minimal entry if it has total_executions to preserve totals
                if self.operation_metrics[name].total_executions == 0:
                    del self.operation_metrics[name]

    def record_task_start(self) -> None:
        """Record the start of an async task (compatibility shim)."""
        # We don't track start events separately; this is a no-op on purpose
        pass

    def record_task_completion(self, execution_time: float) -> None:
        """Record completion of an async task (compatibility shim)."""
        self.record_operation("async_task", execution_time, success=True)

    # Circuit breaker compatibility wrappers
    def record_circuit_breaker_open(self, circuit_name: str) -> None:
        self.record_circuit_breaker_state_change(circuit_name, "OPEN")

    def record_circuit_breaker_close(self, circuit_name: str) -> None:
        self.record_circuit_breaker_state_change(circuit_name, "CLOSED")

    def record_circuit_breaker_success(self, circuit_name: str) -> None:
        self.record_circuit_breaker_call(circuit_name, True)

    def record_circuit_breaker_failure(self, circuit_name: str, failure_type: Optional[str] = None) -> None:
        # failure_type is currently informational only
        self.record_circuit_breaker_call(circuit_name, False)

    def export_metrics_for_monitoring(self) -> Dict[str, Any]:
        """Export metrics in format suitable for external monitoring systems."""
        metrics = self.get_all_metrics()
        
        # Flatten structure for easier consumption by monitoring tools
        flattened = {}
        
        # Add operation metrics
        for op_name, op_metrics in metrics["operations"].items():
            prefix = f"fuzzy_engine.{op_name}"
            flattened[f"{prefix}.total_executions"] = op_metrics["total_executions"]
            flattened[f"{prefix}.average_execution_time"] = op_metrics["average_execution_time"]
            flattened[f"{prefix}.error_rate"] = op_metrics["error_rate"]
            flattened[f"{prefix}.throughput_per_second"] = op_metrics["throughput_per_second"]
        # Add memory metrics
        for key, value in metrics["memory"].items():
            flattened[f"fuzzy_engine.memory.{key}"] = value
        # Add cache metrics
        for key, value in metrics["cache"].items():
            flattened[f"fuzzy_engine.cache.{key}"] = value
        # Add circuit breaker metrics
        for cb_name, cb_metrics in metrics["circuit_breakers"].items():
            prefix = f"fuzzy_engine.circuit_breaker.{cb_name}"
            for key, value in cb_metrics.items():
                flattened[f"{prefix}.{key}"] = value
        # Add system metrics
        flattened["fuzzy_engine.uptime_seconds"] = metrics["uptime_seconds"]
        return flattened