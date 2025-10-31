import pytest
from datetime import datetime, timezone

from FuzzyService.Domain.Entities.fuzzy_evaluation import (
    FuzzyEvaluation,
    InputValue,
    OutputValue,
    RuleActivation
)
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId


class TestInputValue:
    """Tests for InputValue creation and methods."""

    def test_create_input_value_with_valid_data(self):
        # Arrange & Act
        input_val = InputValue(sensor_id="sensor_1", value=25.5)

        # Assert
        assert input_val.sensor_id == "sensor_1"
        assert input_val.value == 25.5

    def test_create_input_value_with_spaces_in_sensor_id_strips_them(self):
        # Arrange & Act
        input_val = InputValue(sensor_id="  sensor_1  ", value=30.0)

        # Assert
        assert input_val.sensor_id == "sensor_1"

    def test_create_input_value_with_empty_sensor_id_raises_error(self):
        # Arrange & Act & Assert
        with pytest.raises(ValueError, match="sensor_id debe ser un string no vacío"):
            InputValue(sensor_id="", value=25.5)

    def test_create_input_value_with_whitespace_sensor_id_raises_error(self):
        # Arrange & Act & Assert
        with pytest.raises(ValueError, match="sensor_id debe ser un string no vacío"):
            InputValue(sensor_id="   ", value=25.5)

    def test_create_input_value_with_non_numeric_value_raises_error(self):
        # Arrange & Act & Assert
        with pytest.raises(ValueError, match="value debe ser numérico"):
            InputValue(sensor_id="sensor_1", value="not_a_number")  # type: ignore

    def test_to_dict_serializes_correctly(self):
        # Arrange
        input_val = InputValue(sensor_id="sensor_1", value=25.5)

        # Act
        result = input_val.to_dict()

        # Assert
        assert result["sensor_id"] == "sensor_1"
        assert result["value"] == 25.5

    def test_from_dict_deserializes_correctly(self):
        # Arrange
        data = {"sensor_id": "sensor_2", "value": 30.0}

        # Act
        input_val = InputValue.from_dict(data)

        # Assert
        assert input_val.sensor_id == "sensor_2"
        assert input_val.value == 30.0


class TestOutputValue:
    """Tests for OutputValue creation and methods."""

    def test_create_output_value_with_power_on(self):
        # Arrange & Act
        output_val = OutputValue(
            reference_code="pump_1",
            power="ON",
            duration=30.0
        )

        # Assert
        assert output_val.reference_code == "pump_1"
        assert output_val.power == "ON"
        assert output_val.duration == 30.0

    def test_create_output_value_with_power_off(self):
        # Arrange & Act
        output_val = OutputValue(
            reference_code="fan_1",
            power="OFF",
            duration=10.0
        )

        # Assert
        assert output_val.power == "OFF"

    def test_create_output_value_with_duty_cycle(self):
        # Arrange & Act
        output_val = OutputValue(
            reference_code="led_1",
            dutyCycle=75.0,
            duration=20.0
        )

        # Assert
        assert output_val.dutyCycle == 75.0
        assert output_val.power is None

    def test_create_output_value_with_empty_reference_code_raises_error(self):
        # Arrange & Act & Assert
        with pytest.raises(ValueError, match="reference_code debe ser un string no vacío"):
            OutputValue(reference_code="", power="ON", duration=30.0)

    def test_create_output_value_with_invalid_power_raises_error(self):
        # Arrange & Act & Assert
        with pytest.raises(ValueError, match="power debe ser 'ON' o 'OFF'"):
            OutputValue(reference_code="pump_1", power="INVALID", duration=30.0)

    def test_create_output_value_with_duty_cycle_below_zero_raises_error(self):
        # Arrange & Act & Assert
        with pytest.raises(ValueError, match="dutyCycle debe estar entre 0 y 100"):
            OutputValue(reference_code="led_1", dutyCycle=-5.0, duration=20.0)

    def test_create_output_value_with_duty_cycle_above_hundred_raises_error(self):
        # Arrange & Act & Assert
        with pytest.raises(ValueError, match="dutyCycle debe estar entre 0 y 100"):
            OutputValue(reference_code="led_1", dutyCycle=150.0, duration=20.0)

    def test_create_output_value_without_power_or_duty_cycle_raises_error(self):
        # Arrange & Act & Assert
        with pytest.raises(ValueError, match="Debe definirse power o dutyCycle"):
            OutputValue(reference_code="pump_1", duration=30.0)

    def test_to_dict_with_power_serializes_correctly(self):
        # Arrange
        output_val = OutputValue(reference_code="pump_1", power="ON", duration=30.0)

        # Act
        result = output_val.to_dict()

        # Assert
        assert result["reference_code"] == "pump_1"
        assert result["power"] == "ON"
        assert result["duration"] == 30.0
        assert "dutyCycle" not in result

    def test_from_dict_with_power_deserializes_correctly(self):
        # Arrange
        data = {"reference_code": "pump_1", "power": "OFF", "duration": 15.0}

        # Act
        output_val = OutputValue.from_dict(data)

        # Assert
        assert output_val.power == "OFF"

    def test_from_dict_with_actuator_id_uses_it_as_reference_code(self):
        # Arrange
        data = {"actuator_id": "actuator_1", "power": "ON", "duration": 30.0}

        # Act
        output_val = OutputValue.from_dict(data)

        # Assert
        assert output_val.reference_code == "actuator_1"


