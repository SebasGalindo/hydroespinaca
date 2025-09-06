"""Comprehensive tests for FuzzyEngine caching system.

Tests cover LRU cache, TTL functionality, eviction policies,
memory management, dependency invalidation, and performance.
"""

import unittest
import time
import threading
from datetime import datetime, timezone, timedelta
from unittest.mock import Mock, patch, MagicMock
import tempfile
import os
from typing import Dict, Any, List

from FuzzyService.Infrastructure.FuzzyEngine.FuzzyEngineCache import (
    LRUTTLCache, FuzzyEngineCache, CacheEntry, CacheEvictionPolicy,
    CacheInvalidationStrategy, create_cache_key, cache_result
)
from FuzzyService.Infrastructure.FuzzyEngine.FuzzyEngineConfiguration import CacheSettings
from FuzzyService.Infrastructure.FuzzyEngine.FuzzyEngineMetrics import CacheMetrics


class TestCacheEntry(unittest.TestCase):
    """Test CacheEntry functionality."""
    
    def test_cache_entry_creation(self):
        """Test cache entry creation and basic properties."""
        now = datetime.now(timezone.utc)
        entry = CacheEntry(
            value="test_value",
            created_at=now,
            last_accessed=now,
            ttl_seconds=300,
            dependencies=["dep1", "dep2"],
            tags=["tag1", "tag2"]
        )
        
        self.assertEqual(entry.value, "test_value")
        self.assertEqual(entry.created_at, now)
        self.assertEqual(entry.last_accessed, now)
        self.assertEqual(entry.ttl_seconds, 300)
        self.assertEqual(entry.dependencies, ["dep1", "dep2"])
        self.assertEqual(entry.tags, ["tag1", "tag2"])
        self.assertEqual(entry.access_count, 0)
        self.assertGreater(entry.size_bytes, 0)
    
    def test_cache_entry_expiration(self):
        """Test TTL expiration logic."""
        now = datetime.now(timezone.utc)
        
        # Non-expiring entry
        entry_no_ttl = CacheEntry(
            value="test",
            created_at=now,
            last_accessed=now
        )
        self.assertFalse(entry_no_ttl.is_expired())
        
        # Fresh entry
        entry_fresh = CacheEntry(
            value="test",
            created_at=now,
            last_accessed=now,
            ttl_seconds=300
        )
        self.assertFalse(entry_fresh.is_expired())
        
        # Expired entry
        old_time = now - timedelta(seconds=400)
        entry_expired = CacheEntry(
            value="test",
            created_at=old_time,
            last_accessed=old_time,
            ttl_seconds=300
        )
        self.assertTrue(entry_expired.is_expired())
    
    def test_cache_entry_touch(self):
        """Test access tracking."""
        now = datetime.now(timezone.utc)
        entry = CacheEntry(
            value="test",
            created_at=now,
            last_accessed=now
        )
        
        initial_access_count = entry.access_count
        initial_last_accessed = entry.last_accessed
        
        time.sleep(0.01)  # Small delay
        entry.touch()
        
        self.assertEqual(entry.access_count, initial_access_count + 1)
        self.assertGreater(entry.last_accessed, initial_last_accessed)
    
    def test_cache_entry_to_dict(self):
        """Test serialization to dictionary."""
        now = datetime.now(timezone.utc)
        entry = CacheEntry(
            value="test",
            created_at=now,
            last_accessed=now,
            ttl_seconds=300,
            dependencies=["dep1"],
            tags=["tag1"]
        )
        
        entry_dict = entry.to_dict()
        
        self.assertIn('created_at', entry_dict)
        self.assertIn('last_accessed', entry_dict)
        self.assertIn('access_count', entry_dict)
        self.assertIn('ttl_seconds', entry_dict)
        self.assertIn('dependencies', entry_dict)
        self.assertIn('tags', entry_dict)
        self.assertIn('size_bytes', entry_dict)
        self.assertIn('is_expired', entry_dict)
        
        self.assertEqual(entry_dict['ttl_seconds'], 300)
        self.assertEqual(entry_dict['dependencies'], ["dep1"])
        self.assertEqual(entry_dict['tags'], ["tag1"])


