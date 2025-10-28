import pytest
from FuzzyService.Domain.ValueObjects.RuleCondition import RuleCondition


class TestRuleConditionBasic:
    """Basic tests for RuleCondition"""

    def test_create_rule_condition_with_valid_data_should_succeed(self):
        # Arrange & Act
        condition = RuleCondition(
            condition_id="cond1",
            sensor_name="temperature",
            operator="IS",
            target_value="high"
        )

        # Assert
        assert condition.condition_id == "cond1"
        assert condition.sensor_name == "temperature"
        assert condition.operator == "IS"
        assert condition.target_value == "high"

    def test_is_linguistic_condition_should_return_true(self):
        # Arrange
        condition = RuleCondition(
            condition_id="cond1",
            sensor_name="temperature",
            operator="IS",
            target_value="high"
        )

        # Act & Assert
        assert condition.is_linguistic_condition() is True
