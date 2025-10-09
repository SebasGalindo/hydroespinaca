"""
Test unitario para verificar las validaciones de OutputValue.

Valida que Pydantic rechace valores inválidos en power y dutyCycle.
"""

import pytest
from pydantic import ValidationError

from FuzzyService.Domain.Entities.fuzzy_evaluation import OutputValue


class TestOutputValueValidation:
    """Tests para validaciones de OutputValue."""

    def test_power_must_be_on_or_off(self):
        """Verifica que power solo acepte 'ON' o 'OFF'."""
        # Válido: "ON"
        output_on = OutputValue(
            actuator_id="actuator_test",
            power="ON",
            duration=60
        )
        assert output_on.power == "ON"

        # Válido: "OFF"
        output_off = OutputValue(
            actuator_id="actuator_test",
            power="OFF",
            duration=60
        )
        assert output_off.power == "OFF"

        # Inválido: número en lugar de string
        with pytest.raises(ValidationError) as exc_info:
            OutputValue(
                actuator_id="actuator_test",
                power=100,  # ❌ Debería ser "ON"
                duration=60
            )
        assert "power" in str(exc_info.value)

        # Inválido: float en lugar de string
        with pytest.raises(ValidationError) as exc_info:
            OutputValue(
                actuator_id="actuator_test",
                power=0.0,  # ❌ Debería ser "OFF"
                duration=60
            )
        assert "power" in str(exc_info.value)

        # Inválido: string inválido
        with pytest.raises(ValidationError) as exc_info:
            OutputValue(
                actuator_id="actuator_test",
                power="MEDIUM",  # ❌ Solo "ON" o "OFF"
                duration=60
            )
        assert "power debe ser 'ON' o 'OFF'" in str(exc_info.value)

    def test_duty_cycle_must_be_numeric_0_100(self):
        """Verifica que dutyCycle solo acepte números entre 0 y 100."""
        # Válido: 0
        output_0 = OutputValue(
            actuator_id="actuator_test",
            dutyCycle=0,
            duration=60
        )
        assert output_0.dutyCycle == 0.0

        # Válido: 50.5
        output_50 = OutputValue(
            actuator_id="actuator_test",
            dutyCycle=50.5,
            duration=60
        )
        assert output_50.dutyCycle == 50.5

        # Válido: 100
        output_100 = OutputValue(
            actuator_id="actuator_test",
            dutyCycle=100,
            duration=60
        )
        assert output_100.dutyCycle == 100.0

        # Inválido: mayor que 100
        with pytest.raises(ValidationError) as exc_info:
            OutputValue(
                actuator_id="actuator_test",
                dutyCycle=150,  # ❌ Fuera de rango
                duration=60
            )
        assert "dutyCycle debe estar entre 0 y 100" in str(exc_info.value)

        # Inválido: negativo
        with pytest.raises(ValidationError) as exc_info:
            OutputValue(
                actuator_id="actuator_test",
                dutyCycle=-10,  # ❌ Fuera de rango
                duration=60
            )
        assert "dutyCycle debe estar entre 0 y 100" in str(exc_info.value)

    def test_at_least_one_field_required(self):
        """Verifica que al menos power o dutyCycle deba estar definido."""
        # Inválido: ninguno definido
        with pytest.raises(ValidationError) as exc_info:
            OutputValue(
                actuator_id="actuator_test",
                duration=60
            )
        assert "Debe definirse power o dutyCycle" in str(exc_info.value)

    def test_power_and_dutycycle_can_both_be_none(self):
        """
        Verifica que si se pasan ambos como None explícitamente,
        se rechace (debe haber al menos uno).
        """
        with pytest.raises(ValidationError) as exc_info:
            OutputValue(
                actuator_id="actuator_test",
                power=None,
                dutyCycle=None,
                duration=60
            )
        assert "Debe definirse power o dutyCycle" in str(exc_info.value)

    def test_valid_digital_actuator(self):
        """Verifica caso válido completo para actuador digital."""
        output = OutputValue(
            actuator_id="actuator_calefactor",
            power="ON",
            duration=180
        )
        assert output.actuator_id == "actuator_calefactor"
        assert output.power == "ON"
        assert output.dutyCycle is None
        assert output.duration == 180

    def test_valid_pwm_actuator(self):
        """Verifica caso válido completo para actuador PWM."""
        output = OutputValue(
            actuator_id="actuator_ventilador",
            dutyCycle=75.5,
            duration=120
        )
        assert output.actuator_id == "actuator_ventilador"
        assert output.power is None
        assert output.dutyCycle == 75.5
        assert output.duration == 120

    def test_to_dict_includes_only_defined_field(self):
        """Verifica que to_dict solo incluya el campo definido."""
        # Digital: solo power
        output_digital = OutputValue(
            actuator_id="actuator_calefactor",
            power="ON",
            duration=180
        )
        dict_digital = output_digital.to_dict()
        assert "power" in dict_digital
        assert "dutyCycle" not in dict_digital
        assert dict_digital["power"] == "ON"

        # PWM: solo dutyCycle
        output_pwm = OutputValue(
            actuator_id="actuator_ventilador",
            dutyCycle=75.5,
            duration=120
        )
        dict_pwm = output_pwm.to_dict()
        assert "dutyCycle" in dict_pwm
        assert "power" not in dict_pwm
        assert dict_pwm["dutyCycle"] == 75.5

    def test_duration_out_of_range(self):
        """Verifica que duration deba estar dentro del rango permitido."""
        # La configuración por defecto es SECONDS_5_60 (0.5-10000 segundos)
        # Válido: dentro de rango
        output_valid = OutputValue(
            actuator_id="actuator_test",
            power="ON",
            duration=300
        )
        assert output_valid.duration == 300

        # Inválido: menor que 0.5
        with pytest.raises(ValidationError) as exc_info:
            OutputValue(
                actuator_id="actuator_test",
                power="ON",
                duration=0.1  # ❌ Menor que 0.5
            )
        assert "duration fuera de rango permitido" in str(exc_info.value)

        # Inválido: mayor que 10000
        with pytest.raises(ValidationError) as exc_info:
            OutputValue(
                actuator_id="actuator_test",
                power="ON",
                duration=20000  # ❌ Mayor que 10000
            )
        assert "duration fuera de rango permitido" in str(exc_info.value)
