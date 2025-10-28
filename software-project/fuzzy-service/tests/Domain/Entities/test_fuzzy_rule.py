import pytest
from datetime import datetime, timezone
from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule
from FuzzyService.Domain.Entities.rule_consequent import RuleConsequent
from FuzzyService.Domain.ValueObjects import FuzzyRuleId, FuzzySystemId, FuzzyVariableId, FuzzyTermId
from FuzzyService.Domain.Enums import RuleConnector, LogicalOperator


class TestFuzzyRuleCreation:
    """Tests for FuzzyRule creation and validation"""

    def test_create_fuzzy_rule_with_minimal_valid_data_should_succeed(self):
        # Arrange & Act
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )

        rule = FuzzyRule(
            name="Test Rule",
            conditions=[{
                "variableId": FuzzyVariableId.generate(),
                "operator": LogicalOperator.IS,
                "value": "high"
            }],
            connectors=[],
            consequents=[consequent]
        )

        # Assert
        assert rule.name == "Test Rule"
        assert len(rule.conditions) == 1
        assert len(rule.connectors) == 0
        assert len(rule.consequents) == 1
        assert rule.created_at is not None

    def test_create_fuzzy_rule_with_empty_name_should_fail(self):
        # Arrange & Act & Assert
        with pytest.raises(ValueError, match="nombre de la regla no puede estar vacío"):
            consequent = RuleConsequent(
                variable_id=FuzzyVariableId.generate(),
                terms=[FuzzyTermId.generate()]
            )

            FuzzyRule(
                name="",
                conditions=[{
                    "variableId": FuzzyVariableId.generate(),
                    "operator": LogicalOperator.IS,
                    "value": "high"
                }],
                consequents=[consequent]
            )

    def test_create_fuzzy_rule_with_whitespace_name_should_fail(self):
        # Arrange & Act & Assert
        with pytest.raises(ValueError, match="nombre de la regla no puede estar vacío"):
            consequent = RuleConsequent(
                variable_id=FuzzyVariableId.generate(),
                terms=[FuzzyTermId.generate()]
            )

            FuzzyRule(
                name="   ",
                conditions=[{
                    "variableId": FuzzyVariableId.generate(),
                    "operator": LogicalOperator.IS,
                    "value": "high"
                }],
                consequents=[consequent]
            )

    def test_create_fuzzy_rule_with_name_too_long_should_fail(self):
        # Arrange & Act & Assert
        with pytest.raises(ValueError, match="no puede exceder 100 caracteres"):
            consequent = RuleConsequent(
                variable_id=FuzzyVariableId.generate(),
                terms=[FuzzyTermId.generate()]
            )

            FuzzyRule(
                name="x" * 101,
                conditions=[{
                    "variableId": FuzzyVariableId.generate(),
                    "operator": LogicalOperator.IS,
                    "value": "high"
                }],
                consequents=[consequent]
            )

    def test_create_fuzzy_rule_without_consequents_should_fail(self):
        # Arrange & Act & Assert
        with pytest.raises(ValueError, match="debe tener al menos un consecuente"):
            FuzzyRule(
                name="Test Rule",
                conditions=[{
                    "variableId": FuzzyVariableId.generate(),
                    "operator": LogicalOperator.IS,
                    "value": "high"
                }],
                consequents=[]
            )

    def test_create_fuzzy_rule_with_two_conditions_should_require_one_connector(self):
        # Arrange
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )

        # Act
        rule = FuzzyRule(
            name="Test Rule",
            conditions=[
                {
                    "variableId": FuzzyVariableId.generate(),
                    "operator": LogicalOperator.IS,
                    "value": "high"
                },
                {
                    "variableId": FuzzyVariableId.generate(),
                    "operator": LogicalOperator.IS,
                    "value": "low"
                }
            ],
            connectors=[RuleConnector.AND],
            consequents=[consequent]
        )

        # Assert
        assert len(rule.conditions) == 2
        assert len(rule.connectors) == 1

    def test_create_fuzzy_rule_with_mismatched_connectors_should_fail(self):
        # Arrange & Act & Assert
        with pytest.raises(ValueError, match="Número de conectores inválido"):
            consequent = RuleConsequent(
                variable_id=FuzzyVariableId.generate(),
                terms=[FuzzyTermId.generate()]
            )

            FuzzyRule(
                name="Test Rule",
                conditions=[
                    {
                        "variableId": FuzzyVariableId.generate(),
                        "operator": LogicalOperator.IS,
                        "value": "high"
                    },
                    {
                        "variableId": FuzzyVariableId.generate(),
                        "operator": LogicalOperator.IS,
                        "value": "low"
                    }
                ],
                connectors=[],  # Should have 1 connector
                consequents=[consequent]
            )

    def test_create_fuzzy_rule_with_duplicate_variable_ids_should_fail(self):
        # Arrange
        var_id = FuzzyVariableId.generate()
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )

        # Act & Assert
        with pytest.raises(ValueError, match="variableId duplicado"):
            FuzzyRule(
                name="Test Rule",
                conditions=[
                    {
                        "variableId": var_id,
                        "operator": LogicalOperator.IS,
                        "value": "high"
                    },
                    {
                        "variableId": var_id,
                        "operator": LogicalOperator.IS,
                        "value": "low"
                    }
                ],
                connectors=[RuleConnector.AND],
                consequents=[consequent]
            )


