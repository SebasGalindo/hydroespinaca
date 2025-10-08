"""
Unit tests para consecuentes directos Mamdani y agregación multi-regla.
"""

import pytest
import numpy as np
from unittest.mock import Mock, AsyncMock
from datetime import datetime, timezone

from FuzzyService.Domain.Entities.fuzzy_variable import FuzzyVariable
from FuzzyService.Domain.Entities.fuzzy_term import FuzzyTerm
from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule
from FuzzyService.Domain.Entities.rule_consequent import RuleConsequent
from FuzzyService.Domain.ValueObjects.DomainId import (
    FuzzyVariableId,
    FuzzyTermId,
    FuzzyRuleId,
    FuzzySystemId
)
from FuzzyService.Domain.ValueObjects.MembershipFunction import MembershipFunction
from FuzzyService.Domain.Enums.MembershipFunctionType import MembershipFunctionType
from FuzzyService.Domain.Enums import LogicalOperator, RuleConnector
from FuzzyService.Infrastructure.ExternalServices.FuzzyEngine.ScikitFuzzyEngine import ScikitFuzzyEngine
from FuzzyService.Infrastructure.ExternalServices.FuzzyEngine.FuzzyResultTypes import (
    BatchRuleEvaluationResult,
    RuleActivationResult
)
from medyator import Medyator


class TestRuleConsequent:
    """Tests para la entidad RuleConsequent."""

    def test_create_valid_consequent(self):
        """Test: crear consecuente válido."""
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId("68e05364d86d6edc39982875"),
            terms=[
                FuzzyTermId("68e05364d86d6edc39982890"),
                FuzzyTermId("68e05364d86d6edc39982891")
            ],
            aggregation_method="max"
        )

        assert consequent.variable_id == FuzzyVariableId("68e05364d86d6edc39982875")
        assert len(consequent.terms) == 2
        assert consequent.aggregation_method == "max"

    def test_consequent_requires_terms(self):
        """Test: consecuente requiere al menos un término."""
        with pytest.raises(ValueError, match="debe tener al menos un término"):
            RuleConsequent(
                variable_id=FuzzyVariableId("68e05364d86d6edc39982875"),
                terms=[],  # ❌ Vacío
                aggregation_method="max"
            )

    def test_consequent_no_duplicate_terms(self):
        """Test: no se permiten términos duplicados."""
        with pytest.raises(ValueError, match="duplicados"):
            RuleConsequent(
                variable_id=FuzzyVariableId("68e05364d86d6edc39982875"),
                terms=[
                    FuzzyTermId("68e05364d86d6edc39982890"),
                    FuzzyTermId("68e05364d86d6edc39982890")  # ❌ Duplicado
                ],
                aggregation_method="max"
            )

    def test_valid_aggregation_methods(self):
        """Test: métodos de agregación válidos."""
        valid_methods = ["max", "sum", "probabilistic_or"]

        for method in valid_methods:
            consequent = RuleConsequent(
                variable_id=FuzzyVariableId("68e05364d86d6edc39982875"),
                terms=[FuzzyTermId("68e05364d86d6edc39982890")],
                aggregation_method=method
            )
            assert consequent.aggregation_method == method

    def test_invalid_aggregation_method(self):
        """Test: método de agregación inválido."""
        with pytest.raises(ValueError, match="aggregation_method inválido"):
            RuleConsequent(
                variable_id=FuzzyVariableId("68e05364d86d6edc39982875"),
                terms=[FuzzyTermId("68e05364d86d6edc39982890")],
                aggregation_method="invalid_method"
            )

    def test_consequent_serialization(self):
        """Test: serialización de consecuente."""
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId("68e05364d86d6edc39982875"),
            terms=[FuzzyTermId("68e05364d86d6edc39982890")],
            aggregation_method="max"
        )

        data = consequent.to_dict()

        assert data["variable_id"] == "68e05364d86d6edc39982875"
        assert len(data["terms"]) == 1
        assert data["aggregation_method"] == "max"

        # Round-trip
        restored = RuleConsequent.from_dict(data)
        assert str(restored.variable_id) == str(consequent.variable_id)
        assert len(restored.terms) == len(consequent.terms)