class TestLRUTTLCache(unittest.TestCase):
    """Test LRU cache with TTL functionality."""
    
    def setUp(self):
        """Set up test cache."""
        self.cache = LRUTTLCache(
            max_size=5,
            default_ttl_seconds=300,
            memory_limit_mb=1.0,
            cleanup_interval_seconds=1
        )
    
    def tearDown(self):
        """Clean up test cache."""
        self.cache.shutdown()
    
    def test_basic_operations(self):
        """Test basic cache operations."""
        # Test put and get
        self.cache.put("key1", "value1")
        self.assertEqual(self.cache.get("key1"), "value1")
        
        # Test non-existent key
        self.assertIsNone(self.cache.get("nonexistent"))
        
        # Test size
        self.assertEqual(self.cache.size(), 1)
        
        # Test remove
        self.assertTrue(self.cache.remove("key1"))
        self.assertIsNone(self.cache.get("key1"))
        self.assertEqual(self.cache.size(), 0)
        
        # Test remove non-existent
        self.assertFalse(self.cache.remove("nonexistent"))
    
    def test_lru_eviction(self):
        """Test LRU eviction policy."""
        # Fill cache to capacity
        for i in range(5):
            self.cache.put(f"key{i}", f"value{i}")
        
        self.assertEqual(self.cache.size(), 5)
        
        # Access key1 to make it recently used
        self.cache.get("key1")
        
        # Add one more item to trigger eviction
        self.cache.put("key5", "value5")
        
        # key0 should be evicted (least recently used)
        self.assertIsNone(self.cache.get("key0"))
        self.assertEqual(self.cache.get("key1"), "value1")  # Should still exist
        self.assertEqual(self.cache.size(), 5)
    
    def test_ttl_expiration(self):
        """Test TTL-based expiration."""
        # Add item with short TTL
        self.cache.put("key1", "value1", ttl_seconds=1)
        
        # Should be available immediately
        self.assertEqual(self.cache.get("key1"), "value1")
        
        # Wait for expiration
        time.sleep(1.1)
        
        # Should be expired
        self.assertIsNone(self.cache.get("key1"))
        self.assertEqual(self.cache.size(), 0)
    
    def test_dependency_invalidation(self):
        """Test dependency-based invalidation."""
        # Add items with dependencies
        self.cache.put("key1", "value1", dependencies=["dep1"])
        self.cache.put("key2", "value2", dependencies=["dep1", "dep2"])
        self.cache.put("key3", "value3", dependencies=["dep2"])
        
        self.assertEqual(self.cache.size(), 3)
        
        # Invalidate by dependency
        invalidated = self.cache.invalidate_by_dependency("dep1")
        self.assertEqual(invalidated, 2)  # key1 and key2
        
        self.assertIsNone(self.cache.get("key1"))
        self.assertIsNone(self.cache.get("key2"))
        self.assertEqual(self.cache.get("key3"), "value3")  # Should remain
        self.assertEqual(self.cache.size(), 1)
    
    def test_tag_invalidation(self):
        """Test tag-based invalidation."""
        # Add items with tags
        self.cache.put("key1", "value1", tags=["tag1"])
        self.cache.put("key2", "value2", tags=["tag1", "tag2"])
        self.cache.put("key3", "value3", tags=["tag2"])
        
        self.assertEqual(self.cache.size(), 3)
        
        # Invalidate by tag
        invalidated = self.cache.invalidate_by_tag("tag1")
        self.assertEqual(invalidated, 2)  # key1 and key2
        
        self.assertIsNone(self.cache.get("key1"))
        self.assertIsNone(self.cache.get("key2"))
        self.assertEqual(self.cache.get("key3"), "value3")  # Should remain
        self.assertEqual(self.cache.size(), 1)
    
    def test_pattern_invalidation(self):
        """Test pattern-based invalidation."""
        # Add items with pattern-matching keys
        self.cache.put("user_123", "value1")
        self.cache.put("user_456", "value2")
        self.cache.put("product_789", "value3")
        
        self.assertEqual(self.cache.size(), 3)
        
        # Invalidate by pattern
        invalidated = self.cache.invalidate_by_pattern(r"user_\d+")
        self.assertEqual(invalidated, 2)  # user_123 and user_456
        
        self.assertIsNone(self.cache.get("user_123"))
        self.assertIsNone(self.cache.get("user_456"))
        self.assertEqual(self.cache.get("product_789"), "value3")  # Should remain
        self.assertEqual(self.cache.size(), 1)
    
    def test_metrics_tracking(self):
        """Test cache metrics tracking."""
        metrics = self.cache.get_metrics()
        initial_hits = metrics.cache_hits
        initial_misses = metrics.cache_misses
        
        # Generate hits and misses
        self.cache.put("key1", "value1")
        self.cache.get("key1")  # Hit
        self.cache.get("nonexistent")  # Miss
        
        metrics = self.cache.get_metrics()
        self.assertEqual(metrics.cache_hits, initial_hits + 1)
        self.assertEqual(metrics.cache_misses, initial_misses + 1)
        self.assertEqual(metrics.cache_size, 1)
    
    def test_memory_limit_enforcement(self):
        """Test memory limit enforcement."""
        # Create cache with very small memory limit
        small_cache = LRUTTLCache(
            max_size=100,
            memory_limit_mb=0.001,  # 1KB limit
            cleanup_interval_seconds=0.1
        )
        
        try:
            # Add large items to exceed memory limit
            large_value = "x" * 1000  # 1KB string
            
            for i in range(5):
                small_cache.put(f"key{i}", large_value)
            
            # Wait for cleanup
            time.sleep(0.2)
            
            # Should have evicted some entries due to memory pressure
            self.assertLess(small_cache.size(), 5)
            
        finally:
            small_cache.shutdown()
    
    def test_entry_info(self):
        """Test entry information retrieval."""
        self.cache.put("key1", "value1", ttl_seconds=300, 
                      dependencies=["dep1"], tags=["tag1"])
        
        entry_info = self.cache.get_entry_info("key1")
        self.assertIsNotNone(entry_info)
        self.assertIn('created_at', entry_info)
        self.assertIn('access_count', entry_info)
        self.assertEqual(entry_info['ttl_seconds'], 300)
        self.assertEqual(entry_info['dependencies'], ["dep1"])
        self.assertEqual(entry_info['tags'], ["tag1"])
        
        # Test non-existent key
        self.assertIsNone(self.cache.get_entry_info("nonexistent"))
    
    def test_optimization(self):
        """Test cache optimization."""
        # Add some entries
        for i in range(3):
            self.cache.put(f"key{i}", f"value{i}")
        
        # Add expired entry
        self.cache.put("expired_key", "expired_value", ttl_seconds=1)
        time.sleep(1.1)
        
        initial_size = self.cache.size()
        
        # Run optimization
        report = self.cache.optimize_cache()
        
        self.assertIn('initial_entries', report)
        self.assertIn('final_entries', report)
        self.assertIn('entries_removed', report)
        self.assertIn('optimization_timestamp', report)
        
        # Should have removed expired entry
        self.assertLess(self.cache.size(), initial_size)
    
    def test_clear(self):
        """Test cache clearing."""
        # Add some entries
        for i in range(3):
            self.cache.put(f"key{i}", f"value{i}")
        
        self.assertEqual(self.cache.size(), 3)
        
        # Clear cache
        self.cache.clear()
        
        self.assertEqual(self.cache.size(), 0)
        for i in range(3):
            self.assertIsNone(self.cache.get(f"key{i}"))
    
    def test_concurrent_access(self):
        """Test thread-safe concurrent access."""
        def worker(thread_id):
            for i in range(10):
                key = f"thread{thread_id}_key{i}"
                value = f"thread{thread_id}_value{i}"
                self.cache.put(key, value)
                retrieved = self.cache.get(key)
                self.assertEqual(retrieved, value)
        
        # Create multiple threads
        threads = []
        for i in range(5):
            thread = threading.Thread(target=worker, args=(i,))
            threads.append(thread)
            thread.start()
        
        # Wait for all threads to complete
        for thread in threads:
            thread.join()
        
        # Verify cache state is consistent
        self.assertGreater(self.cache.size(), 0)
        metrics = self.cache.get_metrics()
        self.assertGreater(metrics.cache_hits, 0)


