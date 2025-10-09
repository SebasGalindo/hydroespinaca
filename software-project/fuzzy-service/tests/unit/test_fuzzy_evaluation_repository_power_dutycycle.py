"""
Test unitario para verificar que FuzzyEvaluationRepository maneja correctamente
los campos power (string) y dutyCycle (float) en output_values.
"""

import pytest
from datetime import datetime, timezone
from FuzzyService.Domain.Entities.fuzzy_evaluation import (
    FuzzyEvaluation,
    InputValue,
    RuleActivation,
    OutputValue,
)
from FuzzyService.Domain.ValueObjects import FuzzyEvaluationId, FuzzySystemId, FuzzyRuleId
from FuzzyService.Infrastructure.Persistence.Repositories.FuzzyEvaluationRepository import (
    FuzzyEvaluationRepository,
)


class TestFuzzyEvaluationRepositoryPowerDutyCycle:
    """Tests para verificar serialización de power/dutyCycle."""

    def test_entity_to_doc_with_digital_actuators(self):
        """Verifica que _entity_to_doc convierte correctamente power="ON"/"OFF"."""
        # Crear evaluación con actuadores digitales
        evaluation = FuzzyEvaluation(
            id=FuzzyEvaluationId("507f1f77bcf86cd799439011"),
            system_id=FuzzySystemId("507f1f77bcf86cd799439012"),
            timestamp=datetime(2025, 1, 1, 12, 0, 0, tzinfo=timezone.utc),
        )
        evaluation.add_input("sensor_temp", 25.5)

        # Regla con output_values que tienen power="ON"
        rule_activation = RuleActivation(
            rule_id=FuzzyRuleId("507f1f77bcf86cd799439013"),
            firing_strength=0.85,
            output_values=[
                OutputValue(
                    actuator_id="actuator_calefactor",
                    power="ON",
                    duration=180,
                ),
                OutputValue(
                    actuator_id="actuator_humidificador",
                    power="OFF",
                    duration=1,
                ),
            ],
        )
        evaluation.add_rule_activation(rule_activation)

        # Convertir a documento
        doc = FuzzyEvaluationRepository._entity_to_doc(evaluation)

        # Verificar estructura del documento
        assert "activated_rules" in doc
        assert len(doc["activated_rules"]) == 1

        activation = doc["activated_rules"][0]
        assert len(activation["output_values"]) == 2

        # Verificar primer output (ON)
        output1 = activation["output_values"][0]
        assert output1["actuator_id"] == "actuator_calefactor"
        assert output1["power"] == "ON"  # String, no número
        assert "dutyCycle" not in output1  # NO debe tener dutyCycle
        assert output1["duration"] == 180

        # Verificar segundo output (OFF)
        output2 = activation["output_values"][1]
        assert output2["actuator_id"] == "actuator_humidificador"
        assert output2["power"] == "OFF"  # String, no número
        assert "dutyCycle" not in output2  # NO debe tener dutyCycle
        assert output2["duration"] == 1

    def test_entity_to_doc_with_pwm_actuators(self):
        """Verifica que _entity_to_doc convierte correctamente dutyCycle numérico."""
        # Crear evaluación con actuadores PWM
        evaluation = FuzzyEvaluation(
            id=FuzzyEvaluationId("507f1f77bcf86cd799439011"),
            system_id=FuzzySystemId("507f1f77bcf86cd799439012"),
            timestamp=datetime(2025, 1, 1, 12, 0, 0, tzinfo=timezone.utc),
        )
        evaluation.add_input("sensor_temp", 30.5)

        # Regla con output_values que tienen dutyCycle
        rule_activation = RuleActivation(
            rule_id=FuzzyRuleId("507f1f77bcf86cd799439013"),
            firing_strength=0.72,
            output_values=[
                OutputValue(
                    actuator_id="actuator_ventilador",
                    dutyCycle=75.5,
                    duration=120,
                ),
                OutputValue(
                    actuator_id="actuator_bomba",
                    dutyCycle=50.0,
                    duration=300,
                ),
            ],
        )
        evaluation.add_rule_activation(rule_activation)

        # Convertir a documento
        doc = FuzzyEvaluationRepository._entity_to_doc(evaluation)

        # Verificar estructura del documento
        assert "activated_rules" in doc
        assert len(doc["activated_rules"]) == 1

        activation = doc["activated_rules"][0]
        assert len(activation["output_values"]) == 2

        # Verificar primer output (dutyCycle)
        output1 = activation["output_values"][0]
        assert output1["actuator_id"] == "actuator_ventilador"
        assert output1["dutyCycle"] == 75.5  # Float
        assert "power" not in output1  # NO debe tener power
        assert output1["duration"] == 120

        # Verificar segundo output (dutyCycle)
        output2 = activation["output_values"][1]
        assert output2["actuator_id"] == "actuator_bomba"
        assert output2["dutyCycle"] == 50.0  # Float
        assert "power" not in output2  # NO debe tener power
        assert output2["duration"] == 300

    def test_entity_to_doc_with_mixed_actuators(self):
        """Verifica que _entity_to_doc maneja mezcla de power y dutyCycle."""
        # Crear evaluación con ambos tipos
        evaluation = FuzzyEvaluation(
            id=FuzzyEvaluationId("507f1f77bcf86cd799439011"),
            system_id=FuzzySystemId("507f1f77bcf86cd799439012"),
            timestamp=datetime(2025, 1, 1, 12, 0, 0, tzinfo=timezone.utc),
        )
        evaluation.add_input("sensor_temp", 28.0)

        # Regla con mezcla de digitales y PWM
        rule_activation = RuleActivation(
            rule_id=FuzzyRuleId("507f1f77bcf86cd799439013"),
            firing_strength=0.65,
            output_values=[
                OutputValue(
                    actuator_id="actuator_calefactor",
                    power="ON",  # Digital
                    duration=180,
                ),
                OutputValue(
                    actuator_id="actuator_ventilador",
                    dutyCycle=60.0,  # PWM
                    duration=120,
                ),
            ],
        )
        evaluation.add_rule_activation(rule_activation)

        # Convertir a documento
        doc = FuzzyEvaluationRepository._entity_to_doc(evaluation)

        activation = doc["activated_rules"][0]
        assert len(activation["output_values"]) == 2

        # Verificar output digital
        output_digital = activation["output_values"][0]
        assert output_digital["power"] == "ON"
        assert "dutyCycle" not in output_digital

        # Verificar output PWM
        output_pwm = activation["output_values"][1]
        assert output_pwm["dutyCycle"] == 60.0
        assert "power" not in output_pwm

    def test_doc_to_entity_preserves_power_and_dutycycle(self):
        """Verifica que _doc_to_entity reconstruye correctamente power/dutyCycle."""
        # Documento MongoDB simulado
        doc = {
            "_id": "507f1f77bcf86cd799439011",
            "system_id": "507f1f77bcf86cd799439012",
            "timestamp": datetime(2025, 1, 1, 12, 0, 0, tzinfo=timezone.utc),
            "inputs": [{"sensor_id": "sensor_temp", "value": 25.5}],
            "activated_rules": [
                {
                    "ruleId": "507f1f77bcf86cd799439013",
                    "firingStrength": 0.85,
                    "output_values": [
                        {
                            "actuator_id": "actuator_calefactor",
                            "power": "ON",  # String
                            "duration": 180,
                        },
                        {
                            "actuator_id": "actuator_ventilador",
                            "dutyCycle": 75.5,  # Float
                            "duration": 120,
                        },
                    ],
                }
            ],
        }

        # Convertir a entidad
        entity = FuzzyEvaluationRepository._doc_to_entity(doc)

        # Verificar que la entidad preserva los campos correctamente
        assert len(entity.activated_rules) == 1
        rule = entity.activated_rules[0]
        assert len(rule.output_values) == 2

        # Verificar output digital (power)
        output_digital = rule.output_values[0]
        assert output_digital.power == "ON"
        assert output_digital.dutyCycle is None

        # Verificar output PWM (dutyCycle)
        output_pwm = rule.output_values[1]
        assert output_pwm.power is None
        assert output_pwm.dutyCycle == 75.5