class TestRuleActivation:
    """Tests for RuleActivation creation and methods."""

    def test_create_rule_activation_with_string_rule_id(self):
        # Arrange & Act
        activation = RuleActivation(
            rule_id="rule_1",  # type: ignore
            firing_strength=0.8
        )

        # Assert
        assert activation.rule_id == "rule_1"
        assert activation.firing_strength == 0.8

    def test_create_rule_activation_with_dict_rule_id_extracts_oid(self):
        # Arrange & Act
        activation = RuleActivation(
            rule_id={"$oid": "rule_oid_123"},  # type: ignore
            firing_strength=0.6
        )

        # Assert
        assert activation.rule_id == "rule_oid_123"

    def test_create_rule_activation_with_output_values(self):
        # Arrange
        output = OutputValue(reference_code="pump_1", power="ON", duration=30.0)

        # Act
        activation = RuleActivation(
            rule_id="rule_1",  # type: ignore
            firing_strength=0.9,
            output_values=[output]
        )

        # Assert
        assert len(activation.output_values) == 1

    def test_create_rule_activation_with_firing_strength_zero(self):
        # Arrange & Act
        activation = RuleActivation(
            rule_id="rule_1",  # type: ignore
            firing_strength=0.0
        )

        # Assert
        assert activation.firing_strength == 0.0

    def test_create_rule_activation_with_firing_strength_one(self):
        # Arrange & Act
        activation = RuleActivation(
            rule_id="rule_1",  # type: ignore
            firing_strength=1.0
        )

        # Assert
        assert activation.firing_strength == 1.0

    def test_to_dict_serializes_correctly(self):
        # Arrange
        output = OutputValue(reference_code="pump_1", power="ON", duration=30.0)
        activation = RuleActivation(
            rule_id="rule_1",  # type: ignore
            firing_strength=0.85,
            output_values=[output]
        )

        # Act
        result = activation.to_dict()

        # Assert
        assert result["ruleId"] == "rule_1"
        assert result["firingStrength"] == 0.85
        assert len(result["output_values"]) == 1

    def test_from_dict_deserializes_correctly(self):
        # Arrange
        data = {
            "ruleId": "rule_2",
            "firingStrength": 0.7,
            "output_values": []
        }

        # Act
        activation = RuleActivation.from_dict(data)

        # Assert
        assert activation.rule_id == "rule_2"  # type: ignore
        assert activation.firing_strength == 0.7


class TestFuzzyEvaluation:
    """Tests for FuzzyEvaluation creation and methods."""

    def test_create_fuzzy_evaluation_with_minimal_data(self):
        # Arrange & Act
        evaluation = FuzzyEvaluation(system_id=FuzzySystemId())

        # Assert
        assert evaluation.system_id is not None
        assert evaluation.timestamp is not None
        assert len(evaluation.inputs) == 0
        assert len(evaluation.activated_rules) == 0

    def test_create_fuzzy_evaluation_generates_timestamp_if_not_provided(self):
        # Arrange & Act
        before = datetime.now(timezone.utc)
        evaluation = FuzzyEvaluation(system_id=FuzzySystemId())
        after = datetime.now(timezone.utc)

        # Assert
        assert before <= evaluation.timestamp <= after

    def test_add_input_adds_input_value(self):
        # Arrange
        evaluation = FuzzyEvaluation(system_id=FuzzySystemId())

        # Act
        evaluation.add_input("sensor_1", 25.5)

        # Assert
        assert len(evaluation.inputs) == 1
        assert evaluation.inputs[0].sensor_id == "sensor_1"
        assert evaluation.inputs[0].value == 25.5

    def test_add_input_multiple_times_adds_all(self):
        # Arrange
        evaluation = FuzzyEvaluation(system_id=FuzzySystemId())

        # Act
        evaluation.add_input("sensor_1", 25.5)
        evaluation.add_input("sensor_2", 60.0)
        evaluation.add_input("sensor_3", 7.2)

        # Assert
        assert len(evaluation.inputs) == 3

    def test_add_rule_activation_adds_activation(self):
        # Arrange
        evaluation = FuzzyEvaluation(system_id=FuzzySystemId())
        activation = RuleActivation(rule_id="rule_1", firing_strength=0.8)  # type: ignore

        # Act
        evaluation.add_rule_activation(activation)

        # Assert
        assert len(evaluation.activated_rules) == 1

    def test_add_rule_activation_multiple_times_adds_all(self):
        # Arrange
        evaluation = FuzzyEvaluation(system_id=FuzzySystemId())
        activation1 = RuleActivation(rule_id="rule_1", firing_strength=0.8)  # type: ignore
        activation2 = RuleActivation(rule_id="rule_2", firing_strength=0.6)  # type: ignore

        # Act
        evaluation.add_rule_activation(activation1)
        evaluation.add_rule_activation(activation2)

        # Assert
        assert len(evaluation.activated_rules) == 2

    def test_to_dict_with_none_id_serializes_none(self):
        # Arrange
        evaluation = FuzzyEvaluation(system_id=FuzzySystemId())

        # Act
        result = evaluation.to_dict()

        # Assert
        assert result["evalId"] is None

