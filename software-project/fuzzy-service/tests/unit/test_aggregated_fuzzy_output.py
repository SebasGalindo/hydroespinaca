"""Tests for aggregated fuzzy output generation (Mamdani aggregation)."""

import pytest
from unittest.mock import MagicMock
from datetime import datetime, timezone

from FuzzyService.Application.Features.SensorProcessing.Handlers.ProcessSensorReadingsHandler import ProcessSensorReadingsHandler
from FuzzyService.Domain.Entities.fuzzy_variable import FuzzyVariable
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyVariableId, FuzzySystemId


class TestAggregatedFuzzyOutput:
    """Test suite for aggregated fuzzy output with conflicting rules."""

    def test_conflicting_rules_produce_single_aggregated_output(self):
        """Test that two conflicting rules produce ONE aggregated output, not two contradictory ones.

        Scenario: Two rules fire simultaneously:
        - Rule 1 ("Temperatura agua fría"): firing_strength=0.6 → Calefactor ON
        - Rule 2 ("Temperatura agua normal"): firing_strength=0.4 → Calefactor OFF

        Expected: ONE output with aggregated value (not two contradictory outputs)
        """
        # Arrange
        handler = ProcessSensorReadingsHandler()

        # Create output variables
        calefactor_control_id = str(FuzzyVariableId.generate())
        calefactor_duration_id = str(FuzzyVariableId.generate())
        actuator_code = "CALEFACTOR_AGUA"

        calefactor_control = MagicMock(spec=FuzzyVariable)
        calefactor_control.id = FuzzyVariableId(calefactor_control_id)
        calefactor_control.name = "Control Calefactor Agua"
        calefactor_control.reference_code = "CALEFACTOR_AGUA_OUTPUT"
        calefactor_control.actuator_code = actuator_code
        calefactor_control.actuator_type = "DIGITAL"

        calefactor_duration = MagicMock(spec=FuzzyVariable)
        calefactor_duration.id = FuzzyVariableId(calefactor_duration_id)
        calefactor_duration.name = "Duración de Calefacción de Agua"
        calefactor_duration.actuator_code = actuator_code
        calefactor_duration.actuator_type = "DIGITAL"

        output_variables = [calefactor_control, calefactor_duration]

        # Simulate activated_rules_data (2 conflicting rules)
        activated_rules_data = [
            {
                "rule_id": "68eb54b4b3a28606a87fc55e",
                "rule_name": "Temperatura agua fría",
                "firing_strength": 0.6,  # Stronger activation
                "output_values": []  # No usado en nuevo flujo
            },
            {
                "rule_id": "68eb54b5b3a28606a87fc55f",
                "rule_name": "Temperatura agua normal o más alta",
                "firing_strength": 0.4,  # Weaker activation
                "output_values": []  # No usado en nuevo flujo
            }
        ]

        # Simulate routines_payload (result of Mamdani aggregation)
        # The fuzzy engine already aggregated both rules and produced ONE value per variable
        routines_payload = [
            {
                "variable_id": calefactor_control_id,
                "variable_name": "Control Calefactor Agua",
                "crisp_value": 65.0,  # Aggregated value (between ON threshold)
                "command": {"power": "ON"}  # Result of defuzzification + threshold
            },
            {
                "variable_id": calefactor_duration_id,
                "variable_name": "Duración de Calefacción de Agua",
                "crisp_value": 45.0,  # Aggregated duration
                "command": {}
            }
        ]

        # Act
        consolidated_rules = handler._create_aggregated_rule_activation(
            activated_rules_data, output_variables, routines_payload
        )

        # Assert
        assert len(consolidated_rules) == 1, "Should create ONLY ONE consolidated RuleActivation"

        consolidated_rule = consolidated_rules[0]
        assert len(consolidated_rule.output_values) == 1, "Should have ONE output (not two contradictory ones)"

        output_val = consolidated_rule.output_values[0]
        assert str(output_val.actuator_id) == calefactor_control_id
        assert output_val.power == "ON", "Aggregated result should be ON (firing_strength 0.6 > 0.4)"
        assert output_val.duration == 45.0, "Duration should be the aggregated value"

    def test_multiple_actuators_with_aggregation(self):
        """Test aggregation with multiple independent actuators."""
        # Arrange
        handler = ProcessSensorReadingsHandler()

        # Create variables for two actuators
        calefactor_control_id = str(FuzzyVariableId.generate())
        calefactor_duration_id = str(FuzzyVariableId.generate())
        ventilador_control_id = str(FuzzyVariableId.generate())
        ventilador_duration_id = str(FuzzyVariableId.generate())

        calefactor_control = MagicMock(spec=FuzzyVariable)
        calefactor_control.id = FuzzyVariableId(calefactor_control_id)
        calefactor_control.name = "Control Calefactor Aire"
        calefactor_control.reference_code = "CALEFACTOR_AIRE_OUTPUT"
        calefactor_control.actuator_code = "CALEFACTOR_AIRE"
        calefactor_control.actuator_type = "DIGITAL"

        calefactor_duration = MagicMock(spec=FuzzyVariable)
        calefactor_duration.id = FuzzyVariableId(calefactor_duration_id)
        calefactor_duration.name = "Duración de Calefacción de Aire"
        calefactor_duration.actuator_code = "CALEFACTOR_AIRE"
        calefactor_duration.actuator_type = "DIGITAL"

        ventilador_control = MagicMock(spec=FuzzyVariable)
        ventilador_control.id = FuzzyVariableId(ventilador_control_id)
        ventilador_control.name = "Potencia del Ventilador"
        ventilador_control.reference_code = "VENTILADOR_OUTPUT"
        ventilador_control.actuator_code = "VENTILADOR"
        ventilador_control.actuator_type = "PWM"

        ventilador_duration = MagicMock(spec=FuzzyVariable)
        ventilador_duration.id = FuzzyVariableId(ventilador_duration_id)
        ventilador_duration.name = "Duración de Ventilación"
        ventilador_duration.actuator_code = "VENTILADOR"
        ventilador_duration.actuator_type = "PWM"

        output_variables = [
            calefactor_control, calefactor_duration,
            ventilador_control, ventilador_duration
        ]

        # Multiple rules contributing to different actuators
        activated_rules_data = [
            {"rule_id": "rule1", "rule_name": "Rule 1", "firing_strength": 0.7},
            {"rule_id": "rule2", "rule_name": "Rule 2", "firing_strength": 0.5},
            {"rule_id": "rule3", "rule_name": "Rule 3", "firing_strength": 0.3}
        ]

        # Aggregated outputs for two actuators
        routines_payload = [
            {
                "variable_id": calefactor_control_id,
                "variable_name": "Control Calefactor Aire",
                "crisp_value": 80.0,
                "command": {"power": "ON"}
            },
            {
                "variable_id": calefactor_duration_id,
                "variable_name": "Duración de Calefacción de Aire",
                "crisp_value": 60.0,
                "command": {}
            },
            {
                "variable_id": ventilador_control_id,
                "variable_name": "Potencia del Ventilador",
                "crisp_value": 75.5,
                "command": {"dutyCycle": 75.5}
            },
            {
                "variable_id": ventilador_duration_id,
                "variable_name": "Duración de Ventilación",
                "crisp_value": 120.0,
                "command": {}
            }
        ]

        # Act
        consolidated_rules = handler._create_aggregated_rule_activation(
            activated_rules_data, output_variables, routines_payload
        )

        # Assert
        assert len(consolidated_rules) == 1, "Should create ONE consolidated RuleActivation"

        consolidated_rule = consolidated_rules[0]
        assert len(consolidated_rule.output_values) == 2, "Should have TWO outputs (one per actuator)"

        # Check calefactor output
        calefactor_output = next(
            (ov for ov in consolidated_rule.output_values
             if str(ov.actuator_id) == calefactor_control_id),
            None
        )
        assert calefactor_output is not None
        assert calefactor_output.power == "ON"
        assert calefactor_output.duration == 60.0

        # Check ventilador output
        ventilador_output = next(
            (ov for ov in consolidated_rule.output_values
             if str(ov.actuator_id) == ventilador_control_id),
            None
        )
        assert ventilador_output is not None
        assert ventilador_output.dutyCycle == 75.5
        assert ventilador_output.duration == 120.0

    def test_no_duration_variable_uses_default(self):
        """Test that missing duration variable defaults to 0.5 seconds."""
        # Arrange
        handler = ProcessSensorReadingsHandler()

        control_id = str(FuzzyVariableId.generate())

        control_var = MagicMock(spec=FuzzyVariable)
        control_var.id = FuzzyVariableId(control_id)
        control_var.name = "Control Solo"
        control_var.reference_code = "CONTROL_SOLO_OUTPUT"
        control_var.actuator_code = "CONTROL_SOLO"
        control_var.actuator_type = "DIGITAL"

        output_variables = [control_var]

        activated_rules_data = [
            {"rule_id": "rule1", "rule_name": "Rule 1", "firing_strength": 0.8}
        ]

        routines_payload = [
            {
                "variable_id": control_id,
                "variable_name": "Control Solo",
                "crisp_value": 90.0,
                "command": {"power": "ON"}
            }
            # No duration variable in payload
        ]

        # Act
        consolidated_rules = handler._create_aggregated_rule_activation(
            activated_rules_data, output_variables, routines_payload
        )

        # Assert
        assert len(consolidated_rules) == 1
        consolidated_rule = consolidated_rules[0]
        assert len(consolidated_rule.output_values) == 1

        output_val = consolidated_rule.output_values[0]
        assert output_val.duration == 0.5, "Should use default duration when no duration variable"


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