class TestFuzzyEngineCache(unittest.TestCase):
    """Test FuzzyEngineCache main cache manager."""
    
    def setUp(self):
        """Set up test cache manager."""
        self.config = CacheSettings(
            enable_membership_cache=True,
            enable_rule_cache=True,
            enable_defuzzification_cache=True,
            cache_ttl_seconds=300,
            max_cache_entries=100,
            cache_cleanup_interval_seconds=1,
            cache_hit_ratio_threshold=0.8,
            cache_memory_limit_mb=10.0
        )
        self.cache_manager = FuzzyEngineCache(self.config)
    
    def tearDown(self):
        """Clean up test cache manager."""
        self.cache_manager.shutdown()
    
    def test_cache_manager_initialization(self):
        """Test cache manager initialization."""
        self.assertIsNotNone(self.cache_manager.membership_cache)
        self.assertIsNotNone(self.cache_manager.rule_cache)
        self.assertIsNotNone(self.cache_manager.defuzzification_cache)
        self.assertIsNotNone(self.cache_manager.variable_cache)
    
    def test_membership_caching(self):
        """Test membership value caching."""
        # Cache membership value
        self.cache_manager.cache_membership_value("var1_low", 0.75)
        
        # Retrieve cached value
        cached_value = self.cache_manager.get_membership_value("var1_low")
        self.assertEqual(cached_value, 0.75)
        
        # Test non-existent key
        self.assertIsNone(self.cache_manager.get_membership_value("nonexistent"))
    
    def test_rule_caching(self):
        """Test rule result caching."""
        rule_result = {
            "firing_strength": 0.8,
            "output_value": 0.6,
            "rule_id": "rule1"
        }
        
        # Cache rule result
        self.cache_manager.cache_rule_result("rule1_result", rule_result)
        
        # Retrieve cached result
        cached_result = self.cache_manager.get_rule_result("rule1_result")
        self.assertEqual(cached_result, rule_result)
        
        # Test non-existent key
        self.assertIsNone(self.cache_manager.get_rule_result("nonexistent"))
    
    def test_defuzzification_caching(self):
        """Test defuzzification result caching."""
        # Cache defuzzification result
        self.cache_manager.cache_defuzzification_result("defuzz1", 42.5)
        
        # Retrieve cached result
        cached_result = self.cache_manager.get_defuzzification_result("defuzz1")
        self.assertEqual(cached_result, 42.5)
        
        # Test non-existent key
        self.assertIsNone(self.cache_manager.get_defuzzification_result("nonexistent"))
    
    def test_variable_caching(self):
        """Test variable definition caching."""
        variable_def = {
            "name": "temperature",
            "range": [0, 100],
            "membership_functions": ["low", "medium", "high"]
        }
        
        # Cache variable definition
        self.cache_manager.cache_variable_definition("temp_var", variable_def)
        
        # Retrieve cached definition
        cached_def = self.cache_manager.get_variable_definition("temp_var")
        self.assertEqual(cached_def, variable_def)
        
        # Test non-existent key
        self.assertIsNone(self.cache_manager.get_variable_definition("nonexistent"))
    
    def test_dependency_invalidation(self):
        """Test cross-cache dependency invalidation."""
        # Cache values with same dependency
        self.cache_manager.cache_membership_value("key1", 0.5, dependencies=["sensor1"])
        self.cache_manager.cache_rule_result("key2", {"result": "test"}, dependencies=["sensor1"])
        
        # Verify cached
        self.assertIsNotNone(self.cache_manager.get_membership_value("key1"))
        self.assertIsNotNone(self.cache_manager.get_rule_result("key2"))
        
        # Invalidate by dependency
        invalidated = self.cache_manager.invalidate_by_dependency("sensor1")
        self.assertEqual(invalidated, 2)
        
        # Verify invalidated
        self.assertIsNone(self.cache_manager.get_membership_value("key1"))
        self.assertIsNone(self.cache_manager.get_rule_result("key2"))
    
    def test_tag_invalidation(self):
        """Test cross-cache tag invalidation."""
        # Cache values with same tag
        self.cache_manager.cache_membership_value("key1", 0.5, tags=["temperature"])
        self.cache_manager.cache_defuzzification_result("key2", 42.0, tags=["temperature"])
        
        # Verify cached
        self.assertIsNotNone(self.cache_manager.get_membership_value("key1"))
        self.assertIsNotNone(self.cache_manager.get_defuzzification_result("key2"))
        
        # Invalidate by tag
        invalidated = self.cache_manager.invalidate_by_tag("temperature")
        self.assertEqual(invalidated, 2)
        
        # Verify invalidated
        self.assertIsNone(self.cache_manager.get_membership_value("key1"))
        self.assertIsNone(self.cache_manager.get_defuzzification_result("key2"))
    
    def test_global_metrics(self):
        """Test global metrics aggregation."""
        # Add some cache entries
        self.cache_manager.cache_membership_value("key1", 0.5)
        self.cache_manager.cache_rule_result("key2", {"test": "value"})
        
        # Generate some hits and misses
        self.cache_manager.get_membership_value("key1")  # Hit
        self.cache_manager.get_membership_value("nonexistent")  # Miss
        self.cache_manager.get_rule_result("key2")  # Hit
        
        # Get global metrics
        metrics = self.cache_manager.get_global_metrics()
        
        self.assertGreater(metrics.cache_hits, 0)
        self.assertGreater(metrics.cache_misses, 0)
        self.assertGreater(metrics.cache_size, 0)
        self.assertGreater(metrics.hit_rate, 0)
    
    def test_cache_status(self):
        """Test cache status reporting."""
        status = self.cache_manager.get_cache_status()
        
        self.assertIn('global_metrics', status)
        self.assertIn('cache_instances', status)
        
        instances = status['cache_instances']
        self.assertIn('membership', instances)
        self.assertIn('rule', instances)
        self.assertIn('defuzzification', instances)
        self.assertIn('variable', instances)
        
        # All should be enabled based on config
        for instance_name in ['membership', 'rule', 'defuzzification', 'variable']:
            self.assertTrue(instances[instance_name]['enabled'])
    
    def test_optimization(self):
        """Test cache optimization across all instances."""
        # Add some entries
        self.cache_manager.cache_membership_value("key1", 0.5)
        self.cache_manager.cache_rule_result("key2", {"test": "value"})
        
        # Run optimization
        report = self.cache_manager.optimize_all_caches()
        
        self.assertIn('timestamp', report)
        self.assertIn('cache_optimizations', report)
        
        optimizations = report['cache_optimizations']
        self.assertIn('membership', optimizations)
        self.assertIn('rule', optimizations)
        self.assertIn('defuzzification', optimizations)
        self.assertIn('variable', optimizations)
    
    def test_health_check(self):
        """Test cache health monitoring."""
        health = self.cache_manager.health_check()
        
        self.assertIn('status', health)
        self.assertIn('timestamp', health)
        self.assertIn('issues', health)
        self.assertIn('metrics', health)
        
        # Should be healthy initially
        self.assertIn(health['status'], ['healthy', 'degraded', 'critical'])
    
    def test_clear_all_caches(self):
        """Test clearing all cache instances."""
        # Add entries to different caches
        self.cache_manager.cache_membership_value("key1", 0.5)
        self.cache_manager.cache_rule_result("key2", {"test": "value"})
        self.cache_manager.cache_defuzzification_result("key3", 42.0)
        
        # Verify entries exist
        self.assertIsNotNone(self.cache_manager.get_membership_value("key1"))
        self.assertIsNotNone(self.cache_manager.get_rule_result("key2"))
        self.assertIsNotNone(self.cache_manager.get_defuzzification_result("key3"))
        
        # Clear all caches
        self.cache_manager.clear_all_caches()
        
        # Verify all entries are gone
        self.assertIsNone(self.cache_manager.get_membership_value("key1"))
        self.assertIsNone(self.cache_manager.get_rule_result("key2"))
        self.assertIsNone(self.cache_manager.get_defuzzification_result("key3"))
        
        # Verify global metrics reflect empty caches
        metrics = self.cache_manager.get_global_metrics()
        self.assertEqual(metrics.cache_size, 0)
    
    def test_disabled_caches(self):
        """Test behavior with disabled cache types."""
        # Create config with some caches disabled
        disabled_config = CacheSettings(
            enable_membership_cache=False,
            enable_rule_cache=True,
            enable_defuzzification_cache=False,
            cache_ttl_seconds=300,
            max_cache_entries=100,
            cache_cleanup_interval_seconds=60,
            cache_hit_ratio_threshold=0.8,
            cache_memory_limit_mb=10.0
        )
        
        disabled_cache_manager = FuzzyEngineCache(disabled_config)
        
        try:
            # Test disabled membership cache
            disabled_cache_manager.cache_membership_value("key1", 0.5)
            self.assertIsNone(disabled_cache_manager.get_membership_value("key1"))
            
            # Test enabled rule cache
            disabled_cache_manager.cache_rule_result("key2", {"test": "value"})
            self.assertIsNotNone(disabled_cache_manager.get_rule_result("key2"))
            
            # Test disabled defuzzification cache
            disabled_cache_manager.cache_defuzzification_result("key3", 42.0)
            self.assertIsNone(disabled_cache_manager.get_defuzzification_result("key3"))
            
            # Variable cache should always be enabled
            disabled_cache_manager.cache_variable_definition("key4", {"test": "def"})
            self.assertIsNotNone(disabled_cache_manager.get_variable_definition("key4"))
            
        finally:
            disabled_cache_manager.shutdown()


