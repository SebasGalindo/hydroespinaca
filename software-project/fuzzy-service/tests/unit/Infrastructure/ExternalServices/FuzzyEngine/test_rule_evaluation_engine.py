import pytest
import asyncio
from unittest.mock import Mock, patch
from datetime import datetime, timezone
from typing import List, Dict, Any

from FuzzyService.Infrastructure.ExternalServices.FuzzyEngine.RuleEvaluationEngine import (
    RuleEvaluationEngine,
    RuleActivationResult,
    BatchRuleEvaluationResult
)
from FuzzyService.Infrastructure.ExternalServices.FuzzyEngine.FuzzificationTypes import FuzzificationResult
from FuzzyService.Domain.Entities.fuzzy_system import FuzzySystem
from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId, FuzzyRuleId
from FuzzyService.Domain.ValueObjects.OperatorsConfig import OperatorsConfig
from FuzzyService.Domain.Enums.OperatorMethods import AndOperatorMethod, OrOperatorMethod
from FuzzyService.Domain.Errors.DomainErrors import ValidationError


class TestRuleEvaluationEngine:
    """Tests para el motor de evaluación de reglas fuzzy."""
    
    @pytest.fixture
    def engine(self):
        """Fixture del motor de evaluación."""
        return RuleEvaluationEngine()
    
    @pytest.fixture
    def mock_system(self):
        """Fixture de sistema fuzzy mock."""
        system = Mock(spec=FuzzySystem)
        system.id = FuzzySystemId.generate()
        system.status = 'active'
        
        # Configuración de operadores
        operators_config = Mock(spec=OperatorsConfig)
        operators_config.and_method = AndOperatorMethod.MIN
        operators_config.or_method = OrOperatorMethod.MAX
        system.operators = operators_config
        
        return system
    
    @pytest.fixture
    def mock_rules(self):
        """Fixture de reglas mock."""
        # Regla simple con una condición
        rule1 = Mock(spec=FuzzyRule)
        rule1.id = FuzzyRuleId.generate()
        rule1.name = "Regla Temperatura Alta"
        rule1.conditions = [
            {
                "variableId": "temperatura_id_001",
                "value": "alta"
            }
        ]
        rule1.connectors = []
        rule1.consequent = "routine_1"
        
        # Regla con múltiples condiciones y conectores
        rule2 = Mock(spec=FuzzyRule)
        rule2.id = FuzzyRuleId.generate()
        rule2.name = "Regla Temperatura Alta Y Humedad Baja"
        rule2.conditions = [
            {
                "variableId": "temperatura_id_001",
                "value": "alta"
            },
            {
                "variableId": "humedad_id_001",
                "value": "baja"
            }
        ]
        rule2.connectors = ["AND"]
        rule2.consequent = "routine_2"
        
        # Regla con OR
        rule3 = Mock(spec=FuzzyRule)
        rule3.id = FuzzyRuleId.generate()
        rule3.name = "Regla Temperatura Baja O Humedad Alta"
        rule3.conditions = [
            {
                "variableId": "temperatura_id_001",
                "value": "baja"
            },
            {
                "variableId": "humedad_id_001",
                "value": "alta"
            }
        ]
        rule3.connectors = ["OR"]
        rule3.consequent = "routine_3"
        
        return [rule1, rule2, rule3]
    
    @pytest.fixture
    def mock_fuzzification_results(self):
        """Fixture de resultados de fuzzificación mock."""
        # Resultado para temperatura
        temp_result = FuzzificationResult("temperatura", "sensor_temp_001", 28.5, "temperatura_id_001")
        temp_result.add_term_activation("baja", 0.1)
        temp_result.add_term_activation("media", 0.3)
        temp_result.add_term_activation("alta", 0.8)
        
        # Resultado para humedad
        hum_result = FuzzificationResult("humedad", "sensor_hum_001", 35.0, "humedad_id_001")
        hum_result.add_term_activation("baja", 0.7)
        hum_result.add_term_activation("media", 0.2)
        hum_result.add_term_activation("alta", 0.1)
        
        return [temp_result, hum_result]
    
    @pytest.mark.asyncio
    async def test_evaluate_rules_success(self, engine, mock_system, mock_rules, mock_fuzzification_results):
        """Test de evaluación exitosa de reglas."""
        result = await engine.evaluate_rules(
            system=mock_system,
            rules=mock_rules,
            fuzzification_results=mock_fuzzification_results
        )
        
        assert isinstance(result, BatchRuleEvaluationResult)
        assert result.rules_evaluated == 3
        assert result.rules_activated >= 1  # Al menos una regla debería activarse
        assert result.total_processing_time_ms > 0
        assert len(result.rule_results) == 3
    
    @pytest.mark.asyncio
    async def test_evaluate_single_condition_rule(self, engine, mock_system, mock_fuzzification_results):
        """Test de evaluación de regla con una sola condición."""
        # Crear regla simple
        rule = Mock(spec=FuzzyRule)
        rule.id = FuzzyRuleId.generate()
        rule.name = "Regla Simple"
        rule.conditions = [
            {
                "variableId": "temperatura_id_001",
                "value": "alta"
            }
        ]
        rule.connectors = []
        rule.consequent = "test_routine_id"
        
        result = await engine.evaluate_rules(
            system=mock_system,
            rules=[rule],
            fuzzification_results=mock_fuzzification_results
        )
        
        assert result.rules_evaluated == 1
        rule_result = result.rule_results[0]
        assert rule_result.firing_strength == 0.8  # Grado de pertenencia de "alta"
        assert rule_result.is_activated is True
        assert len(rule_result.condition_evaluations) == 1
    
    @pytest.mark.asyncio
    async def test_evaluate_and_connector(self, engine, mock_system, mock_fuzzification_results):
        """Test de evaluación con conector AND."""
        # Crear regla con AND
        rule = Mock(spec=FuzzyRule)
        rule.id = FuzzyRuleId.generate()
        rule.name = "Regla AND"
        rule.conditions = [
            {
                "variableId": "temperatura_id_001",
                "value": "alta"  # 0.8
            },
            {
                "variableId": "humedad_id_001",
                "value": "baja"   # 0.7
            }
        ]
        rule.connectors = ["AND"]
        rule.consequent = "test_routine_and"
        
        result = await engine.evaluate_rules(
            system=mock_system,
            rules=[rule],
            fuzzification_results=mock_fuzzification_results
        )
        
        rule_result = result.rule_results[0]
        # Con AND y método MIN: min(0.8, 0.7) = 0.7
        assert rule_result.firing_strength == 0.7
        assert rule_result.is_activated is True
    
    @pytest.mark.asyncio
    async def test_evaluate_or_connector(self, engine, mock_system, mock_fuzzification_results):
        """Test de evaluación con conector OR."""
        # Crear regla con OR
        rule = Mock(spec=FuzzyRule)
        rule.id = FuzzyRuleId.generate()
        rule.name = "Regla OR"
        rule.conditions = [
            {
                "variableId": "temperatura_id_001",
                "value": "baja"   # 0.1
            },
            {
                "variableId": "humedad_id_001",
                "value": "alta"   # 0.1
            }
        ]
        rule.connectors = ["OR"]
        rule.consequent = "test_routine_or"
        
        result = await engine.evaluate_rules(
            system=mock_system,
            rules=[rule],
            fuzzification_results=mock_fuzzification_results
        )
        
        rule_result = result.rule_results[0]
        # Con OR y método MAX: max(0.1, 0.1) = 0.1
        assert rule_result.firing_strength == 0.1
        assert rule_result.is_activated is True
    
    @pytest.mark.asyncio
    async def test_evaluate_prod_and_method(self, engine, mock_fuzzification_results):
        """Test de evaluación con método AND PROD."""
        # Sistema con método PROD para AND
        system = Mock(spec=FuzzySystem)
        system.id = FuzzySystemId.generate()
        system.status = 'active'
        
        operators_config = Mock(spec=OperatorsConfig)
        operators_config.and_method = AndOperatorMethod.PROD
        operators_config.or_method = OrOperatorMethod.MAX
        system.operators = operators_config
        
        # Regla con AND
        rule = Mock(spec=FuzzyRule)
        rule.id = FuzzyRuleId.generate()
        rule.name = "Regla PROD"
        rule.conditions = [
            {
                "variableId": "temperatura_id_001",
                "value": "alta"  # 0.8
            },
            {
                "variableId": "humedad_id_001",
                "value": "baja"   # 0.7
            }
        ]
        rule.connectors = ["AND"]
        rule.consequent = "test_routine_prod"
        
        result = await engine.evaluate_rules(
            system=system,
            rules=[rule],
            fuzzification_results=mock_fuzzification_results
        )
        
        rule_result = result.rule_results[0]
        # Con AND y método PROD: 0.8 * 0.7 = 0.56
        assert abs(rule_result.firing_strength - 0.56) < 0.001
    
    @pytest.mark.asyncio
    async def test_evaluate_inactive_system(self, engine, mock_rules, mock_fuzzification_results):
        """Test de evaluación con sistema inactivo."""
        # Sistema inactivo
        system = Mock(spec=FuzzySystem)
        system.id = FuzzySystemId.generate()
        system.status = 'inactive'
        
        with pytest.raises(ValidationError, match="no está operativo"):
            await engine.evaluate_rules(
                system=system,
                rules=mock_rules,
                fuzzification_results=mock_fuzzification_results
            )
    
    @pytest.mark.asyncio
    async def test_evaluate_empty_rules(self, engine, mock_system, mock_fuzzification_results):
        """Test de evaluación con lista vacía de reglas."""
        result = await engine.evaluate_rules(
            system=mock_system,
            rules=[],
            fuzzification_results=mock_fuzzification_results
        )
        
        assert result.rules_evaluated == 0
        assert result.rules_activated == 0
        assert len(result.rule_results) == 0
    
    @pytest.mark.asyncio
    async def test_evaluate_missing_variable(self, engine, mock_system, mock_fuzzification_results):
        """Test de evaluación con variable faltante en fuzzificación."""
        # Regla que requiere variable no presente en fuzzificación
        rule = Mock(spec=FuzzyRule)
        rule.id = FuzzyRuleId.generate()
        rule.name = "Regla Variable Faltante"
        rule.conditions = [
            {
                "variableId": "presion_id_001",  # Variable no presente
                "value": "alta"
            }
        ]
        rule.connectors = []
        rule.consequent = "test_routine_missing_var"
        
        result = await engine.evaluate_rules(
            system=mock_system,
            rules=[rule],
            fuzzification_results=mock_fuzzification_results
        )
        
        rule_result = result.rule_results[0]
        assert rule_result.firing_strength == 0.0
        assert rule_result.is_activated is False
    
    @pytest.mark.asyncio
    async def test_evaluate_missing_term(self, engine, mock_system, mock_fuzzification_results):
        """Test de evaluación con término faltante."""
        # Regla que requiere término no presente
        rule = Mock(spec=FuzzyRule)
        rule.id = FuzzyRuleId.generate()
        rule.name = "Regla Término Faltante"
        rule.conditions = [
            {
                "variableId": "temperatura_id_001",
                "value": "extrema"  # Término no presente
            }
        ]
        rule.connectors = []
        rule.consequent = "test_routine_missing_term"
        
        result = await engine.evaluate_rules(
            system=mock_system,
            rules=[rule],
            fuzzification_results=mock_fuzzification_results
        )
        
        rule_result = result.rule_results[0]
        assert rule_result.firing_strength == 0.0
        assert rule_result.is_activated is False
    
    def test_rule_activation_result_creation(self):
        """Test de creación de resultado de activación de regla."""
        result = RuleActivationResult("rule_123", "Test Rule")
        
        assert result.rule_id == "rule_123"
        assert result.rule_name == "Test Rule"
        assert result.firing_strength == 0.0
        assert result.is_activated is False
        assert len(result.condition_evaluations) == 0
        
        # Agregar evaluación de condición
        result.add_condition_evaluation("temp", "alta", 0.8, 25.5)
        assert len(result.condition_evaluations) == 1
        
        condition = result.condition_evaluations[0]
        assert condition["variable_name"] == "temp"
        assert condition["term_label"] == "alta"
        assert condition["membership_degree"] == 0.8
        assert condition["sensor_value"] == 25.5
        
        # Establecer firing strength
        result.set_firing_strength(0.75)
        assert result.firing_strength == 0.75
        assert result.is_activated is True
    
    def test_batch_result_management(self):
        """Test de gestión de resultados en lote."""
        batch = BatchRuleEvaluationResult()
        
        # Agregar resultado activado
        result1 = RuleActivationResult("rule_1", "Rule 1")
        result1.set_firing_strength(0.8)
        batch.add_rule_result(result1)
        
        # Agregar resultado no activado
        result2 = RuleActivationResult("rule_2", "Rule 2")
        result2.set_firing_strength(0.0)
        batch.add_rule_result(result2)
        
        assert batch.rules_evaluated == 2
        assert batch.rules_activated == 1
        assert len(batch.rule_results) == 2
        
        activated = batch.get_activated_rules()
        assert len(activated) == 1
        assert activated[0].rule_id == "rule_1"
    
    def test_firing_strength_clamping(self):
        """Test de limitación del firing strength entre 0 y 1."""
        result = RuleActivationResult("rule_test", "Test")
        
        # Valor negativo
        result.set_firing_strength(-0.5)
        assert result.firing_strength == 0.0
        
        # Valor mayor a 1
        result.set_firing_strength(1.5)
        assert result.firing_strength == 1.0
        
        # Valor válido
        result.set_firing_strength(0.7)
        assert result.firing_strength == 0.7