class TestFuzzyRuleConditions:
    """Tests for FuzzyRule condition management"""

    def test_add_condition_to_rule_without_conditions_should_succeed(self):
        # Arrange
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )

        rule = FuzzyRule(
            name="Test Rule",
            conditions=[{
                "variableId": FuzzyVariableId.generate(),
                "operator": LogicalOperator.IS,
                "value": "initial"
            }],
            consequents=[consequent]
        )
        initial_count = len(rule.conditions)

        # Act
        rule.add_condition(
            FuzzyVariableId.generate(),
            LogicalOperator.IS,
            "new_value",
            RuleConnector.AND
        )

        # Assert
        assert len(rule.conditions) == initial_count + 1
        assert len(rule.connectors) == 1

    def test_add_condition_without_connector_when_needed_should_fail(self):
        # Arrange
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )

        rule = FuzzyRule(
            name="Test Rule",
            conditions=[{
                "variableId": FuzzyVariableId.generate(),
                "operator": LogicalOperator.IS,
                "value": "initial"
            }],
            consequents=[consequent]
        )

        # Act & Assert
        with pytest.raises(ValueError, match="Debe especificarse un conector"):
            rule.add_condition(
                FuzzyVariableId.generate(),
                LogicalOperator.IS,
                "new_value",
                None
            )

    def test_add_duplicate_condition_variable_should_fail(self):
        # Arrange
        var_id = FuzzyVariableId.generate()
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )

        rule = FuzzyRule(
            name="Test Rule",
            conditions=[{
                "variableId": var_id,
                "operator": LogicalOperator.IS,
                "value": "initial"
            }],
            consequents=[consequent]
        )

        # Act & Assert
        with pytest.raises(ValueError, match="Ya existe una condición para la variable"):
            rule.add_condition(var_id, LogicalOperator.IS, "new_value", RuleConnector.AND)

    def test_remove_condition_should_adjust_connectors(self):
        # Arrange
        var_id1 = FuzzyVariableId.generate()
        var_id2 = FuzzyVariableId.generate()
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )

        rule = FuzzyRule(
            name="Test Rule",
            conditions=[
                {"variableId": var_id1, "operator": LogicalOperator.IS, "value": "high"},
                {"variableId": var_id2, "operator": LogicalOperator.IS, "value": "low"}
            ],
            connectors=[RuleConnector.AND],
            consequents=[consequent]
        )

        # Act
        rule.remove_condition(var_id1)

        # Assert
        assert len(rule.conditions) == 1
        assert len(rule.connectors) == 0

    def test_remove_last_condition_should_fail(self):
        # Arrange
        var_id = FuzzyVariableId.generate()
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )

        rule = FuzzyRule(
            name="Test Rule",
            conditions=[{
                "variableId": var_id,
                "operator": LogicalOperator.IS,
                "value": "high"
            }],
            consequents=[consequent]
        )

        # Act & Assert
        with pytest.raises(ValueError, match="debe tener al menos una condición"):
            rule.remove_condition(var_id)

    def test_update_condition_should_change_operator_and_value(self):
        # Arrange
        var_id = FuzzyVariableId.generate()
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )

        rule = FuzzyRule(
            name="Test Rule",
            conditions=[{
                "variableId": var_id,
                "operator": LogicalOperator.IS,
                "value": "high"
            }],
            consequents=[consequent]
        )

        # Act
        rule.update_condition(var_id, LogicalOperator.IS_NOT, "low")

        # Assert
        condition = rule.get_condition_by_variable(var_id)
        assert condition["operator"] == LogicalOperator.IS_NOT
        assert condition["value"] == "low"