class TestCacheUtilities(unittest.TestCase):
    """Test cache utility functions."""
    
    def test_create_cache_key(self):
        """Test cache key generation."""
        # Test with simple arguments
        key1 = create_cache_key("arg1", "arg2", param1="value1", param2="value2")
        key2 = create_cache_key("arg1", "arg2", param1="value1", param2="value2")
        key3 = create_cache_key("arg1", "arg2", param1="value1", param2="different")
        
        # Same arguments should produce same key
        self.assertEqual(key1, key2)
        
        # Different arguments should produce different keys
        self.assertNotEqual(key1, key3)
        
        # Test with complex objects
        key4 = create_cache_key([1, 2, 3], {"nested": "dict"})
        key5 = create_cache_key([1, 2, 3], {"nested": "dict"})
        
        self.assertEqual(key4, key5)
    
    def test_cache_result_decorator(self):
        """Test cache result decorator."""
        cache = LRUTTLCache(max_size=10)
        
        call_count = 0
        
        @cache_result(
            cache_instance=cache,
            key_generator=lambda x, y: f"func_{x}_{y}",
            ttl_seconds=300
        )
        def expensive_function(x, y):
            nonlocal call_count
            call_count += 1
            return x + y
        
        try:
            # First call should execute function
            result1 = expensive_function(1, 2)
            self.assertEqual(result1, 3)
            self.assertEqual(call_count, 1)
            
            # Second call with same args should use cache
            result2 = expensive_function(1, 2)
            self.assertEqual(result2, 3)
            self.assertEqual(call_count, 1)  # Should not increment
            
            # Call with different args should execute function
            result3 = expensive_function(2, 3)
            self.assertEqual(result3, 5)
            self.assertEqual(call_count, 2)
            
        finally:
            cache.shutdown()


