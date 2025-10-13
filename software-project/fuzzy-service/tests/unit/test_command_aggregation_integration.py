"""
Test de integración para verificar que no haya comandos duplicados
en el flujo completo de evaluación fuzzy.

Este test simula el escenario reportado donde múltiples reglas
generan comandos para el mismo actuador.
"""

import pytest
from FuzzyService.Domain.Entities.fuzzy_evaluation import FuzzyEvaluation, RuleActivation, OutputValue, InputValue
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId, FuzzyRuleId, FuzzyEvaluationId
from FuzzyService.Application.Services.CommandAggregator import CommandAggregator
from datetime import datetime, timezone


class TestCommandAggregationIntegration:
    """Tests de integración para agregación de comandos."""

    def test_duplicate_actuator_commands_are_consolidated(self):
        """
        Test: Escenario del problema reportado - múltiples reglas afectan
        al mismo actuador (calefactor-agua) y los comandos se consolidan.
        """
        # Crear evaluación fuzzy con 2 reglas que afectan el mismo actuador
        evaluation = FuzzyEvaluation(
            id=FuzzyEvaluationId.generate(),
            system_id=FuzzySystemId.generate(),
            timestamp=datetime.now(timezone.utc),
            inputs=[
                InputValue(sensor_id="T_AGUA", value=18.5),
                InputValue(sensor_id="T_AMB", value=22.0)
            ],
            activated_rules=[
                # Regla 1: "Si temperatura agua es fría → calentar agua fuerte"
                RuleActivation(
                    rule_id=FuzzyRuleId.generate(),
                    firing_strength=0.85,
                    output_values=[
                        OutputValue(
                            reference_code="calefactor-agua",
                            dutyCycle=80.0,
                            duration=5.0
                        )
                    ]
                ),
                # Regla 2: "Si temperatura ambiente es fresca → calentar agua moderado"
                RuleActivation(
                    rule_id=FuzzyRuleId.generate(),
                    firing_strength=0.65,
                    output_values=[
                        OutputValue(
                            reference_code="calefactor-agua",
                            dutyCycle=96.33,  # Valor del centroide reportado en logs
                            duration=8.0
                        )
                    ]
                )
            ]
        )

        # Recolectar todos los output_values
        all_output_values = []
        for rule_activation in evaluation.activated_rules:
            all_output_values.extend(rule_activation.output_values)

        # Verificar que hay duplicados antes de agregar
        assert len(all_output_values) == 2
        actuator_codes = [ov.reference_code for ov in all_output_values]
        assert actuator_codes == ["calefactor-agua", "calefactor-agua"]

        # Aplicar agregación
        aggregator = CommandAggregator(aggregation_method="max")
        commands = aggregator.aggregate_commands(all_output_values)

        # Verificar que solo hay 1 comando después de la agregación
        assert len(commands) == 1, "Debe haber exactamente 1 comando (sin duplicados)"

        # Verificar el comando consolidado
        command = commands[0]
        assert command["actuatorCode"] == "calefactor-agua"
        # Debe usar el máximo dutyCycle
        assert command["dutyCycle"] == 96.33
        # Debe usar el máximo duration
        assert command["duration"] == 8.0

    def test_multiple_actuators_with_mixed_duplicates(self):
        """
        Test: Escenario real con múltiples actuadores donde algunos
        tienen duplicados y otros no.
        """
        evaluation = FuzzyEvaluation(
            id=FuzzyEvaluationId.generate(),
            system_id=FuzzySystemId.generate(),
            timestamp=datetime.now(timezone.utc),
            inputs=[
                InputValue(sensor_id="T_AGUA", value=18.5),
                InputValue(sensor_id="T_AMB", value=22.0),
                InputValue(sensor_id="HUMEDAD", value=45.0)
            ],
            activated_rules=[
                # Regla 1: Calentar agua + ventilar
                RuleActivation(
                    rule_id=FuzzyRuleId.generate(),
                    firing_strength=0.85,
                    output_values=[
                        OutputValue(
                            reference_code="calefactor-agua",
                            dutyCycle=80.0,
                            duration=5.0
                        ),
                        OutputValue(
                            reference_code="ventilador",
                            dutyCycle=60.0,
                            duration=10.0
                        )
                    ]
                ),
                # Regla 2: Calentar agua + humidificar
                RuleActivation(
                    rule_id=FuzzyRuleId.generate(),
                    firing_strength=0.65,
                    output_values=[
                        OutputValue(
                            reference_code="calefactor-agua",
                            dutyCycle=96.33,
                            duration=8.0
                        ),
                        OutputValue(
                            reference_code="humidificador",
                            power="ON",
                            duration=15.0
                        )
                    ]
                ),
                # Regla 3: Solo ventilar
                RuleActivation(
                    rule_id=FuzzyRuleId.generate(),
                    firing_strength=0.50,
                    output_values=[
                        OutputValue(
                            reference_code="ventilador",
                            dutyCycle=70.0,
                            duration=12.0
                        )
                    ]
                )
            ]
        )

        # Recolectar todos los output_values
        all_output_values = []
        for rule_activation in evaluation.activated_rules:
            all_output_values.extend(rule_activation.output_values)

        # Verificar cantidad antes de agregar
        assert len(all_output_values) == 5

        # Aplicar agregación
        aggregator = CommandAggregator(aggregation_method="max")
        commands = aggregator.aggregate_commands(all_output_values)

        # Verificar que hay 3 comandos únicos
        assert len(commands) == 3

        # Verificar cada actuador
        actuator_codes = {cmd["actuatorCode"] for cmd in commands}
        assert actuator_codes == {"calefactor-agua", "ventilador", "humidificador"}

        # Verificar calefactor-agua (2 duplicados → 1 consolidado)
        calefactor = next(c for c in commands if c["actuatorCode"] == "calefactor-agua")
        assert calefactor["dutyCycle"] == 96.33  # max(80.0, 96.33)
        assert calefactor["duration"] == 8.0     # max(5.0, 8.0)

        # Verificar ventilador (2 duplicados → 1 consolidado)
        ventilador = next(c for c in commands if c["actuatorCode"] == "ventilador")
        assert ventilador["dutyCycle"] == 70.0   # max(60.0, 70.0)
        assert ventilador["duration"] == 12.0    # max(10.0, 12.0)

        # Verificar humidificador (sin duplicados)
        humidificador = next(c for c in commands if c["actuatorCode"] == "humidificador")
        assert humidificador["power"] == "ON"
        assert humidificador["duration"] == 15.0

    def test_no_duplicates_maintains_all_commands(self):
        """
        Test: Cuando no hay duplicados, todos los comandos se mantienen.
        """
        evaluation = FuzzyEvaluation(
            id=FuzzyEvaluationId.generate(),
            system_id=FuzzySystemId.generate(),
            timestamp=datetime.now(timezone.utc),
            inputs=[
                InputValue(sensor_id="T_AGUA", value=20.0)
            ],
            activated_rules=[
                RuleActivation(
                    rule_id=FuzzyRuleId.generate(),
                    firing_strength=0.75,
                    output_values=[
                        OutputValue(
                            reference_code="bomba-riego",
                            dutyCycle=80.0,
                            duration=10.0
                        ),
                        OutputValue(
                            reference_code="bomba-aireacion",
                            dutyCycle=60.0,
                            duration=15.0
                        ),
                        OutputValue(
                            reference_code="luz-crecimiento",
                            power="ON",
                            duration=7200.0
                        )
                    ]
                )
            ]
        )

        all_output_values = []
        for rule_activation in evaluation.activated_rules:
            all_output_values.extend(rule_activation.output_values)

        aggregator = CommandAggregator(aggregation_method="max")
        commands = aggregator.aggregate_commands(all_output_values)

        # Todos los comandos deben mantenerse (no hay duplicados)
        assert len(commands) == 3
        actuator_codes = {cmd["actuatorCode"] for cmd in commands}
        assert actuator_codes == {"bomba-riego", "bomba-aireacion", "luz-crecimiento"}


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
