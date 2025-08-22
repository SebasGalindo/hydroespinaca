"""Pruebas para el motor de lógica difusa.

Este archivo contiene pruebas completas para el motor fuzzy.
"""

import pytest
import numpy as np
from unittest.mock import MagicMock, patch
from datetime import datetime

from infrastructure.fuzzy_engine import FuzzyVariable, FuzzyRule, FuzzyEngine
from application.dtos import ReadingBatch, ReadingInput
from domain.models import Routine, ActuatorMapping, ActuatorType


class TestFuzzyVariable:
    """Pruebas para la clase FuzzyVariable."""
    
    @pytest.fixture
    def sample_universe(self):
        """Universo de ejemplo para pruebas."""
        return np.arange(0, 101, 1)
    
    @pytest.fixture
    def fuzzy_variable(self, sample_universe):
        """Variable difusa de ejemplo."""
        return FuzzyVariable("temperature", sample_universe)
    
    def test_fuzzy_variable_creation(self, fuzzy_variable, sample_universe):
        """Prueba creación de variable difusa."""
        assert fuzzy_variable.name == "temperature"
        assert np.array_equal(fuzzy_variable.universe, sample_universe)
        assert len(fuzzy_variable.sets) == 0
    
    def test_add_fuzzy_set(self, fuzzy_variable, sample_universe):
        """Prueba agregar conjunto difuso."""
        # Crear conjunto triangular
        membership_func = np.array([0, 0.5, 1, 0.5, 0] + [0] * 96)
        
        fuzzy_variable.add_set("low", membership_func)
        
        assert "low" in fuzzy_variable.sets
        assert np.array_equal(fuzzy_variable.sets["low"], membership_func)
    
    def test_get_membership_existing_set(self, fuzzy_variable):
        """Prueba obtener membresía de conjunto existente."""
        # Crear conjunto simple para prueba
        universe = np.arange(0, 11, 1)
        fuzzy_variable.universe = universe
        membership_func = np.array([1, 0.8, 0.6, 0.4, 0.2, 0, 0, 0, 0, 0, 0])
        fuzzy_variable.add_set("test_set", membership_func)
        
        # Probar membresía en diferentes puntos
        assert fuzzy_variable.get_membership(0, "test_set") == 1.0
        assert fuzzy_variable.get_membership(1, "test_set") == 0.8
        assert fuzzy_variable.get_membership(5, "test_set") == 0.0
    
    def test_get_membership_nonexistent_set(self, fuzzy_variable):
        """Prueba obtener membresía de conjunto inexistente."""
        result = fuzzy_variable.get_membership(50, "nonexistent")
        assert result == 0.0


class TestFuzzyRule:
    """Pruebas para la clase FuzzyRule."""
    
    @pytest.fixture
    def sample_fuzzy_vars(self):
        """Variables difusas de ejemplo."""
        temp_var = FuzzyVariable("temperature", np.arange(0, 101, 1))
        temp_var.add_set("high", np.array([0] * 70 + [0.5, 1] + [1] * 29))
        
        humidity_var = FuzzyVariable("humidity", np.arange(0, 101, 1))
        humidity_var.add_set("low", np.array([1] * 30 + [0.5, 0] + [0] * 69))
        
        return {"temperature": temp_var, "humidity": humidity_var}
    
    @pytest.fixture
    def simple_rule(self):
        """Regla difusa simple."""
        return FuzzyRule(
            rule_id="rule_001",
            antecedents=[("temperature", "high")],
            consequent=("fan", "high"),
            weight=1.0
        )
    
    @pytest.fixture
    def complex_rule(self):
        """Regla difusa con múltiples antecedentes."""
        return FuzzyRule(
            rule_id="rule_002",
            antecedents=[("temperature", "high"), ("humidity", "low")],
            consequent=("fan", "medium"),
            weight=0.8
        )
    
    def test_fuzzy_rule_creation(self, simple_rule):
        """Prueba creación de regla difusa."""
        assert simple_rule.rule_id == "rule_001"
        assert simple_rule.antecedents == [("temperature", "high")]
        assert simple_rule.consequent == ("fan", "high")
        assert simple_rule.weight == 1.0
    
    def test_evaluate_simple_rule(self, simple_rule, sample_fuzzy_vars):
        """Prueba evaluación de regla simple."""
        input_values = {"temperature": 80}
        
        activation = simple_rule.evaluate(sample_fuzzy_vars, input_values)
        
        # Temperatura 80 debería tener membresía alta en "high"
        assert activation > 0.0
        assert activation <= 1.0
    
    def test_evaluate_complex_rule(self, complex_rule, sample_fuzzy_vars):
        """Prueba evaluación de regla con múltiples antecedentes."""
        input_values = {"temperature": 80, "humidity": 20}
        
        activation = complex_rule.evaluate(sample_fuzzy_vars, input_values)
        
        # Debería usar operador AND (mínimo) y aplicar peso
        assert activation > 0.0
        assert activation <= 0.8  # Limitado por el peso
    
    def test_evaluate_missing_variable(self, simple_rule, sample_fuzzy_vars):
        """Prueba evaluación con variable faltante."""
        input_values = {"humidity": 50}  # Falta temperature
        
        activation = simple_rule.evaluate(sample_fuzzy_vars, input_values)
        
        assert activation == 0.0
    
    def test_evaluate_empty_antecedents(self, sample_fuzzy_vars):
        """Prueba evaluación con antecedentes vacíos."""
        empty_rule = FuzzyRule(
            rule_id="empty",
            antecedents=[],
            consequent=("fan", "off")
        )
        
        activation = empty_rule.evaluate(sample_fuzzy_vars, {"temperature": 50})
        
        assert activation == 0.0