class TestFuzzyRuleConsequents:
    """Tests for FuzzyRule consequent management"""

    def test_add_consequent_should_succeed(self):
        # Arrange
        var_id1 = FuzzyVariableId.generate()
        var_id2 = FuzzyVariableId.generate()
        consequent1 = RuleConsequent(
            variable_id=var_id1,
            terms=[FuzzyTermId.generate()]
        )

        rule = FuzzyRule(
            name="Test Rule",
            conditions=[{
                "variableId": FuzzyVariableId.generate(),
                "operator": LogicalOperator.IS,
                "value": "high"
            }],
            consequents=[consequent1]
        )

        consequent2 = RuleConsequent(
            variable_id=var_id2,
            terms=[FuzzyTermId.generate()]
        )

        # Act
        rule.add_consequent(consequent2)

        # Assert
        assert rule.get_consequent_count() == 2

    def test_add_duplicate_consequent_variable_should_fail(self):
        # Arrange
        var_id = FuzzyVariableId.generate()
        consequent1 = RuleConsequent(
            variable_id=var_id,
            terms=[FuzzyTermId.generate()]
        )

        rule = FuzzyRule(
            name="Test Rule",
            conditions=[{
                "variableId": FuzzyVariableId.generate(),
                "operator": LogicalOperator.IS,
                "value": "high"
            }],
            consequents=[consequent1]
        )

        consequent2 = RuleConsequent(
            variable_id=var_id,
            terms=[FuzzyTermId.generate()]
        )

        # Act & Assert
        with pytest.raises(ValueError, match="Ya existe un consecuente"):
            rule.add_consequent(consequent2)

    def test_remove_consequent_should_succeed(self):
        # Arrange
        var_id1 = FuzzyVariableId.generate()
        var_id2 = FuzzyVariableId.generate()

        rule = FuzzyRule(
            name="Test Rule",
            conditions=[{
                "variableId": FuzzyVariableId.generate(),
                "operator": LogicalOperator.IS,
                "value": "high"
            }],
            consequents=[
                RuleConsequent(variable_id=var_id1, terms=[FuzzyTermId.generate()]),
                RuleConsequent(variable_id=var_id2, terms=[FuzzyTermId.generate()])
            ]
        )

        # Act
        rule.remove_consequent(var_id1)

        # Assert
        assert rule.get_consequent_count() == 1
        assert rule.get_consequent_for_variable(var_id1) is None

    def test_get_consequent_for_variable_should_return_correct_consequent(self):
        # Arrange
        var_id = FuzzyVariableId.generate()
        consequent = RuleConsequent(
            variable_id=var_id,
            terms=[FuzzyTermId.generate()]
        )

        rule = FuzzyRule(
            name="Test Rule",
            conditions=[{
                "variableId": FuzzyVariableId.generate(),
                "operator": LogicalOperator.IS,
                "value": "high"
            }],
            consequents=[consequent]
        )

        # Act
        result = rule.get_consequent_for_variable(var_id)

        # Assert
        assert result is not None
        assert result.variable_id == var_id


class TestFuzzyRuleConnectors:
    """Tests for FuzzyRule connector management"""

    def test_set_connector_at_valid_index_should_succeed(self):
        # Arrange
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )

        rule = FuzzyRule(
            name="Test Rule",
            conditions=[
                {"variableId": FuzzyVariableId.generate(), "operator": LogicalOperator.IS, "value": "high"},
                {"variableId": FuzzyVariableId.generate(), "operator": LogicalOperator.IS, "value": "low"}
            ],
            connectors=[RuleConnector.AND],
            consequents=[consequent]
        )

        # Act
        rule.set_connector_at(0, RuleConnector.OR)

        # Assert
        assert rule.connectors[0] == RuleConnector.OR

    def test_set_connector_at_invalid_index_should_fail(self):
        # Arrange
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )

        rule = FuzzyRule(
            name="Test Rule",
            conditions=[
                {"variableId": FuzzyVariableId.generate(), "operator": LogicalOperator.IS, "value": "high"},
                {"variableId": FuzzyVariableId.generate(), "operator": LogicalOperator.IS, "value": "low"}
            ],
            connectors=[RuleConnector.AND],
            consequents=[consequent]
        )

        # Act & Assert
        with pytest.raises(IndexError, match="fuera de rango"):
            rule.set_connector_at(10, RuleConnector.OR)

    def test_set_connectors_with_correct_count_should_succeed(self):
        # Arrange
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )

        rule = FuzzyRule(
            name="Test Rule",
            conditions=[
                {"variableId": FuzzyVariableId.generate(), "operator": LogicalOperator.IS, "value": "high"},
                {"variableId": FuzzyVariableId.generate(), "operator": LogicalOperator.IS, "value": "low"},
                {"variableId": FuzzyVariableId.generate(), "operator": LogicalOperator.IS, "value": "medium"}
            ],
            connectors=[RuleConnector.AND, RuleConnector.OR],
            consequents=[consequent]
        )

        # Act
        rule.set_connectors([RuleConnector.OR, RuleConnector.AND])

        # Assert
        assert len(rule.connectors) == 2
        # Los conectores se convierten a enum durante la coerción
        assert str(rule.connectors[0]) == "OR" or rule.connectors[0] == RuleConnector.OR
        assert str(rule.connectors[1]) == "AND" or rule.connectors[1] == RuleConnector.AND

    def test_set_connectors_with_incorrect_count_should_fail(self):
        # Arrange
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )

        rule = FuzzyRule(
            name="Test Rule",
            conditions=[
                {"variableId": FuzzyVariableId.generate(), "operator": LogicalOperator.IS, "value": "high"},
                {"variableId": FuzzyVariableId.generate(), "operator": LogicalOperator.IS, "value": "low"}
            ],
            connectors=[RuleConnector.AND],
            consequents=[consequent]
        )

        # Act & Assert
        with pytest.raises(ValueError, match="Número de conectores inválido"):
            rule.set_connectors([RuleConnector.OR, RuleConnector.AND])


