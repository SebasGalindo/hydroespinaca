"""Advanced caching system for FuzzyEngine operations.

Provides LRU cache with TTL, intelligent invalidation, cache metrics,
and memory management for optimal performance.
"""

from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from typing import Any, Dict, Optional, List, Tuple, Callable, TypeVar, Generic
from datetime import datetime, timezone, timedelta
from collections import OrderedDict
import threading
import time
import hashlib
import pickle
import weakref
import logging
import psutil
from enum import Enum

from .FuzzyEngineConfiguration import CacheSettings
from .FuzzyEngineMetrics import CacheMetrics
from .FuzzyEngineExceptions import ValidationException, PerformanceException

T = TypeVar('T')
K = TypeVar('K')
V = TypeVar('V')


class CacheEvictionPolicy(Enum):
    """Cache eviction policies."""
    LRU = "lru"  # Least Recently Used
    LFU = "lfu"  # Least Frequently Used
    FIFO = "fifo"  # First In, First Out
    TTL_BASED = "ttl_based"  # Time To Live based
    ADAPTIVE = "adaptive"  # Adaptive based on access patterns


class CacheInvalidationStrategy(Enum):
    """Cache invalidation strategies."""
    MANUAL = "manual"  # Manual invalidation only
    TIME_BASED = "time_based"  # TTL-based invalidation
    DEPENDENCY_BASED = "dependency_based"  # Invalidate based on dependencies
    PATTERN_BASED = "pattern_based"  # Pattern-based invalidation
    INTELLIGENT = "intelligent"  # AI-based invalidation prediction