class TestFuzzyRuleWithConsequents:
    """Tests para FuzzyRule con soporte de consecuentes Mamdani."""

    def test_create_rule_with_consequents(self):
        """Test: crear regla con consecuentes."""
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId("68e05364d86d6edc39982875"),
            terms=[FuzzyTermId("68e05364d86d6edc39982890")],
            aggregation_method="max"
        )

        rule = FuzzyRule(
            name="Test Rule",
            system_id=FuzzySystemId("68e05364d86d6edc39982860"),
            conditions=[
                {
                    "variableId": FuzzyVariableId("68e05364d86d6edc39982870"),
                    "operator": LogicalOperator.IS,
                    "value": "alto"
                }
            ],
            connectors=[],
            consequents=[consequent]
        )

        assert rule.has_consequents() == True
        assert len(rule.consequents) == 1
        assert rule.get_consequent_count() == 1

    def test_rule_requires_consequents(self):
        """Test: regla requiere al menos un consecuente."""
        with pytest.raises(ValueError, match="debe tener al menos un consecuente"):
            FuzzyRule(
                name="Test Rule",
                system_id=FuzzySystemId("68e05364d86d6edc39982860"),
                conditions=[
                    {
                        "variableId": FuzzyVariableId("68e05364d86d6edc39982870"),
                        "operator": LogicalOperator.IS,
                        "value": "alto"
                    }
                ],
                connectors=[],
                consequents=[]  # ❌ Vacío
            )

    def test_add_consequent_to_rule(self):
        """Test: agregar consecuente a regla."""
        consequent1 = RuleConsequent(
            variable_id=FuzzyVariableId("68e05364d86d6edc39982875"),
            terms=[FuzzyTermId("68e05364d86d6edc39982890")],
            aggregation_method="max"
        )

        rule = FuzzyRule(
            name="Test Rule",
            system_id=FuzzySystemId("68e05364d86d6edc39982860"),
            conditions=[
                {
                    "variableId": FuzzyVariableId("68e05364d86d6edc39982870"),
                    "operator": LogicalOperator.IS,
                    "value": "alto"
                }
            ],
            connectors=[],
            consequents=[consequent1]
        )

        consequent2 = RuleConsequent(
            variable_id=FuzzyVariableId("68e05364d86d6edc39982876"),  # Diferente variable
            terms=[FuzzyTermId("68e05364d86d6edc39982891")],
            aggregation_method="max"
        )

        rule.add_consequent(consequent2)

        assert len(rule.consequents) == 2
        assert rule.has_consequents() == True

    def test_cannot_add_duplicate_variable_consequent(self):
        """Test: no se puede agregar consecuente duplicado para misma variable."""
        consequent1 = RuleConsequent(
            variable_id=FuzzyVariableId("68e05364d86d6edc39982875"),
            terms=[FuzzyTermId("68e05364d86d6edc39982890")],
            aggregation_method="max"
        )

        rule = FuzzyRule(
            name="Test Rule",
            system_id=FuzzySystemId("68e05364d86d6edc39982860"),
            conditions=[
                {
                    "variableId": FuzzyVariableId("68e05364d86d6edc39982870"),
                    "operator": LogicalOperator.IS,
                    "value": "alto"
                }
            ],
            connectors=[],
            consequents=[consequent1]
        )

        consequent2 = RuleConsequent(
            variable_id=FuzzyVariableId("68e05364d86d6edc39982875"),  # ❌ Misma variable
            terms=[FuzzyTermId("68e05364d86d6edc39982891")],
            aggregation_method="max"
        )

        with pytest.raises(ValueError, match="Ya existe un consecuente"):
            rule.add_consequent(consequent2)

    def test_remove_consequent_from_rule(self):
        """Test: remover consecuente de regla."""
        consequent1 = RuleConsequent(
            variable_id=FuzzyVariableId("68e05364d86d6edc39982875"),
            terms=[FuzzyTermId("68e05364d86d6edc39982890")],
            aggregation_method="max"
        )

        consequent2 = RuleConsequent(
            variable_id=FuzzyVariableId("68e05364d86d6edc39982876"),
            terms=[FuzzyTermId("68e05364d86d6edc39982891")],
            aggregation_method="max"
        )

        rule = FuzzyRule(
            name="Test Rule",
            system_id=FuzzySystemId("68e05364d86d6edc39982860"),
            conditions=[
                {
                    "variableId": FuzzyVariableId("68e05364d86d6edc39982870"),
                    "operator": LogicalOperator.IS,
                    "value": "alto"
                }
            ],
            connectors=[],
            consequents=[consequent1, consequent2]
        )

        assert len(rule.consequents) == 2

        # Remover un consecuente
        rule.remove_consequent(FuzzyVariableId("68e05364d86d6edc39982875"))

        assert len(rule.consequents) == 1
        assert rule.get_consequent_for_variable(FuzzyVariableId("68e05364d86d6edc39982876")) is not None
        assert rule.get_consequent_for_variable(FuzzyVariableId("68e05364d86d6edc39982875")) is None


