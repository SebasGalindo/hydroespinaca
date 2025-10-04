"""
Pruebas unitarias para validar la generación de output_values en fuzzy evaluations.

Verifica que cuando una regla se activa, los valores de salida (output_values) se
generan correctamente a partir de las rutinas defuzzificadas.
"""

import pytest
from unittest.mock import MagicMock, AsyncMock
from datetime import datetime, timezone
from typing import Dict, List, Any

from FuzzyService.Infrastructure.ExternalServices.FuzzyEngine.ScikitFuzzyEngine import ScikitFuzzyEngine
from FuzzyService.Infrastructure.ExternalServices.FuzzyEngine.FuzzyResultTypes import (
    BatchRuleEvaluationResult,
    RuleActivationResult
)


class TestOutputValuesGeneration:
    """Pruebas para validar la generación de output_values."""

    @pytest.fixture
    def fuzzy_engine(self):
        """Crea una instancia del motor fuzzy para pruebas."""
        # Crear mock de mediator para evitar inyección de dependencias
        mock_mediator = MagicMock()
        return ScikitFuzzyEngine(mediator=mock_mediator)

    @pytest.fixture
    def sample_rule_evaluation_result(self):
        """Resultado de evaluación de reglas con 2 reglas activadas."""
        rule1 = RuleActivationResult(
            rule_id="rule_001",
            rule_name="Temperatura Alta",
            consequent="routine_001"  # Rutina consecuente
        )
        rule1.set_firing_strength(0.85)
        rule1.evaluation_time_ms = 5.2

        rule2 = RuleActivationResult(
            rule_id="rule_002",
            rule_name="Humedad Baja",
            consequent="routine_002"  # Rutina consecuente
        )
        rule2.set_firing_strength(0.72)
        rule2.evaluation_time_ms = 4.8

        batch_result = BatchRuleEvaluationResult()
        batch_result.add_rule_result(rule1)
        batch_result.add_rule_result(rule2)
        batch_result.total_processing_time_ms = 10.0

        return batch_result

    @pytest.fixture
    def sample_routines_payload_digital(self):
        """Payload de rutinas con actuadores DIGITALES (power: ON/OFF)."""
        return [
            {
                "routineId": "Encender Calefactor Aire",
                "_routine_id": "routine_001",  # ID interno para mapeo
                "steps": [
                    {
                        "outputVariable": "actuator_calefactor_aire",
                        "power": "ON",  # DIGITAL
                        "duration": 180
                    }
                ]
            },
            {
                "routineId": "Encender Humidificador",
                "_routine_id": "routine_002",  # ID interno para mapeo
                "steps": [
                    {
                        "outputVariable": "actuator_humidificador",
                        "power": "ON",  # DIGITAL
                        "duration": 300
                    }
                ]
            }
        ]

    @pytest.fixture
    def sample_routines_payload_pwm(self):
        """Payload de rutinas con actuadores PWM (dutyCycle)."""
        return [
            {
                "routineId": "Encender Ventilador",
                "_routine_id": "routine_001",
                "steps": [
                    {
                        "outputVariable": "actuator_ventilador",
                        "dutyCycle": 75.5,  # PWM
                        "duration": 120
                    }
                ]
            },
            {
                "routineId": "Encender Bomba Aire",
                "_routine_id": "routine_002",
                "steps": [
                    {
                        "outputVariable": "actuator_bomba_aire",
                        "dutyCycle": 92.0,  # PWM
                        "duration": 600
                    }
                ]
            }
        ]

    @pytest.fixture
    def sample_routines_payload_mixed(self):
        """Payload de rutinas con actuadores mixtos (DIGITAL + PWM)."""
        return [
            {
                "routineId": "Emergencia Termica",
                "_routine_id": "routine_001",
                "steps": [
                    {
                        "outputVariable": "actuator_calefactor",
                        "power": "ON",  # DIGITAL
                        "duration": 180
                    },
                    {
                        "outputVariable": "actuator_ventilador",
                        "dutyCycle": 75,  # PWM
                        "duration": 120
                    }
                ]
            }
        ]

    def test_serialize_rule_evaluation_result_with_digital_actuators(
        self,
        fuzzy_engine,
        sample_rule_evaluation_result,
        sample_routines_payload_digital
    ):
        """
        Prueba que output_values se generan correctamente para actuadores DIGITALES.

        Verifica que:
        1. output_values no está vacío
        2. actuator_id se extrae de "outputVariable"
        3. power se mantiene como "ON" (string)
        4. NO hay dutyCycle
        5. duration se extrae correctamente
        """
        result = fuzzy_engine._serialize_rule_evaluation_result(
            sample_rule_evaluation_result,
            sample_routines_payload_digital
        )

        # Verificar que hay reglas activadas
        assert len(result["activated_rules"]) == 2

        # Verificar primera regla
        rule1 = result["activated_rules"][0]
        assert rule1["rule_id"] == "rule_001"
        assert rule1["firing_strength"] == 0.85
        assert len(rule1["output_values"]) == 1  # ✅ NO DEBE ESTAR VACÍO

        output1 = rule1["output_values"][0]
        assert output1["actuator_id"] == "actuator_calefactor_aire"
        assert output1["power"] == "ON"  # Mantiene el string "ON"
        assert "dutyCycle" not in output1  # NO debe tener dutyCycle
        assert output1["duration"] == 180

        # Verificar segunda regla
        rule2 = result["activated_rules"][1]
        assert rule2["rule_id"] == "rule_002"
        assert rule2["firing_strength"] == 0.72
        assert len(rule2["output_values"]) == 1  # ✅ NO DEBE ESTAR VACÍO

        output2 = rule2["output_values"][0]
        assert output2["actuator_id"] == "actuator_humidificador"
        assert output2["power"] == "ON"  # Mantiene el string "ON"
        assert "dutyCycle" not in output2  # NO debe tener dutyCycle
        assert output2["duration"] == 300

    def test_serialize_rule_evaluation_result_with_pwm_actuators(
        self,
        fuzzy_engine,
        sample_rule_evaluation_result,
        sample_routines_payload_pwm
    ):
        """
        Prueba que output_values se generan correctamente para actuadores PWM.

        Verifica que:
        1. output_values no está vacío
        2. actuator_id se extrae de "outputVariable"
        3. dutyCycle se mantiene como número
        4. NO hay power
        5. duration se extrae correctamente
        """
        result = fuzzy_engine._serialize_rule_evaluation_result(
            sample_rule_evaluation_result,
            sample_routines_payload_pwm
        )

        # Verificar que hay reglas activadas
        assert len(result["activated_rules"]) == 2

        # Verificar primera regla
        rule1 = result["activated_rules"][0]
        assert len(rule1["output_values"]) == 1  # ✅ NO DEBE ESTAR VACÍO

        output1 = rule1["output_values"][0]
        assert output1["actuator_id"] == "actuator_ventilador"
        assert output1["dutyCycle"] == 75.5  # Mantiene dutyCycle como número
        assert "power" not in output1  # NO debe tener power
        assert output1["duration"] == 120

        # Verificar segunda regla
        rule2 = result["activated_rules"][1]
        assert len(rule2["output_values"]) == 1  # ✅ NO DEBE ESTAR VACÍO

        output2 = rule2["output_values"][0]
        assert output2["actuator_id"] == "actuator_bomba_aire"
        assert output2["dutyCycle"] == 92.0  # Mantiene dutyCycle como número
        assert "power" not in output2  # NO debe tener power
        assert output2["duration"] == 600

    def test_serialize_rule_evaluation_result_with_mixed_actuators(
        self,
        fuzzy_engine,
        sample_rule_evaluation_result,
        sample_routines_payload_mixed
    ):
        """
        Prueba que output_values se generan correctamente para rutinas con múltiples steps.

        Verifica que una rutina con varios pasos (DIGITAL + PWM) genera múltiples output_values.
        """
        # Ajustar el resultado para tener solo 1 regla con múltiples steps
        rule1 = sample_rule_evaluation_result.rule_results[0]
        adjusted_result = BatchRuleEvaluationResult()
        adjusted_result.add_rule_result(rule1)
        adjusted_result.total_processing_time_ms = 5.0

        result = fuzzy_engine._serialize_rule_evaluation_result(
            adjusted_result,
            sample_routines_payload_mixed
        )

        # Verificar que hay 1 regla activada
        assert len(result["activated_rules"]) == 1

        rule = result["activated_rules"][0]
        assert len(rule["output_values"]) == 2  # ✅ 2 steps → 2 output_values

        # Verificar primer output (DIGITAL)
        output1 = rule["output_values"][0]
        assert output1["actuator_id"] == "actuator_calefactor"
        assert output1["power"] == "ON"  # Mantiene el string "ON"
        assert "dutyCycle" not in output1  # NO debe tener dutyCycle
        assert output1["duration"] == 180

        # Verificar segundo output (PWM)
        output2 = rule["output_values"][1]
        assert output2["actuator_id"] == "actuator_ventilador"
        assert output2["dutyCycle"] == 75  # Mantiene dutyCycle como número
        assert "power" not in output2  # NO debe tener power
        assert output2["duration"] == 120

    def test_serialize_rule_evaluation_result_with_off_power(self, fuzzy_engine):
        """
        Prueba que power "OFF" se mantiene como string "OFF".
        """
        rule = RuleActivationResult(
            rule_id="rule_003",
            rule_name="Apagar Calefactor",
            consequent="routine_003"
        )
        rule.set_firing_strength(0.95)
        rule.evaluation_time_ms = 3.5

        result_obj = BatchRuleEvaluationResult()
        result_obj.add_rule_result(rule)
        result_obj.total_processing_time_ms = 3.5

        routines_payload = [
            {
                "routineId": "Apagar Calefactor",
                "_routine_id": "routine_003",
                "steps": [
                    {
                        "outputVariable": "actuator_calefactor",
                        "power": "OFF",  # DIGITAL OFF
                        "duration": 1
                    }
                ]
            }
        ]

        result = fuzzy_engine._serialize_rule_evaluation_result(result_obj, routines_payload)

        assert len(result["activated_rules"]) == 1
        rule_result = result["activated_rules"][0]
        assert len(rule_result["output_values"]) == 1

        output = rule_result["output_values"][0]
        assert output["power"] == "OFF"  # Mantiene el string "OFF"
        assert "dutyCycle" not in output  # NO debe tener dutyCycle

    def test_serialize_rule_evaluation_result_with_no_matching_routine(self, fuzzy_engine):
        """
        Prueba que si no se encuentra la rutina, output_values queda vacío.
        """
        rule = RuleActivationResult(
            rule_id="rule_004",
            rule_name="Rutina No Encontrada",
            consequent="routine_nonexistent"  # No existe
        )
        rule.set_firing_strength(0.60)
        rule.evaluation_time_ms = 2.0

        result_obj = BatchRuleEvaluationResult()
        result_obj.add_rule_result(rule)
        result_obj.total_processing_time_ms = 2.0

        routines_payload = [
            {
                "_routine_id": "routine_other",
                "steps": []
            }
        ]

        result = fuzzy_engine._serialize_rule_evaluation_result(result_obj, routines_payload)

        assert len(result["activated_rules"]) == 1
        rule_result = result["activated_rules"][0]
        assert len(rule_result["output_values"]) == 0  # ✅ Vacío porque no se encontró la rutina


@pytest.mark.asyncio
class TestOutputValuesIntegration:
    """Pruebas de integración para validar el flujo completo."""

    async def test_full_evaluation_generates_output_values(self):
        """
        Prueba de integración que simula el flujo completo desde readings hasta
        fuzzy_evaluation con output_values poblados.

        Este test simula:
        1. Readings de sensores
        2. Evaluación de reglas (con reglas activadas)
        3. Defuzzificación de rutinas
        4. Generación de fuzzy_evaluation con output_values
        """
        # Este test requiere mocks de repositorios y servicios completos
        # Por ahora, validamos que la lógica de serialización funciona correctamente
        # con los tests unitarios anteriores
        pass  # Implementar en fase 2 si se requiere
