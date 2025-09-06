import unittest
import numpy as np
from unittest.mock import Mock, patch, MagicMock
from datetime import datetime
from typing import Dict, List, Any

# Import the classes we're testing
from FuzzyService.Infrastructure.FuzzyEngine.FuzzificationEngine import (
    FuzzificationEngine, FuzzificationResult, BatchFuzzificationResult
)
from FuzzyService.Infrastructure.FuzzyEngine.FuzzyEngineConfiguration import (
    FuzzyEngineConfiguration, PerformanceLimits, ValidationSettings, 
    CacheSettings, LoggingSettings, DefuzzificationMethod
)
from FuzzyService.Infrastructure.FuzzyEngine.FuzzyEngineExceptions import (
    FuzzificationException, ValidationException
)
from FuzzyService.Infrastructure.FuzzyEngine.FuzzyEngineMetrics import (
    FuzzyEngineMetrics
)


class TestFuzzificationProcess(unittest.TestCase):
    """Test suite for FuzzificationEngine functionality."""
    
    def setUp(self):
        """Set up test fixtures."""
        # Create basic configuration
        self.config = FuzzyEngineConfiguration(
            performance_limits=PerformanceLimits(),
            validation_settings=ValidationSettings(),
            cache_settings=CacheSettings(
                enable_membership_cache=False,
                enable_rule_cache=False,
                enable_defuzzification_cache=False
            ),
            logging_settings=LoggingSettings()
        )
        
        # Mock membership converter
        self.mock_converter = Mock()
        self.mock_converter.cache = Mock()
        
        # Mock metrics
        self.mock_metrics = Mock(spec=FuzzyEngineMetrics)
        self.mock_metrics.monitor_operation.return_value.__enter__ = Mock()
        self.mock_metrics.monitor_operation.return_value.__exit__ = Mock(return_value=None)
        
        # Create engine instance
        self.engine = FuzzificationEngine(
            config=self.config,
            metrics=self.mock_metrics,
            membership_converter=self.mock_converter
        )
        
        # Sample variable definition with correct parameter format
        self.sample_variable_def = {
            'id': 'temperature',
            'name': 'temperature',
            'min_value': 0.0,
            'max_value': 100.0,
            'sensor_id': 'temp_sensor_1',
            'terms': [
                {
                    'id': 'low',
                    'name': 'Low Temperature',
                    'function_type': 'triangular',
                    'parameters': {'a': 0, 'b': 25, 'c': 50}  # Dict format, not list
                },
                {
                    'id': 'medium',
                    'name': 'Medium Temperature', 
                    'function_type': 'triangular',
                    'parameters': {'a': 25, 'b': 50, 'c': 75}  # Dict format, not list
                },
                {
                    'id': 'high',
                    'name': 'High Temperature',
                    'function_type': 'triangular', 
                    'parameters': {'a': 50, 'b': 75, 'c': 100}  # Dict format, not list
                }
            ]
        }
        
        # Sample sensor readings
        self.sample_sensor_readings = {
            'temp_sensor_1': 35.5,
            'humidity_sensor_1': 65.0
        }
        
        # Mock membership function results
        self.mock_converter.convert_membership_function.return_value = (
            np.linspace(0, 100, 101),  # universe
            np.array([0.0, 0.5, 1.0, 0.5, 0.0] + [0.0] * 96)  # membership values
        )
        
        # Mock time.time() to ensure processing_time > 0
        import time
        original_time = time.time
        time_values = [1000.0, 1000.001]  # Start and end times
        time.time = lambda: time_values.pop(0) if time_values else original_time()
    
    def test_engine_initialization(self):
        """Test FuzzificationEngine initialization."""
        self.assertIsNotNone(self.engine)
        self.assertEqual(self.engine.config, self.config)
        self.assertEqual(self.engine.membership_converter, self.mock_converter)
    
    def test_fuzzify_single_value_basic(self):
        """Test basic single value fuzzification."""
        result = self.engine.fuzzify_single_value(
            crisp_value=35.5,
            variable_definition=self.sample_variable_def,
            variable_id='temperature'
        )
        
        self.assertIsInstance(result, FuzzificationResult)
        self.assertEqual(result.variable_id, 'temperature')
        self.assertEqual(result.crisp_value, 35.5)
        self.assertIsInstance(result.membership_degrees, dict)
        self.assertIsInstance(result.timestamp, datetime)
    
    def test_fuzzify_single_value_edge_cases(self):
        """Test single value fuzzification with edge cases."""
        # Test minimum value
        result_min = self.engine.fuzzify_single_value(
            crisp_value=0.0,
            variable_definition=self.sample_variable_def,
            variable_id='temperature'
        )
        self.assertEqual(result_min.crisp_value, 0.0)
        
        # Test maximum value
        result_max = self.engine.fuzzify_single_value(
            crisp_value=100.0,
            variable_definition=self.sample_variable_def,
            variable_id='temperature'
        )
        self.assertEqual(result_max.crisp_value, 100.0)
    
    def test_fuzzify_single_value_invalid_input(self):
        """Test single value fuzzification with invalid inputs."""
        # Test invalid crisp value
        with self.assertRaises(ValidationException):
            self.engine.fuzzify_single_value(
                crisp_value=float('nan'),
                variable_definition=self.sample_variable_def,
                variable_id='temperature'
            )
        
        # Test invalid variable definition
        with self.assertRaises(ValidationException):
            self.engine.fuzzify_single_value(
                crisp_value=35.5,
                variable_definition="invalid",  # Should be dict
                variable_id='temperature'
            )
        
        # Test empty variable ID
        with self.assertRaises(ValidationException):
            self.engine.fuzzify_single_value(
                crisp_value=35.5,
                variable_definition=self.sample_variable_def,
                variable_id=""  # Empty string
            )
    
    def test_batch_fuzzification_basic(self):
        """Test basic batch fuzzification."""
        fuzzy_variables = [self.sample_variable_def]
        
        result = self.engine.fuzzify_sensor_readings(
            sensor_readings=self.sample_sensor_readings,
            fuzzy_variables=fuzzy_variables,
            system_id='test_system'
        )
        
        self.assertIsInstance(result, BatchFuzzificationResult)
        self.assertIsInstance(result.fuzzified_values, dict)
        self.assertIsInstance(result.timestamp, datetime)
        self.assertTrue(isinstance(result.success, bool))
        self.assertGreaterEqual(result.processing_time, 0)
    
    def test_batch_fuzzification_missing_sensor(self):
        """Test batch fuzzification with missing sensor data."""
        # Create variable that maps to non-existent sensor
        variable_def = self.sample_variable_def.copy()
        variable_def['sensor_id'] = 'non_existent_sensor'
        
        fuzzy_variables = [variable_def]
        
        result = self.engine.fuzzify_sensor_readings(
            sensor_readings=self.sample_sensor_readings,
            fuzzy_variables=fuzzy_variables,
            system_id='test_system'
        )
        
        # Should handle missing sensor gracefully
        self.assertIsInstance(result, BatchFuzzificationResult)
    
    def test_batch_fuzzification_invalid_input(self):
        """Test batch fuzzification with invalid inputs."""
        # Test invalid sensor readings
        with self.assertRaises((ValidationException, FuzzificationException)):
            self.engine.fuzzify_sensor_readings(
                sensor_readings="invalid",  # Should be dict
                fuzzy_variables=[self.sample_variable_def],
                system_id='test_system'
            )
        
        # Test invalid fuzzy variables
        with self.assertRaises((ValidationException, FuzzificationException)):
            self.engine.fuzzify_sensor_readings(
                sensor_readings=self.sample_sensor_readings,
                fuzzy_variables="invalid",  # Should be list
                system_id='test_system'
            )
        
        # Test empty system ID
        with self.assertRaises((ValidationException, FuzzificationException)):
            self.engine.fuzzify_sensor_readings(
                sensor_readings=self.sample_sensor_readings,
                fuzzy_variables=[self.sample_variable_def],
                system_id=""  # Empty string
            )
    
    def test_fuzzification_result_methods(self):
        """Test FuzzificationResult methods."""
        # Create a sample result
        result = FuzzificationResult(
            variable_id='temperature',
            sensor_id='temp_sensor_1',
            crisp_value=35.5,
            membership_degrees={'low': 0.3, 'medium': 0.7, 'high': 0.0},
            processing_time=0.001,
            timestamp=datetime.now()
        )
        
        # Test get_dominant_term
        dominant = result.get_dominant_term()
        self.assertIsNotNone(dominant)
        self.assertEqual(dominant[0], 'medium')  # Highest membership
        self.assertEqual(dominant[1], 0.7)
        
        # Test to_dict
        result_dict = result.to_dict()
        self.assertIsInstance(result_dict, dict)
        self.assertEqual(result_dict['variable_id'], 'temperature')
        self.assertEqual(result_dict['crisp_value'], 35.5)
    
    def test_batch_fuzzification_result_methods(self):
        """Test BatchFuzzificationResult methods."""
        # Create batch result with correct structure
        batch_result = BatchFuzzificationResult(
            fuzzified_values={'temperature': {'low': 0.3, 'medium': 0.7, 'high': 0.0}},
            processing_time=0.005,
            timestamp=datetime.now(),
            success=True,
            warnings=[],
            missing_sensors=[]
        )
        
        # Test to_dict method
        result_dict = batch_result.to_dict()
        self.assertIsInstance(result_dict, dict)
        self.assertIn('fuzzified_values', result_dict)
        self.assertIn('processing_time', result_dict)
        self.assertIn('success', result_dict)
        self.assertIn('variables_processed', result_dict)
        self.assertEqual(result_dict['variables_processed'], 1)


if __name__ == '__main__':
    unittest.main()