class TestFuzzyEngine:
    """Pruebas para la clase FuzzyEngine."""
    
    @pytest.fixture
    def fuzzy_engine(self):
        """Motor difuso de ejemplo."""
        return FuzzyEngine()
    
    def test_fuzzy_engine_initialization(self, fuzzy_engine):
        """Prueba inicialización del motor difuso."""
        # Debería tener variables por defecto
        assert "ph" in fuzzy_engine.fuzzy_vars
        assert "ec" in fuzzy_engine.fuzzy_vars
        assert "temperatura" in fuzzy_engine.fuzzy_vars
        assert "intensidad" in fuzzy_engine.fuzzy_vars
        
        # Debería tener reglas por defecto
        assert len(fuzzy_engine.rules) > 0
    
    def test_evaluate_with_valid_inputs(self, fuzzy_engine):
        """Prueba evaluación con entradas válidas."""
        inputs = {
            "ph": 6.0,
            "ec": 1.5,
            "temperatura": 24.0
        }
        
        outputs = fuzzy_engine.evaluate(inputs)
        
        assert "intensidad" in outputs
        assert 0.0 <= outputs["intensidad"] <= 100.0
    
    def test_evaluate_with_edge_values(self, fuzzy_engine):
        """Prueba evaluación con valores en los límites."""
        # Valores en los límites inferiores
        inputs_low = {
            "ph": 4.0,
            "ec": 0.5,
            "temperatura": 15.0
        }
        
        outputs_low = fuzzy_engine.evaluate(inputs_low)
        assert "intensidad" in outputs_low
        
        # Valores en los límites superiores
        inputs_high = {
            "ph": 8.0,
            "ec": 3.0,
            "temperatura": 35.0
        }
        
        outputs_high = fuzzy_engine.evaluate(inputs_high)
        assert "intensidad" in outputs_high
    
    def test_evaluate_with_missing_inputs(self, fuzzy_engine):
        """Prueba evaluación con entradas faltantes."""
        inputs = {"ph": 6.0}  # Faltan ec y temperatura
        
        outputs = fuzzy_engine.evaluate(inputs)
        
        # Debería manejar entradas faltantes graciosamente
        assert "intensidad" in outputs
        assert outputs["intensidad"] >= 0.0
    
    def test_evaluate_with_empty_inputs(self, fuzzy_engine):
        """Prueba evaluación con entradas vacías."""
        outputs = fuzzy_engine.evaluate({})
        
        assert "intensidad" in outputs
        assert outputs["intensidad"] == 0.0
    
    def test_add_variable(self, fuzzy_engine):
        """Prueba agregar variable al motor."""
        new_var = FuzzyVariable("pressure", np.arange(0, 101, 1))
        
        fuzzy_engine.add_variable(new_var)
        
        assert "pressure" in fuzzy_engine.fuzzy_vars
        assert fuzzy_engine.fuzzy_vars["pressure"] == new_var
    
    def test_add_rule(self, fuzzy_engine):
        """Prueba agregar regla al motor."""
        new_rule = FuzzyRule(
            rule_id="test_rule",
            antecedents=[("ph", "optimo")],
            consequent=("intensidad", "medio")
        )
        
        initial_count = len(fuzzy_engine.rules)
        fuzzy_engine.add_rule(new_rule)
        
        assert len(fuzzy_engine.rules) == initial_count + 1
        assert new_rule in fuzzy_engine.rules
    
    def test_get_variable_info_existing(self, fuzzy_engine):
        """Prueba obtener información de variable existente."""
        info = fuzzy_engine.get_variable_info("ph")
        
        assert info is not None
        assert info["name"] == "ph"
        assert "universe_range" in info
        assert "sets" in info
        assert len(info["sets"]) > 0
    
    def test_get_variable_info_nonexistent(self, fuzzy_engine):
        """Prueba obtener información de variable inexistente."""
        info = fuzzy_engine.get_variable_info("nonexistent")
        
        assert info is None
    
    def test_process_readings_integration(self, fuzzy_engine):
        """Prueba integración con procesamiento de lecturas."""
        # Simular lecturas de sensores
        readings = ReadingBatch(
            esp32Id="test_esp32",
            timestamp=datetime.now(),
            readings=[
                ReadingInput(variableId="ph_sensor", value=6.2),
                ReadingInput(variableId="ec_sensor", value=1.8),
                ReadingInput(variableId="temp_sensor", value=25.0)
            ]
        )
        
        # Simular rutina con mapeos de actuadores
        routine = Routine(
            id="test_routine",
            name="Test Routine",
            actuator_mappings=[
                ActuatorMapping(
                    output_name="fan_power",
                    actuatorId="fan_001",
                    esp32Id="test_esp32",
                    actuator_type=ActuatorType.VARIABLE
                )
            ]
        )
        
        # Preparar entradas para el motor difuso
        fuzzy_inputs = {
            "ph": 6.2,
            "ec": 1.8,
            "temperatura": 25.0
        }
        
        outputs = fuzzy_engine.evaluate(fuzzy_inputs)
        
        assert "intensidad" in outputs
        assert isinstance(outputs["intensidad"], (int, float))
        assert 0.0 <= outputs["intensidad"] <= 100.0