class TestCachePerformance(unittest.TestCase):
    """Test cache performance characteristics."""
    
    def test_large_cache_performance(self):
        """Test performance with large number of entries."""
        cache = LRUTTLCache(
            max_size=10000,
            memory_limit_mb=50.0,
            cleanup_interval_seconds=10
        )
        
        try:
            # Measure insertion time
            start_time = time.time()
            
            for i in range(1000):
                cache.put(f"key{i}", f"value{i}")
            
            insertion_time = time.time() - start_time
            
            # Should be reasonably fast (less than 1 second for 1000 insertions)
            self.assertLess(insertion_time, 1.0)
            
            # Measure retrieval time
            start_time = time.time()
            
            for i in range(1000):
                value = cache.get(f"key{i}")
                self.assertEqual(value, f"value{i}")
            
            retrieval_time = time.time() - start_time
            
            # Should be very fast (less than 0.5 seconds for 1000 retrievals)
            self.assertLess(retrieval_time, 0.5)
            
        finally:
            cache.shutdown()
    
    def test_memory_usage_tracking(self):
        """Test memory usage tracking accuracy."""
        cache = LRUTTLCache(
            max_size=100,
            memory_limit_mb=1.0,
            cleanup_interval_seconds=1
        )
        
        try:
            # Add entries and track memory
            initial_memory = cache._calculate_memory_usage()
            
            # Add some entries
            for i in range(10):
                cache.put(f"key{i}", "x" * 100)  # 100-byte strings
            
            final_memory = cache._calculate_memory_usage()
            
            # Memory usage should have increased
            self.assertGreater(final_memory, initial_memory)
            
            # Get metrics
            metrics = cache.get_metrics()
            self.assertGreater(metrics.cache_memory_usage_mb, 0)
            
        finally:
            cache.shutdown()
    
    def test_concurrent_performance(self):
        """Test performance under concurrent access."""
        cache = LRUTTLCache(
            max_size=1000,
            memory_limit_mb=10.0,
            cleanup_interval_seconds=5
        )
        
        def worker(thread_id, operations=100):
            for i in range(operations):
                key = f"thread{thread_id}_key{i}"
                value = f"thread{thread_id}_value{i}"
                
                # Mix of operations
                cache.put(key, value)
                retrieved = cache.get(key)
                self.assertEqual(retrieved, value)
        
        try:
            start_time = time.time()
            
            # Create multiple threads
            threads = []
            for i in range(10):
                thread = threading.Thread(target=worker, args=(i, 50))
                threads.append(thread)
                thread.start()
            
            # Wait for completion
            for thread in threads:
                thread.join()
            
            total_time = time.time() - start_time
            
            # Should complete reasonably quickly (less than 5 seconds)
            self.assertLess(total_time, 5.0)
            
            # Verify cache is in consistent state
            self.assertGreater(cache.size(), 0)
            metrics = cache.get_metrics()
            self.assertGreater(metrics.cache_hits, 0)
            
        finally:
            cache.shutdown()


if __name__ == '__main__':
    unittest.main()