@dataclass
class CacheEntry(Generic[V]):
    """Cache entry with metadata."""
    value: V
    created_at: datetime
    last_accessed: datetime
    access_count: int = 0
    ttl_seconds: Optional[int] = None
    dependencies: List[str] = field(default_factory=list)
    tags: List[str] = field(default_factory=list)
    size_bytes: int = 0
    
    def __post_init__(self):
        if self.size_bytes == 0:
            self.size_bytes = self._estimate_size()
    
    def _estimate_size(self) -> int:
        """Estimate memory size of cached value."""
        try:
            return len(pickle.dumps(self.value))
        except Exception:
            # Fallback estimation
            return 1024  # 1KB default
    
    def is_expired(self) -> bool:
        """Check if entry is expired based on TTL."""
        if self.ttl_seconds is None:
            return False
        
        age = (datetime.now(timezone.utc) - self.created_at).total_seconds()
        return age > self.ttl_seconds
    
    def touch(self) -> None:
        """Update access metadata."""
        self.last_accessed = datetime.now(timezone.utc)
        self.access_count += 1
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary for serialization."""
        return {
            'created_at': self.created_at.isoformat(),
            'last_accessed': self.last_accessed.isoformat(),
            'access_count': self.access_count,
            'ttl_seconds': self.ttl_seconds,
            'dependencies': self.dependencies,
            'tags': self.tags,
            'size_bytes': self.size_bytes,
            'is_expired': self.is_expired()
        }


class CacheInterface(ABC, Generic[K, V]):
    """Abstract interface for cache implementations."""
    
    @abstractmethod
    def get(self, key: K) -> Optional[V]:
        """Get value from cache."""
        pass
    
    @abstractmethod
    def put(self, key: K, value: V, ttl_seconds: Optional[int] = None, 
            dependencies: Optional[List[str]] = None, tags: Optional[List[str]] = None) -> None:
        """Put value in cache."""
        pass
    
    @abstractmethod
    def remove(self, key: K) -> bool:
        """Remove value from cache."""
        pass
    
    @abstractmethod
    def clear(self) -> None:
        """Clear all cache entries."""
        pass
    
    @abstractmethod
    def size(self) -> int:
        """Get number of entries in cache."""
        pass
    
    @abstractmethod
    def get_metrics(self) -> CacheMetrics:
        """Get cache metrics."""
        pass


class LRUTTLCache(CacheInterface[K, V]):
    """LRU cache with TTL support and advanced features."""
    
    def __init__(self, 
                 max_size: int = 1000,
                 default_ttl_seconds: Optional[int] = None,
                 eviction_policy: CacheEvictionPolicy = CacheEvictionPolicy.LRU,
                 invalidation_strategy: CacheInvalidationStrategy = CacheInvalidationStrategy.TIME_BASED,
                 memory_limit_mb: float = 50.0,
                 cleanup_interval_seconds: int = 60):
        
        self.max_size = max_size
        self.default_ttl_seconds = default_ttl_seconds
        self.eviction_policy = eviction_policy
        self.invalidation_strategy = invalidation_strategy
        self.memory_limit_bytes = int(memory_limit_mb * 1024 * 1024)
        self.cleanup_interval_seconds = cleanup_interval_seconds
        
        # Cache storage
        self._cache: OrderedDict[K, CacheEntry[V]] = OrderedDict()
        self._lock = threading.RLock()
        
        # Metrics
        self._metrics = CacheMetrics()
        
        # Cleanup thread
        self._cleanup_thread: Optional[threading.Thread] = None
        self._stop_cleanup = threading.Event()
        
        # Dependency tracking
        self._dependency_map: Dict[str, List[K]] = {}
        self._tag_map: Dict[str, List[K]] = {}
        
        # Access pattern tracking for adaptive eviction
        self._access_patterns: Dict[K, List[datetime]] = {}
        
        self.logger = logging.getLogger(__name__)
        
        # Start cleanup thread
        self._start_cleanup_thread()
    
    def _start_cleanup_thread(self) -> None:
        """Start background cleanup thread."""
        if self._cleanup_thread is None or not self._cleanup_thread.is_alive():
            self._cleanup_thread = threading.Thread(
                target=self._cleanup_worker,
                daemon=True,
                name="CacheCleanup"
            )
            self._cleanup_thread.start()
    
    def _cleanup_worker(self) -> None:
        """Background worker for cache cleanup."""
        while not self._stop_cleanup.wait(self.cleanup_interval_seconds):
            try:
                self._cleanup_expired_entries()
                self._enforce_memory_limit()
                self._update_access_patterns()
            except Exception as e:
                self.logger.error(f"Error during cache cleanup: {e}")
    
    def _cleanup_expired_entries(self) -> None:
        """Remove expired entries from cache."""
        with self._lock:
            expired_keys = []
            
            for key, entry in self._cache.items():
                if entry.is_expired():
                    expired_keys.append(key)
            
            for key in expired_keys:
                self._remove_entry(key)
                self._metrics.add_eviction()
            
            if expired_keys:
                self.logger.debug(f"Cleaned up {len(expired_keys)} expired cache entries")
    
    def _enforce_memory_limit(self) -> None:
        """Enforce memory limit by evicting entries."""
        with self._lock:
            current_memory = self._calculate_memory_usage()
            
            if current_memory > self.memory_limit_bytes:
                # Calculate how much memory to free (with 10% buffer)
                target_memory = int(self.memory_limit_bytes * 0.9)
                memory_to_free = current_memory - target_memory
                
                evicted_keys = self._evict_entries_by_policy(memory_to_free)
                
                if evicted_keys:
                    self.logger.info(
                        f"Evicted {len(evicted_keys)} entries to free {memory_to_free} bytes. "
                        f"Memory usage: {current_memory} -> {self._calculate_memory_usage()}"
                    )
    
    def _calculate_memory_usage(self) -> int:
        """Calculate total memory usage of cache."""
        return sum(entry.size_bytes for entry in self._cache.values())
    
    def _evict_entries_by_policy(self, target_bytes: int) -> List[K]:
        """Evict entries based on eviction policy."""
        evicted_keys = []
        freed_bytes = 0
        
        if self.eviction_policy == CacheEvictionPolicy.LRU:
            # Evict least recently used
            for key, entry in list(self._cache.items()):
                if freed_bytes >= target_bytes:
                    break
                
                freed_bytes += entry.size_bytes
                self._remove_entry(key)
                evicted_keys.append(key)
                self._metrics.add_eviction()
        
        elif self.eviction_policy == CacheEvictionPolicy.LFU:
            # Evict least frequently used
            sorted_entries = sorted(
                self._cache.items(),
                key=lambda x: x[1].access_count
            )
            
            for key, entry in sorted_entries:
                if freed_bytes >= target_bytes:
                    break
                
                freed_bytes += entry.size_bytes
                self._remove_entry(key)
                evicted_keys.append(key)
                self._metrics.add_eviction()
        
        elif self.eviction_policy == CacheEvictionPolicy.TTL_BASED:
            # Evict entries closest to expiration
            now = datetime.now(timezone.utc)
            sorted_entries = sorted(
                self._cache.items(),
                key=lambda x: (x[1].created_at + timedelta(seconds=x[1].ttl_seconds or 0)) if x[1].ttl_seconds else now
            )
            
            for key, entry in sorted_entries:
                if freed_bytes >= target_bytes:
                    break
                
                freed_bytes += entry.size_bytes
                self._remove_entry(key)
                evicted_keys.append(key)
                self._metrics.add_eviction()
        
        elif self.eviction_policy == CacheEvictionPolicy.ADAPTIVE:
            # Adaptive eviction based on access patterns
            evicted_keys = self._adaptive_eviction(target_bytes)
        
        return evicted_keys
    
    def _adaptive_eviction(self, target_bytes: int) -> List[K]:
        """Adaptive eviction based on access patterns and prediction."""
        evicted_keys = []
        freed_bytes = 0
        
        # Score entries based on multiple factors
        scored_entries = []
        now = datetime.now(timezone.utc)
        
        for key, entry in self._cache.items():
            # Calculate composite score (lower = more likely to evict)
            recency_score = (now - entry.last_accessed).total_seconds() / 3600  # Hours since last access
            frequency_score = 1.0 / max(entry.access_count, 1)  # Inverse frequency
            size_score = entry.size_bytes / (1024 * 1024)  # Size in MB
            
            # Predict future access probability
            access_pattern = self._access_patterns.get(key, [])
            prediction_score = self._predict_future_access(access_pattern)
            
            composite_score = (recency_score * 0.3 + 
                             frequency_score * 0.3 + 
                             size_score * 0.2 + 
                             prediction_score * 0.2)
            
            scored_entries.append((composite_score, key, entry))
        
        # Sort by score (highest first = most likely to evict)
        scored_entries.sort(reverse=True)
        
        for score, key, entry in scored_entries:
            if freed_bytes >= target_bytes:
                break
            
            freed_bytes += entry.size_bytes
            self._remove_entry(key)
            evicted_keys.append(key)
            self._metrics.add_eviction()
        
        return evicted_keys
    
    def _predict_future_access(self, access_pattern: List[datetime]) -> float:
        """Predict probability of future access based on historical pattern."""
        if not access_pattern:
            return 1.0  # High score = likely to evict
        
        now = datetime.now(timezone.utc)
        recent_accesses = [
            access for access in access_pattern
            if (now - access).total_seconds() < 3600  # Last hour
        ]
        
        if not recent_accesses:
            return 1.0
        
        # Simple prediction: more recent accesses = lower eviction probability
        avg_interval = sum(
            (now - access).total_seconds() for access in recent_accesses
        ) / len(recent_accesses)
        
        # Normalize to 0-1 range (lower = less likely to evict)
        return min(avg_interval / 3600, 1.0)
    
    def _update_access_patterns(self) -> None:
        """Update access patterns for adaptive eviction."""
        with self._lock:
            # Clean old access patterns (keep last 24 hours)
            cutoff = datetime.now(timezone.utc) - timedelta(hours=24)
            
            for key in list(self._access_patterns.keys()):
                if key not in self._cache:
                    # Remove patterns for evicted entries
                    del self._access_patterns[key]
                else:
                    # Filter old accesses
                    self._access_patterns[key] = [
                        access for access in self._access_patterns[key]
                        if access > cutoff
                    ]
    
    def _remove_entry(self, key: K) -> None:
        """Remove entry and update dependency/tag mappings."""
        if key not in self._cache:
            return
        
        entry = self._cache[key]
        
        # Remove from dependency map
        for dep in entry.dependencies:
            if dep in self._dependency_map:
                self._dependency_map[dep] = [
                    k for k in self._dependency_map[dep] if k != key
                ]
                if not self._dependency_map[dep]:
                    del self._dependency_map[dep]
        
        # Remove from tag map
        for tag in entry.tags:
            if tag in self._tag_map:
                self._tag_map[tag] = [
                    k for k in self._tag_map[tag] if k != key
                ]
                if not self._tag_map[tag]:
                    del self._tag_map[tag]
        
        # Remove from access patterns
        if key in self._access_patterns:
            del self._access_patterns[key]
        
        # Remove from cache
        del self._cache[key]
    
    def get(self, key: K) -> Optional[V]:
        """Get value from cache."""
        with self._lock:
            if key not in self._cache:
                self._metrics.add_miss()
                return None
            
            entry = self._cache[key]
            
            # Check if expired
            if entry.is_expired():
                self._remove_entry(key)
                self._metrics.add_miss()
                return None
            
            # Update access metadata
            entry.touch()
            
            # Move to end for LRU
            self._cache.move_to_end(key)
            
            # Update access patterns
            if key not in self._access_patterns:
                self._access_patterns[key] = []
            self._access_patterns[key].append(datetime.now(timezone.utc))
            
            # Keep only recent access history
            cutoff = datetime.now(timezone.utc) - timedelta(hours=1)
            self._access_patterns[key] = [
                access for access in self._access_patterns[key]
                if access > cutoff
            ]
            
            self._metrics.add_hit()
            return entry.value
    
    def put(self, key: K, value: V, ttl_seconds: Optional[int] = None,
            dependencies: Optional[List[str]] = None, tags: Optional[List[str]] = None) -> None:
        """Put value in cache."""
        with self._lock:
            # Use default TTL if not specified
            if ttl_seconds is None:
                ttl_seconds = self.default_ttl_seconds
            
            # Create cache entry
            now = datetime.now(timezone.utc)
            entry = CacheEntry(
                value=value,
                created_at=now,
                last_accessed=now,
                ttl_seconds=ttl_seconds,
                dependencies=dependencies or [],
                tags=tags or []
            )
            
            # Check if we need to evict entries
            if len(self._cache) >= self.max_size and key not in self._cache:
                self._evict_entries_by_policy(entry.size_bytes)
            
            # Remove existing entry if updating
            if key in self._cache:
                self._remove_entry(key)
            
            # Add new entry
            self._cache[key] = entry
            
            # Update dependency map
            for dep in entry.dependencies:
                if dep not in self._dependency_map:
                    self._dependency_map[dep] = []
                self._dependency_map[dep].append(key)
            
            # Update tag map
            for tag in entry.tags:
                if tag not in self._tag_map:
                    self._tag_map[tag] = []
                self._tag_map[tag].append(key)
            
            # Update metrics
            self._metrics.cache_size = len(self._cache)
            self._metrics.cache_memory_usage_mb = self._calculate_memory_usage() / (1024 * 1024)
    
    def remove(self, key: K) -> bool:
        """Remove value from cache."""
        with self._lock:
            if key in self._cache:
                self._remove_entry(key)
                self._metrics.cache_size = len(self._cache)
                self._metrics.cache_memory_usage_mb = self._calculate_memory_usage() / (1024 * 1024)
                return True
            return False
    
    def clear(self) -> None:
        """Clear all cache entries."""
        with self._lock:
            self._cache.clear()
            self._dependency_map.clear()
            self._tag_map.clear()
            self._access_patterns.clear()
            self._metrics.cache_size = 0
            self._metrics.cache_memory_usage_mb = 0.0
    
    def size(self) -> int:
        """Get number of entries in cache."""
        return len(self._cache)
    
    def get_metrics(self) -> CacheMetrics:
        """Get cache metrics."""
        with self._lock:
            self._metrics.cache_size = len(self._cache)
            self._metrics.cache_memory_usage_mb = self._calculate_memory_usage() / (1024 * 1024)
            return self._metrics
    
    def invalidate_by_dependency(self, dependency: str) -> int:
        """Invalidate all entries with given dependency."""
        with self._lock:
            if dependency not in self._dependency_map:
                return 0
            
            keys_to_remove = self._dependency_map[dependency].copy()
            
            for key in keys_to_remove:
                self._remove_entry(key)
            
            self._metrics.cache_size = len(self._cache)
            self._metrics.cache_memory_usage_mb = self._calculate_memory_usage() / (1024 * 1024)
            
            return len(keys_to_remove)
    
    def invalidate_by_tag(self, tag: str) -> int:
        """Invalidate all entries with given tag."""
        with self._lock:
            if tag not in self._tag_map:
                return 0
            
            keys_to_remove = self._tag_map[tag].copy()
            
            for key in keys_to_remove:
                self._remove_entry(key)
            
            self._metrics.cache_size = len(self._cache)
            self._metrics.cache_memory_usage_mb = self._calculate_memory_usage() / (1024 * 1024)
            
            return len(keys_to_remove)
    
    def invalidate_by_pattern(self, pattern: str) -> int:
        """Invalidate entries matching key pattern."""
        with self._lock:
            import re
            regex = re.compile(pattern)
            
            keys_to_remove = [
                key for key in self._cache.keys()
                if isinstance(key, str) and regex.search(key)
            ]
            
            for key in keys_to_remove:
                self._remove_entry(key)
            
            self._metrics.cache_size = len(self._cache)
            self._metrics.cache_memory_usage_mb = self._calculate_memory_usage() / (1024 * 1024)
            
            return len(keys_to_remove)
    
    def get_entry_info(self, key: K) -> Optional[Dict[str, Any]]:
        """Get detailed information about cache entry."""
        with self._lock:
            if key not in self._cache:
                return None
            
            return self._cache[key].to_dict()
    
    def get_all_entries_info(self) -> Dict[K, Dict[str, Any]]:
        """Get information about all cache entries."""
        with self._lock:
            return {
                key: entry.to_dict()
                for key, entry in self._cache.items()
            }
    
    def optimize_cache(self) -> Dict[str, Any]:
        """Optimize cache performance and return optimization report."""
        with self._lock:
            initial_size = len(self._cache)
            initial_memory = self._calculate_memory_usage()
            
            # Remove expired entries
            self._cleanup_expired_entries()
            
            # Optimize access patterns
            self._update_access_patterns()
            
            # Enforce memory limit
            self._enforce_memory_limit()
            
            final_size = len(self._cache)
            final_memory = self._calculate_memory_usage()
            
            return {
                'initial_entries': initial_size,
                'final_entries': final_size,
                'entries_removed': initial_size - final_size,
                'initial_memory_mb': initial_memory / (1024 * 1024),
                'final_memory_mb': final_memory / (1024 * 1024),
                'memory_freed_mb': (initial_memory - final_memory) / (1024 * 1024),
                'hit_rate': self._metrics.hit_rate,
                'optimization_timestamp': datetime.now(timezone.utc).isoformat()
            }
    
    def shutdown(self) -> None:
        """Shutdown cache and cleanup resources."""
        self._stop_cleanup.set()
        if self._cleanup_thread and self._cleanup_thread.is_alive():
            self._cleanup_thread.join(timeout=5.0)
        
        self.clear()
        self.logger.info("Cache shutdown completed")
    
    def __del__(self):
        """Destructor to ensure cleanup."""
        try:
            self.shutdown()
        except Exception:
            pass


class FuzzyEngineCache:
    """Main cache manager for FuzzyEngine operations."""
    
    def __init__(self, config: CacheSettings):
        self.config = config
        self.logger = logging.getLogger(__name__)
        
        # Initialize different cache instances
        self.membership_cache: Optional[LRUTTLCache] = None
        self.rule_cache: Optional[LRUTTLCache] = None
        self.defuzzification_cache: Optional[LRUTTLCache] = None
        self.variable_cache: Optional[LRUTTLCache] = None
        
        # Global cache metrics
        self._global_metrics = CacheMetrics()
        self._metrics_lock = threading.Lock()
        
        self._initialize_caches()
    
    def _initialize_caches(self) -> None:
        """Initialize cache instances based on configuration."""
        cache_memory_per_type = self.config.cache_memory_limit_mb / 4  # Distribute among 4 cache types
        
        if self.config.enable_membership_cache:
            self.membership_cache = LRUTTLCache(
                max_size=self.config.max_cache_entries // 4,
                default_ttl_seconds=self.config.cache_ttl_seconds,
                memory_limit_mb=cache_memory_per_type,
                cleanup_interval_seconds=self.config.cache_cleanup_interval_seconds
            )
        
        if self.config.enable_rule_cache:
            self.rule_cache = LRUTTLCache(
                max_size=self.config.max_cache_entries // 4,
                default_ttl_seconds=self.config.cache_ttl_seconds,
                memory_limit_mb=cache_memory_per_type,
                cleanup_interval_seconds=self.config.cache_cleanup_interval_seconds
            )
        
        if self.config.enable_defuzzification_cache:
            self.defuzzification_cache = LRUTTLCache(
                max_size=self.config.max_cache_entries // 4,
                default_ttl_seconds=self.config.cache_ttl_seconds,
                memory_limit_mb=cache_memory_per_type,
                cleanup_interval_seconds=self.config.cache_cleanup_interval_seconds
            )
        
        # Variable cache is always enabled for performance
        self.variable_cache = LRUTTLCache(
            max_size=self.config.max_cache_entries // 4,
            default_ttl_seconds=self.config.cache_ttl_seconds * 2,  # Variables change less frequently
            memory_limit_mb=cache_memory_per_type,
            cleanup_interval_seconds=self.config.cache_cleanup_interval_seconds
        )
        
        self.logger.info("FuzzyEngine cache system initialized")
    
    def get_membership_value(self, key: str) -> Optional[float]:
        """Get cached membership value."""
        if not self.membership_cache:
            return None
        return self.membership_cache.get(key)
    
    def cache_membership_value(self, key: str, value: float, 
                              dependencies: Optional[List[str]] = None) -> None:
        """Cache membership value."""
        if self.membership_cache:
            self.membership_cache.put(key, value, dependencies=dependencies, tags=["membership"])
    
    def get_rule_result(self, key: str) -> Optional[Dict[str, Any]]:
        """Get cached rule evaluation result."""
        if not self.rule_cache:
            return None
        return self.rule_cache.get(key)
    
    def cache_rule_result(self, key: str, result: Dict[str, Any],
                         dependencies: Optional[List[str]] = None) -> None:
        """Cache rule evaluation result."""
        if self.rule_cache:
            self.rule_cache.put(key, result, dependencies=dependencies, tags=["rule"])
    
    def get_defuzzification_result(self, key: str) -> Optional[float]:
        """Get cached defuzzification result."""
        if not self.defuzzification_cache:
            return None
        return self.defuzzification_cache.get(key)
    
    def cache_defuzzification_result(self, key: str, result: float,
                                   dependencies: Optional[List[str]] = None) -> None:
        """Cache defuzzification result."""
        if self.defuzzification_cache:
            self.defuzzification_cache.put(key, result, dependencies=dependencies, tags=["defuzzification"])
    
    def get_variable_definition(self, key: str) -> Optional[Dict[str, Any]]:
        """Get cached variable definition."""
        if not self.variable_cache:
            return None
        return self.variable_cache.get(key)
    
    def cache_variable_definition(self, key: str, definition: Dict[str, Any],
                                dependencies: Optional[List[str]] = None) -> None:
        """Cache variable definition."""
        if self.variable_cache:
            self.variable_cache.put(key, definition, dependencies=dependencies, tags=["variable"])
    
    def invalidate_by_dependency(self, dependency: str) -> int:
        """Invalidate all cache entries with given dependency."""
        total_invalidated = 0
        
        for cache in [self.membership_cache, self.rule_cache, 
                     self.defuzzification_cache, self.variable_cache]:
            if cache:
                total_invalidated += cache.invalidate_by_dependency(dependency)
        
        self.logger.info(f"Invalidated {total_invalidated} cache entries for dependency: {dependency}")
        return total_invalidated
    
    def invalidate_by_tag(self, tag: str) -> int:
        """Invalidate all cache entries with given tag."""
        total_invalidated = 0
        
        for cache in [self.membership_cache, self.rule_cache,
                     self.defuzzification_cache, self.variable_cache]:
            if cache:
                total_invalidated += cache.invalidate_by_tag(tag)
        
        self.logger.info(f"Invalidated {total_invalidated} cache entries for tag: {tag}")
        return total_invalidated
    
    def clear_all_caches(self) -> None:
        """Clear all cache instances."""
        for cache in [self.membership_cache, self.rule_cache,
                     self.defuzzification_cache, self.variable_cache]:
            if cache:
                cache.clear()
        
        self.logger.info("All caches cleared")
    
    def get_global_metrics(self) -> CacheMetrics:
        """Get aggregated metrics from all cache instances."""
        with self._metrics_lock:
            total_metrics = CacheMetrics()
            
            for cache in [self.membership_cache, self.rule_cache,
                         self.defuzzification_cache, self.variable_cache]:
                if cache:
                    cache_metrics = cache.get_metrics()
                    total_metrics.cache_hits += cache_metrics.cache_hits
                    total_metrics.cache_misses += cache_metrics.cache_misses
                    total_metrics.cache_evictions += cache_metrics.cache_evictions
                    total_metrics.cache_size += cache_metrics.cache_size
                    total_metrics.cache_memory_usage_mb += cache_metrics.cache_memory_usage_mb
            
            return total_metrics
    
    def get_cache_status(self) -> Dict[str, Any]:
        """Get detailed status of all cache instances."""
        status = {
            'global_metrics': self.get_global_metrics().to_dict(),
            'cache_instances': {}
        }
        
        cache_instances = {
            'membership': self.membership_cache,
            'rule': self.rule_cache,
            'defuzzification': self.defuzzification_cache,
            'variable': self.variable_cache
        }
        
        for name, cache in cache_instances.items():
            if cache:
                metrics = cache.get_metrics()
                status['cache_instances'][name] = {
                    'enabled': True,
                    'size': cache.size(),
                    'metrics': metrics.to_dict(),
                    'memory_usage_mb': metrics.cache_memory_usage_mb,
                    'hit_rate': metrics.hit_rate
                }
            else:
                status['cache_instances'][name] = {
                    'enabled': False
                }
        
        return status
    
    def optimize_all_caches(self) -> Dict[str, Any]:
        """Optimize all cache instances and return optimization report."""
        optimization_report = {
            'timestamp': datetime.now(timezone.utc).isoformat(),
            'cache_optimizations': {}
        }
        
        cache_instances = {
            'membership': self.membership_cache,
            'rule': self.rule_cache,
            'defuzzification': self.defuzzification_cache,
            'variable': self.variable_cache
        }
        
        for name, cache in cache_instances.items():
            if cache:
                optimization_report['cache_optimizations'][name] = cache.optimize_cache()
        
        self.logger.info("Cache optimization completed")
        return optimization_report
    
    def health_check(self) -> Dict[str, Any]:
        """Perform health check on cache system."""
        health_status = {
            'status': 'healthy',
            'timestamp': datetime.now(timezone.utc).isoformat(),
            'issues': [],
            'metrics': self.get_global_metrics().to_dict()
        }
        
        # Check hit rate
        global_metrics = self.get_global_metrics()
        if global_metrics.hit_rate < self.config.cache_hit_ratio_threshold * 100:
            health_status['issues'].append({
                'type': 'low_hit_rate',
                'message': f"Cache hit rate ({global_metrics.hit_rate:.1f}%) below threshold ({self.config.cache_hit_ratio_threshold * 100:.1f}%)",
                'severity': 'warning'
            })
        
        # Check memory usage
        if global_metrics.cache_memory_usage_mb > self.config.cache_memory_limit_mb * 0.9:
            health_status['issues'].append({
                'type': 'high_memory_usage',
                'message': f"Cache memory usage ({global_metrics.cache_memory_usage_mb:.1f}MB) near limit ({self.config.cache_memory_limit_mb}MB)",
                'severity': 'warning'
            })
        
        # Check system memory
        try:
            system_memory = psutil.virtual_memory()
            if system_memory.percent > 90:
                health_status['issues'].append({
                    'type': 'system_memory_pressure',
                    'message': f"System memory usage high ({system_memory.percent:.1f}%)",
                    'severity': 'critical'
                })
        except Exception as e:
            health_status['issues'].append({
                'type': 'memory_check_failed',
                'message': f"Could not check system memory: {e}",
                'severity': 'warning'
            })
        
        # Determine overall status
        if any(issue['severity'] == 'critical' for issue in health_status['issues']):
            health_status['status'] = 'critical'
        elif any(issue['severity'] == 'warning' for issue in health_status['issues']):
            health_status['status'] = 'degraded'
        
        return health_status
    
    def shutdown(self) -> None:
        """Shutdown cache system and cleanup resources."""
        for cache in [self.membership_cache, self.rule_cache,
                     self.defuzzification_cache, self.variable_cache]:
            if cache:
                cache.shutdown()
        
        self.logger.info("FuzzyEngine cache system shutdown completed")
    
    def __enter__(self):
        """Context manager entry."""
        return self
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        """Context manager exit."""
        self.shutdown()


def create_cache_key(*args, **kwargs) -> str:
    """Create a consistent cache key from arguments."""
    # Convert arguments to string representation
    key_parts = []
    
    for arg in args:
        if isinstance(arg, (str, int, float, bool)):
            key_parts.append(str(arg))
        else:
            # Use hash for complex objects
            key_parts.append(str(hash(str(arg))))
    
    for k, v in sorted(kwargs.items()):
        if isinstance(v, (str, int, float, bool)):
            key_parts.append(f"{k}={v}")
        else:
            key_parts.append(f"{k}={hash(str(v))}")
    
    # Create hash of combined key
    combined_key = "|".join(key_parts)
    return hashlib.md5(combined_key.encode()).hexdigest()


def cache_result(cache_instance: CacheInterface, 
                key_generator: Callable[..., str],
                ttl_seconds: Optional[int] = None,
                dependencies: Optional[List[str]] = None,
                tags: Optional[List[str]] = None):
    """Decorator for caching function results."""
    def decorator(func):
        def wrapper(*args, **kwargs):
            # Generate cache key
            cache_key = key_generator(*args, **kwargs)
            
            # Try to get from cache
            cached_result = cache_instance.get(cache_key)
            if cached_result is not None:
                return cached_result
            
            # Execute function and cache result
            result = func(*args, **kwargs)
            cache_instance.put(cache_key, result, ttl_seconds, dependencies, tags)
            
            return result
        return wrapper
    return decorator