class TestMamdaniDefuzzification:
    """Tests para defuzzificación Mamdani con agregación multi-regla."""

    @pytest.fixture
    def mediator(self):
        """Mock mediator."""
        return Mock(spec=Medyator)

    @pytest.fixture
    def fuzzy_engine(self, mediator):
        """Instancia del motor fuzzy."""
        return ScikitFuzzyEngine(mediator=mediator)

    @pytest.fixture
    def output_variable_pwm(self):
        """Variable PWM de salida."""
        return FuzzyVariable(
            id=FuzzyVariableId("68e05364d86d6edc39982875"),
            name="Potencia del Ventilador",
            variable_type="output",
            actuator_type="PWM",
            universe_min=0.0,
            universe_max=100.0,
            defuzzification_threshold=50.0,
            reference_id="68e04314d86d6edc39982869"
        )

    @pytest.fixture
    def terms_pwm(self, output_variable_pwm):
        """Términos fuzzy para variable PWM."""
        return [
            FuzzyTerm(
                id=FuzzyTermId("68e05364d86d6edc39982890"),
                variable_id=output_variable_pwm.id,
                label="potenciaBaja",
                membership_function=MembershipFunction(
                    function_type=MembershipFunctionType.TRIANGULAR,
                    parameters=[0.0, 20.0, 40.0],
                    universe_min=0.0,
                    universe_max=100.0
                )
            ),
            FuzzyTerm(
                id=FuzzyTermId("68e05364d86d6edc39982891"),
                variable_id=output_variable_pwm.id,
                label="potenciaAlta",
                membership_function=MembershipFunction(
                    function_type=MembershipFunctionType.TRIANGULAR,
                    parameters=[60.0, 80.0, 100.0],
                    universe_min=0.0,
                    universe_max=100.0
                )
            )
        ]

    @pytest.mark.asyncio
    async def test_defuzzify_from_consequents_single_rule(
        self, fuzzy_engine, output_variable_pwm, terms_pwm
    ):
        """Test: defuzzificación Mamdani con una sola regla."""
        # Crear regla con consecuente
        consequent = RuleConsequent(
            variable_id=output_variable_pwm.id,
            terms=[terms_pwm[1].id],  # potenciaAlta
            aggregation_method="max"
        )

        rule = FuzzyRule(
            id=FuzzyRuleId("68e05364d86d6edc39982900"),
            name="Test Rule",
            system_id=FuzzySystemId("68e05364d86d6edc39982860"),
            conditions=[
                {
                    "variableId": FuzzyVariableId("68e05364d86d6edc39982870"),
                    "operator": LogicalOperator.IS,
                    "value": "alto"
                }
            ],
            connectors=[],
            consequents=[consequent]
        )

        # Crear resultado de evaluación de reglas
        rule_result = RuleActivationResult(
            rule_id=str(rule.id),
            rule_name=rule.name
        )
        rule_result.set_firing_strength(0.8)

        batch_result = BatchRuleEvaluationResult()
        batch_result.add_rule_result(rule_result)

        # Defuzzificar
        defuzzified_values = await fuzzy_engine.defuzzify_from_consequents(
            fuzzy_rules=[rule],
            output_variables=[output_variable_pwm],
            rule_evaluation_result=batch_result,
            all_terms=terms_pwm
        )

        variable_id = str(output_variable_pwm.id)
        assert variable_id in defuzzified_values

        crisp_value = defuzzified_values[variable_id]

        # Valor debe estar cerca del centroide de potenciaAlta (≈80)
        assert 70.0 <= crisp_value <= 90.0

    @pytest.mark.asyncio
    async def test_defuzzify_from_consequents_multi_rule_aggregation(
        self, fuzzy_engine, output_variable_pwm, terms_pwm
    ):
        """Test: agregación multi-regla para misma variable."""
        # Regla 1: activa potenciaBaja con firing 0.6
        consequent1 = RuleConsequent(
            variable_id=output_variable_pwm.id,
            terms=[terms_pwm[0].id],  # potenciaBaja
            aggregation_method="max"
        )

        rule1 = FuzzyRule(
            id=FuzzyRuleId("68e05364d86d6edc39982900"),
            name="Rule Low",
            system_id=FuzzySystemId("68e05364d86d6edc39982860"),
            conditions=[
                {
                    "variableId": FuzzyVariableId("68e05364d86d6edc39982870"),
                    "operator": LogicalOperator.IS,
                    "value": "bajo"
                }
            ],
            connectors=[],
            consequents=[consequent1]
        )

        # Regla 2: activa potenciaAlta con firing 0.4
        consequent2 = RuleConsequent(
            variable_id=output_variable_pwm.id,
            terms=[terms_pwm[1].id],  # potenciaAlta
            aggregation_method="max"
        )

        rule2 = FuzzyRule(
            id=FuzzyRuleId("68e05364d86d6edc39982901"),
            name="Rule High",
            system_id=FuzzySystemId("68e05364d86d6edc39982860"),
            conditions=[
                {
                    "variableId": FuzzyVariableId("68e05364d86d6edc39982871"),
                    "operator": LogicalOperator.IS,
                    "value": "alto"
                }
            ],
            connectors=[],
            consequents=[consequent2]
        )

        # Resultados de evaluación
        batch_result = BatchRuleEvaluationResult()

        result1 = RuleActivationResult(
            rule_id=str(rule1.id),
            rule_name=rule1.name
        )
        result1.set_firing_strength(0.6)
        batch_result.add_rule_result(result1)

        result2 = RuleActivationResult(
            rule_id=str(rule2.id),
            rule_name=rule2.name
        )
        result2.set_firing_strength(0.4)
        batch_result.add_rule_result(result2)

        # Defuzzificar con agregación
        defuzzified_values = await fuzzy_engine.defuzzify_from_consequents(
            fuzzy_rules=[rule1, rule2],
            output_variables=[output_variable_pwm],
            rule_evaluation_result=batch_result,
            all_terms=terms_pwm
        )

        variable_id = str(output_variable_pwm.id)
        crisp_value = defuzzified_values[variable_id]

        # El centroide debe estar entre los dos picos
        # potenciaBaja clipped a 0.6 contribuye más
        assert 20.0 <= crisp_value <= 60.0  # Valor intermedio esperado
