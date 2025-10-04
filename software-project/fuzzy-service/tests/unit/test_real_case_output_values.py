"""
Prueba unitaria con caso real de evaluación fuzzy.

Basado en el caso reportado donde las reglas se activan correctamente
pero output_values queda vacío.
"""

import pytest
from unittest.mock import MagicMock
from datetime import datetime, timezone

from FuzzyService.Infrastructure.ExternalServices.FuzzyEngine.ScikitFuzzyEngine import ScikitFuzzyEngine
from FuzzyService.Infrastructure.ExternalServices.FuzzyEngine.FuzzyResultTypes import (
    BatchRuleEvaluationResult,
    RuleActivationResult
)


class TestRealCaseOutputValues:
    """Prueba con datos reales del sistema."""

    @pytest.fixture
    def fuzzy_engine(self):
        """Crea una instancia del motor fuzzy."""
        mock_mediator = MagicMock()
        return ScikitFuzzyEngine(mediator=mock_mediator)

    @pytest.fixture
    def real_case_rule_evaluation_result(self):
        """
        Resultado de evaluación de reglas con caso real.

        Simula 5 reglas activadas como en el caso reportado:
        - rule_id: 68e09cdaa37b9aab03c0366e, firing_strength: 0.07857
        - rule_id: 68e09cdaa37b9aab03c0366f, firing_strength: 0.37999
        - ... etc
        """
        # Regla 1
        rule1 = RuleActivationResult(
            rule_id="68e09cdaa37b9aab03c0366e",
            rule_name="Temperatura Baja - Encender Calefactor Aire",
            consequent="68e05365d86d6edc39982900"  # ID de rutina "Encender Calefactor Aire"
        )
        rule1.set_firing_strength(0.07857)

        # Regla 2
        rule2 = RuleActivationResult(
            rule_id="68e09cdaa37b9aab03c0366f",
            rule_name="pH Bajo - Agregar Base",
            consequent="68e05365d86d6edc39982909"  # ID de rutina "Apagar Actuadores de Agua"
        )
        rule2.set_firing_strength(0.37999)

        # Regla 3
        rule3 = RuleActivationResult(
            rule_id="68e09cdaa37b9aab03c03670",
            rule_name="Temperatura Agua Baja - Encender Calefactor Agua",
            consequent="68e05365d86d6edc39982904"  # ID de rutina "Encender Calefactor Agua"
        )
        rule3.set_firing_strength(0.15234)

        # Regla 4
        rule4 = RuleActivationResult(
            rule_id="68e09cdaa37b9aab03c03671",
            rule_name="Humedad Baja - Encender Humidificador",
            consequent="68e05365d86d6edc39982906"  # ID de rutina "Encender Humidificador"
        )
        rule4.set_firing_strength(0.22105)

        # Regla 5
        rule5 = RuleActivationResult(
            rule_id="68e09cdaa37b9aab03c03672",
            rule_name="Nivel Bajo - Activar Alerta",
            consequent="68e05365d86d6edc3998290a"  # ID de rutina "Encender Luz"
        )
        rule5.set_firing_strength(0.08451)

        batch_result = BatchRuleEvaluationResult()
        batch_result.add_rule_result(rule1)
        batch_result.add_rule_result(rule2)
        batch_result.add_rule_result(rule3)
        batch_result.add_rule_result(rule4)
        batch_result.add_rule_result(rule5)
        batch_result.total_processing_time_ms = 15.7

        return batch_result

    @pytest.fixture
    def real_case_routines_payload(self):
        """
        Payload de rutinas generado por defuzzify_routines().

        Incluye:
        - routineId: nombre legible (para actuator-service)
        - _routine_id: ID interno (para mapeo con reglas)
        """
        return [
            {
                "routineId": "Encender Calefactor Aire",
                "_routine_id": "68e05365d86d6edc39982900",
                "steps": [
                    {
                        "outputVariable": "68e04314d86d6edc3998286b",
                        "power": "ON",
                        "duration": 180
                    }
                ]
            },
            {
                "routineId": "Apagar Actuadores de Agua",
                "_routine_id": "68e05365d86d6edc39982909",
                "steps": [
                    {
                        "outputVariable": "68e04314d86d6edc3998286c",
                        "power": "OFF",
                        "duration": 1
                    },
                    {
                        "outputVariable": "68e04314d86d6edc3998286e",
                        "power": "OFF",
                        "duration": 1
                    }
                ]
            },
            {
                "routineId": "Encender Calefactor Agua",
                "_routine_id": "68e05365d86d6edc39982904",
                "steps": [
                    {
                        "outputVariable": "68e04314d86d6edc3998286c",
                        "power": "ON",
                        "duration": 300
                    }
                ]
            },
            {
                "routineId": "Encender Humidificador",
                "_routine_id": "68e05365d86d6edc39982906",
                "steps": [
                    {
                        "outputVariable": "68e04314d86d6edc3998286d",
                        "power": "ON",
                        "duration": 240
                    }
                ]
            },
            {
                "routineId": "Encender Luz",
                "_routine_id": "68e05365d86d6edc3998290a",
                "steps": [
                    {
                        "outputVariable": "68e04314d86d6edc3998286f",
                        "power": "ON",
                        "duration": 7200
                    }
                ]
            }
        ]

    def test_real_case_all_rules_have_output_values(
        self,
        fuzzy_engine,
        real_case_rule_evaluation_result,
        real_case_routines_payload
    ):
        """
        Prueba que reproduce el caso real donde output_values quedaba vacío.

        Entrada:
        - 5 reglas activadas con firing_strength > 0
        - 5 rutinas en routines_payload

        Resultado esperado:
        - Todas las 5 reglas deben tener output_values NO vacíos
        - Cada output_value debe tener actuator_id, power, duration
        """
        result = fuzzy_engine._serialize_rule_evaluation_result(
            real_case_rule_evaluation_result,
            real_case_routines_payload
        )

        # Verificar que hay 5 reglas activadas
        assert len(result["activated_rules"]) == 5

        # Verificar que TODAS las reglas tienen output_values NO vacíos
        for idx, rule in enumerate(result["activated_rules"]):
            assert len(rule["output_values"]) > 0, (
                f"Regla {idx} ('{rule['rule_name']}') tiene output_values vacío. "
                f"Este era el bug reportado."
            )

        # Verificar regla 1: Encender Calefactor Aire
        rule1 = result["activated_rules"][0]
        assert rule1["rule_id"] == "68e09cdaa37b9aab03c0366e"
        assert rule1["firing_strength"] == 0.07857
        assert len(rule1["output_values"]) == 1
        assert rule1["output_values"][0]["actuator_id"] == "68e04314d86d6edc3998286b"
        assert rule1["output_values"][0]["power"] == "ON"  # Mantiene el string "ON"
        assert "dutyCycle" not in rule1["output_values"][0]  # NO debe tener dutyCycle
        assert rule1["output_values"][0]["duration"] == 180

        # Verificar regla 2: Apagar Actuadores de Agua (2 steps)
        rule2 = result["activated_rules"][1]
        assert rule2["rule_id"] == "68e09cdaa37b9aab03c0366f"
        assert rule2["firing_strength"] == 0.37999
        assert len(rule2["output_values"]) == 2  # 2 pasos
        assert rule2["output_values"][0]["power"] == "OFF"  # Mantiene el string "OFF"
        assert "dutyCycle" not in rule2["output_values"][0]  # NO debe tener dutyCycle
        assert rule2["output_values"][1]["power"] == "OFF"  # Mantiene el string "OFF"
        assert "dutyCycle" not in rule2["output_values"][1]  # NO debe tener dutyCycle

        # Verificar regla 3: Encender Calefactor Agua
        rule3 = result["activated_rules"][2]
        assert rule3["rule_id"] == "68e09cdaa37b9aab03c03670"
        assert len(rule3["output_values"]) == 1
        assert rule3["output_values"][0]["power"] == "ON"  # Mantiene el string "ON"
        assert "dutyCycle" not in rule3["output_values"][0]  # NO debe tener dutyCycle

        # Verificar regla 4: Encender Humidificador
        rule4 = result["activated_rules"][3]
        assert rule4["rule_id"] == "68e09cdaa37b9aab03c03671"
        assert len(rule4["output_values"]) == 1
        assert rule4["output_values"][0]["power"] == "ON"  # Mantiene el string "ON"
        assert "dutyCycle" not in rule4["output_values"][0]  # NO debe tener dutyCycle

        # Verificar regla 5: Encender Luz
        rule5 = result["activated_rules"][4]
        assert rule5["rule_id"] == "68e09cdaa37b9aab03c03672"
        assert len(rule5["output_values"]) == 1
        assert rule5["output_values"][0]["power"] == "ON"  # Mantiene el string "ON"
        assert "dutyCycle" not in rule5["output_values"][0]  # NO debe tener dutyCycle

    def test_real_case_routines_payload_structure(
        self,
        real_case_routines_payload
    ):
        """
        Verifica que routines_payload tiene la estructura correcta.

        Debe incluir:
        - routineId: nombre legible
        - _routine_id: ID interno para mapeo
        - steps: lista de pasos
        """
        for routine in real_case_routines_payload:
            assert "routineId" in routine, "Falta routineId (nombre)"
            assert "_routine_id" in routine, "Falta _routine_id (ID interno)"
            assert "steps" in routine, "Falta steps"
            assert isinstance(routine["steps"], list), "steps debe ser lista"
            assert len(routine["steps"]) > 0, "steps no puede estar vacío"

            # Verificar que routineId es un nombre legible (no un ID)
            assert not routine["routineId"].startswith("68e"), (
                f"routineId debe ser nombre, no ID: {routine['routineId']}"
            )

            # Verificar que _routine_id es un ID
            assert len(routine["_routine_id"]) == 24, (
                f"_routine_id debe ser ObjectId de 24 caracteres: {routine['_routine_id']}"
            )