class TestFuzzyRuleUtilities:
    """Tests for FuzzyRule utility methods"""

    def test_get_rule_text_should_format_conditions_correctly(self):
        # Arrange
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )

        rule = FuzzyRule(
            name="Test Rule",
            conditions=[
                {"variableId": FuzzyVariableId.generate(), "operator": LogicalOperator.IS, "value": "high"},
                {"variableId": FuzzyVariableId.generate(), "operator": LogicalOperator.IS, "value": "low"}
            ],
            connectors=[RuleConnector.AND],
            consequents=[consequent]
        )

        # Act
        text = rule.get_rule_text()

        # Assert
        assert "high" in text
        assert "low" in text
        assert "AND" in text

    def test_to_dict_should_serialize_correctly(self):
        # Arrange
        var_id = FuzzyVariableId.generate()
        system_id = FuzzySystemId.generate()
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )

        rule = FuzzyRule(
            id=FuzzyRuleId.generate(),
            name="Test Rule",
            system_id=system_id,
            description="Test description",
            conditions=[{
                "variableId": var_id,
                "operator": LogicalOperator.IS,
                "value": "high"
            }],
            consequents=[consequent]
        )

        # Act
        result = rule.to_dict()

        # Assert
        assert result["name"] == "Test Rule"
        assert result["description"] == "Test description"
        assert "id" in result
        assert "systemId" in result
        assert "conditions" in result
        assert "consequents" in result

    def test_has_condition_for_variable_should_return_true_when_exists(self):
        # Arrange
        var_id = FuzzyVariableId.generate()
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )

        rule = FuzzyRule(
            name="Test Rule",
            conditions=[{
                "variableId": var_id,
                "operator": LogicalOperator.IS,
                "value": "high"
            }],
            consequents=[consequent]
        )

        # Act & Assert
        assert rule.has_condition_for_variable(var_id) is True

    def test_has_condition_for_variable_should_return_false_when_not_exists(self):
        # Arrange
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )

        rule = FuzzyRule(
            name="Test Rule",
            conditions=[{
                "variableId": FuzzyVariableId.generate(),
                "operator": LogicalOperator.IS,
                "value": "high"
            }],
            consequents=[consequent]
        )

        # Act & Assert
        assert rule.has_condition_for_variable(FuzzyVariableId.generate()) is False

    def test_get_variables_used_should_return_all_variable_ids(self):
        # Arrange
        var_id1 = FuzzyVariableId.generate()
        var_id2 = FuzzyVariableId.generate()
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )

        rule = FuzzyRule(
            name="Test Rule",
            conditions=[
                {"variableId": var_id1, "operator": LogicalOperator.IS, "value": "high"},
                {"variableId": var_id2, "operator": LogicalOperator.IS, "value": "low"}
            ],
            connectors=[RuleConnector.AND],
            consequents=[consequent]
        )

        # Act
        variables = rule.get_variables_used()

        # Assert
        assert len(variables) == 2
        assert var_id1 in variables
        assert var_id2 in variables

    def test_update_description_should_change_description(self):
        # Arrange
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )

        rule = FuzzyRule(
            name="Test Rule",
            conditions=[{
                "variableId": FuzzyVariableId.generate(),
                "operator": LogicalOperator.IS,
                "value": "high"
            }],
            consequents=[consequent]
        )

        # Act
        rule.update_description("New description")

        # Assert
        assert rule.description == "New description"
