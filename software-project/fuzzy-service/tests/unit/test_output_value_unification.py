"""Tests for output value unification in ProcessSensorReadingsHandler."""

import pytest
from datetime import datetime, timezone
from unittest.mock import MagicMock

from FuzzyService.Application.Features.SensorProcessing.Handlers.ProcessSensorReadingsHandler import ProcessSensorReadingsHandler
from FuzzyService.Domain.Entities.fuzzy_evaluation import RuleActivation, OutputValue
from FuzzyService.Domain.Entities.fuzzy_variable import FuzzyVariable
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyVariableId, FuzzyRuleId, ActuatorId


class TestOutputValueUnification:
    """Test suite for output value unification logic."""

    def test_unify_output_values_basic(self):
        """Test basic unification of Control + Duration variables."""
        # Arrange
        handler = ProcessSensorReadingsHandler()

        # Create output variables
        control_var_id = str(FuzzyVariableId.generate())
        duration_var_id = str(FuzzyVariableId.generate())
        actuator_id = str(ActuatorId.generate())
        actuator_code = "VENT_01"

        control_var = MagicMock(spec=FuzzyVariable)
        control_var.id = FuzzyVariableId(control_var_id)
        control_var.name = "Control Ventilador"
        control_var.reference_id = actuator_id
        control_var.actuator_code = actuator_code
        control_var.actuator_type = "DIGITAL"

        duration_var = MagicMock(spec=FuzzyVariable)
        duration_var.id = FuzzyVariableId(duration_var_id)
        duration_var.name = "Duración de Ventilación"
        duration_var.actuator_code = actuator_code
        duration_var.actuator_type = "DIGITAL"

        output_variables = [control_var, duration_var]

        # Create rule activations with separate output values
        control_output = OutputValue(
            actuator_id=control_var_id,
            power="ON",
            duration=0.5  # Default/placeholder duration
        )

        duration_output = OutputValue(
            actuator_id=duration_var_id,
            power="ON",
            duration=30.0  # Actual duration from fuzzy logic
        )

        rule_activation = RuleActivation(
            rule_id=FuzzyRuleId.generate(),
            firing_strength=0.75,
            output_values=[control_output, duration_output]
        )

        activated_rules = [rule_activation]

        # Create routines_payload with duration data
        routines_payload = [
            {
                "variable_id": duration_var_id,
                "variable_name": "Duración de Ventilación",
                "crisp_value": 30.0
            }
        ]

        # Act
        unified_rules = handler._unify_output_values_in_rules(
            activated_rules, output_variables, routines_payload
        )

        # Assert
        assert len(unified_rules) == 1, "Should have one unified rule"
        unified_rule = unified_rules[0]

        assert len(unified_rule.output_values) == 1, "Should have one unified output value"
        unified_output = unified_rule.output_values[0]

        # Verify unified output contains control info
        assert str(unified_output.actuator_id) == control_var_id
        assert unified_output.power == "ON"

        # Verify duration was taken from duration variable
        assert unified_output.duration == 30.0, "Duration should come from duration variable"

    def test_unify_output_values_multiple_actuators(self):
        """Test unification with multiple actuators."""
        # Arrange
        handler = ProcessSensorReadingsHandler()

        # Create output variables for two actuators
        control_vent_id = str(FuzzyVariableId.generate())
        duration_vent_id = str(FuzzyVariableId.generate())
        control_bomba_id = str(FuzzyVariableId.generate())
        duration_bomba_id = str(FuzzyVariableId.generate())

        control_vent = MagicMock(spec=FuzzyVariable)
        control_vent.id = FuzzyVariableId(control_vent_id)
        control_vent.name = "Control Ventilador"
        control_vent.actuator_code = "VENT_01"
        control_vent.actuator_type = "DIGITAL"

        duration_vent = MagicMock(spec=FuzzyVariable)
        duration_vent.id = FuzzyVariableId(duration_vent_id)
        duration_vent.name = "Duración de Ventilación"
        duration_vent.actuator_code = "VENT_01"
        duration_vent.actuator_type = "DIGITAL"

        control_bomba = MagicMock(spec=FuzzyVariable)
        control_bomba.id = FuzzyVariableId(control_bomba_id)
        control_bomba.name = "Control Bomba Riego"
        control_bomba.actuator_code = "BOMBA_01"
        control_bomba.actuator_type = "DIGITAL"

        duration_bomba = MagicMock(spec=FuzzyVariable)
        duration_bomba.id = FuzzyVariableId(duration_bomba_id)
        duration_bomba.name = "Duración de Riego"
        duration_bomba.actuator_code = "BOMBA_01"
        duration_bomba.actuator_type = "DIGITAL"

        output_variables = [control_vent, duration_vent, control_bomba, duration_bomba]

        # Create rule activation with 4 separate outputs (2 control + 2 duration)
        outputs = [
            OutputValue(actuator_id=control_vent_id, power="ON", duration=0.5),
            OutputValue(actuator_id=duration_vent_id, power="ON", duration=25.0),
            OutputValue(actuator_id=control_bomba_id, power="ON", duration=0.5),
            OutputValue(actuator_id=duration_bomba_id, power="ON", duration=60.0),
        ]

        rule_activation = RuleActivation(
            rule_id=FuzzyRuleId.generate(),
            firing_strength=0.8,
            output_values=outputs
        )

        activated_rules = [rule_activation]

        # Create routines_payload with duration data
        routines_payload = [
            {
                "variable_id": duration_vent_id,
                "variable_name": "Duración de Ventilación",
                "crisp_value": 25.0
            },
            {
                "variable_id": duration_bomba_id,
                "variable_name": "Duración de Riego",
                "crisp_value": 60.0
            }
        ]

        # Act
        unified_rules = handler._unify_output_values_in_rules(
            activated_rules, output_variables, routines_payload
        )

        # Assert
        assert len(unified_rules) == 1
        unified_rule = unified_rules[0]

        # Should have 2 unified outputs (one per actuator)
        assert len(unified_rule.output_values) == 2, "Should have 2 unified output values"

        # Verify each unified output
        output_ids = {str(ov.actuator_id) for ov in unified_rule.output_values}
        assert control_vent_id in output_ids
        assert control_bomba_id in output_ids

        # Find ventilador output and verify duration
        vent_output = next(
            ov for ov in unified_rule.output_values
            if str(ov.actuator_id) == control_vent_id
        )
        assert vent_output.duration == 25.0

        # Find bomba output and verify duration
        bomba_output = next(
            ov for ov in unified_rule.output_values
            if str(ov.actuator_id) == control_bomba_id
        )
        assert bomba_output.duration == 60.0

    def test_unify_output_values_with_dutycycle(self):
        """Test unification with PWM actuators (dutyCycle)."""
        # Arrange
        handler = ProcessSensorReadingsHandler()

        control_var_id = str(FuzzyVariableId.generate())
        duration_var_id = str(FuzzyVariableId.generate())
        actuator_code = "PWM_VENT_01"

        control_var = MagicMock(spec=FuzzyVariable)
        control_var.id = FuzzyVariableId(control_var_id)
        control_var.name = "Potencia del Ventilador"
        control_var.actuator_code = actuator_code
        control_var.actuator_type = "PWM"

        duration_var = MagicMock(spec=FuzzyVariable)
        duration_var.id = FuzzyVariableId(duration_var_id)
        duration_var.name = "Duración de Ventilación"
        duration_var.actuator_code = actuator_code
        duration_var.actuator_type = "PWM"

        output_variables = [control_var, duration_var]

        # Create outputs with dutyCycle instead of power
        control_output = OutputValue(
            actuator_id=control_var_id,
            dutyCycle=75.5,
            duration=0.5
        )

        duration_output = OutputValue(
            actuator_id=duration_var_id,
            dutyCycle=75.5,
            duration=45.0
        )

        rule_activation = RuleActivation(
            rule_id=FuzzyRuleId.generate(),
            firing_strength=0.9,
            output_values=[control_output, duration_output]
        )

        activated_rules = [rule_activation]

        # Create routines_payload with duration data
        routines_payload = [
            {
                "variable_id": duration_var_id,
                "variable_name": "Duración de Ventilación",
                "crisp_value": 45.0
            }
        ]

        # Act
        unified_rules = handler._unify_output_values_in_rules(
            activated_rules, output_variables, routines_payload
        )

        # Assert
        assert len(unified_rules) == 1
        unified_rule = unified_rules[0]

        assert len(unified_rule.output_values) == 1
        unified_output = unified_rule.output_values[0]

        # Verify PWM control is preserved
        assert unified_output.dutyCycle == 75.5
        assert unified_output.power is None
        assert unified_output.duration == 45.0

    def test_unify_output_values_fallback_duration(self):
        """Test fallback to control duration when duration variable is missing."""
        # Arrange
        handler = ProcessSensorReadingsHandler()

        control_var_id = str(FuzzyVariableId.generate())
        actuator_code = "VENT_SOLO"

        control_var = MagicMock(spec=FuzzyVariable)
        control_var.id = FuzzyVariableId(control_var_id)
        control_var.name = "Control Ventilador"
        control_var.actuator_code = actuator_code
        control_var.actuator_type = "DIGITAL"

        # Only control variable, no duration variable
        output_variables = [control_var]

        # Create output with only control
        control_output = OutputValue(
            actuator_id=control_var_id,
            power="ON",
            duration=15.0  # This should be used as fallback
        )

        rule_activation = RuleActivation(
            rule_id=FuzzyRuleId.generate(),
            firing_strength=0.6,
            output_values=[control_output]
        )

        activated_rules = [rule_activation]

        # Empty routines_payload (no duration data)
        routines_payload = []

        # Act
        unified_rules = handler._unify_output_values_in_rules(
            activated_rules, output_variables, routines_payload
        )

        # Assert
        assert len(unified_rules) == 1
        unified_rule = unified_rules[0]

        assert len(unified_rule.output_values) == 1
        unified_output = unified_rule.output_values[0]

        # Should fallback to control's duration
        assert unified_output.duration == 15.0

    def test_unify_output_values_preserves_firing_strength(self):
        """Test that unification preserves rule firing strength."""
        # Arrange
        handler = ProcessSensorReadingsHandler()

        control_var_id = str(FuzzyVariableId.generate())
        duration_var_id = str(FuzzyVariableId.generate())
        actuator_code = "VENT_STRENGTH"

        control_var = MagicMock(spec=FuzzyVariable)
        control_var.id = FuzzyVariableId(control_var_id)
        control_var.name = "Control Ventilador"
        control_var.actuator_code = actuator_code
        control_var.actuator_type = "DIGITAL"

        duration_var = MagicMock(spec=FuzzyVariable)
        duration_var.id = FuzzyVariableId(duration_var_id)
        duration_var.name = "Duración de Ventilación"
        duration_var.actuator_code = actuator_code
        duration_var.actuator_type = "DIGITAL"

        output_variables = [control_var, duration_var]

        outputs = [
            OutputValue(actuator_id=control_var_id, power="ON", duration=0.5),
            OutputValue(actuator_id=duration_var_id, power="ON", duration=20.0),
        ]

        expected_firing_strength = 0.85
        rule_activation = RuleActivation(
            rule_id=FuzzyRuleId.generate(),
            firing_strength=expected_firing_strength,
            output_values=outputs
        )

        activated_rules = [rule_activation]

        # Create routines_payload with duration data
        routines_payload = [
            {
                "variable_id": duration_var_id,
                "variable_name": "Duración de Ventilación",
                "crisp_value": 20.0
            }
        ]

        # Act
        unified_rules = handler._unify_output_values_in_rules(
            activated_rules, output_variables, routines_payload
        )

        # Assert
        unified_rule = unified_rules[0]
        assert unified_rule.firing_strength == expected_firing_strength


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
