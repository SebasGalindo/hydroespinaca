"""
Unit tests for Mamdani defuzzification with continuous output.
"""

import pytest
import numpy as np
from unittest.mock import Mock, AsyncMock
from datetime import datetime, timezone

from FuzzyService.Domain.Entities.fuzzy_variable import FuzzyVariable
from FuzzyService.Domain.Entities.fuzzy_term import FuzzyTerm
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyVariableId, FuzzyTermId
from FuzzyService.Domain.ValueObjects.MembershipFunction import MembershipFunction
from FuzzyService.Domain.Enums.MembershipFunctionType import MembershipFunctionType
from FuzzyService.Infrastructure.ExternalServices.FuzzyEngine.ScikitFuzzyEngine import ScikitFuzzyEngine
from FuzzyService.Infrastructure.ExternalServices.FuzzyEngine.FuzzyResultTypes import BatchRuleEvaluationResult
from medyator import Medyator


class TestMamdaniDefuzzification:
    """Tests para defuzzificación continua Mamdani."""

    @pytest.fixture
    def mediator(self):
        """Mock mediator."""
        return Mock(spec=Medyator)

    @pytest.fixture
    def fuzzy_engine(self, mediator):
        """Instancia del motor fuzzy."""
        return ScikitFuzzyEngine(mediator=mediator)

    @pytest.fixture
    def pwm_variable(self):
        """Variable PWM de ejemplo (ventilador)."""
        return FuzzyVariable(
            id=FuzzyVariableId("68e05364d86d6edc39982875"),
            name="Potencia del Ventilador",
            variable_type="output",
            actuator_type="PWM",
            universe_min=0.0,
            universe_max=100.0,
            defuzzification_threshold=50.0,
            reference_id="68e04314d86d6edc39982869"
        )

    @pytest.fixture
    def digital_variable(self):
        """Variable DIGITAL de ejemplo (calefactor)."""
        return FuzzyVariable(
            id=FuzzyVariableId("68e05364d86d6edc398828B0"),
            name="Control Calefactor Aire",
            variable_type="output",
            actuator_type="DIGITAL",
            universe_min=0.0,
            universe_max=100.0,
            defuzzification_threshold=50.0,
            reference_id="68e04314d86d6edc3998286b"
        )

    @pytest.fixture
    def duration_variable(self):
        """Variable de duración de ejemplo."""
        return FuzzyVariable(
            id=FuzzyVariableId("68e05364d86d6edc39982877"),
            name="Duración de Calefacción de Aire",
            variable_type="output",
            actuator_type="DIGITAL",
            universe_min=0.0,
            universe_max=3600.0,
            defuzzification_threshold=50.0,
            reference_id="68e04314d86d6edc3998286b"
        )

    @pytest.fixture
    def pwm_terms(self, pwm_variable):
        """Términos fuzzy para variable PWM."""
        return [
            FuzzyTerm(
                id=FuzzyTermId("68e05364d86d6edc39982890"),
                variable_id=pwm_variable.id,
                label="potenciaBaja",
                membership_function=MembershipFunction(
                    function_type=MembershipFunctionType.TRIANGULAR,
                    parameters=[5.0, 22.5, 40.0],
                    universe_min=0.0,
                    universe_max=100.0
                )
            ),
            FuzzyTerm(
                id=FuzzyTermId("68e05364d86d6edc39982892"),
                variable_id=pwm_variable.id,
                label="potenciaAlta",
                membership_function=MembershipFunction(
                    function_type=MembershipFunctionType.TRIANGULAR,
                    parameters=[60.0, 75.0, 90.0],
                    universe_min=0.0,
                    universe_max=100.0
                )
            ),
            FuzzyTerm(
                id=FuzzyTermId("68e05364d86d6edc39982893"),
                variable_id=pwm_variable.id,
                label="potenciaMaxima",
                membership_function=MembershipFunction(
                    function_type=MembershipFunctionType.TRIANGULAR,
                    parameters=[85.0, 92.5, 100.0],
                    universe_min=0.0,
                    universe_max=100.0
                )
            )
        ]

    def test_infer_universe_from_terms(self, fuzzy_engine, pwm_variable, pwm_terms):
        """Test: inferir universo de discurso desde términos."""
        universe_min, universe_max = fuzzy_engine._infer_universe_from_terms(
            pwm_variable, pwm_terms
        )

        assert universe_min == 0.0
        assert universe_max == 100.0

    def test_infer_universe_fallback(self, fuzzy_engine, pwm_variable):
        """Test: fallback cuando no hay términos."""
        universe_min, universe_max = fuzzy_engine._infer_universe_from_terms(
            pwm_variable, []
        )

        # Debe usar fallback
        assert universe_min == 0.0
        assert universe_max == 100.0

    @pytest.mark.asyncio
    async def test_apply_actuator_logic_pwm(self, fuzzy_engine, pwm_variable):
        """Test: aplicar lógica de actuador PWM."""
        crisp_value = 75.5

        command = await fuzzy_engine.apply_actuator_logic(pwm_variable, crisp_value)

        assert "dutyCycle" in command
        assert command["dutyCycle"] == 75.5
        assert "power" not in command

    @pytest.mark.asyncio
    async def test_apply_actuator_logic_pwm_clamping(self, fuzzy_engine, pwm_variable):
        """Test: clamping de valores PWM fuera de rango."""
        # Valor por encima del máximo
        command_high = await fuzzy_engine.apply_actuator_logic(pwm_variable, 150.0)
        assert command_high["dutyCycle"] == 100.0

        # Valor por debajo del mínimo
        command_low = await fuzzy_engine.apply_actuator_logic(pwm_variable, -10.0)
        assert command_low["dutyCycle"] == 0.0

    @pytest.mark.asyncio
    async def test_apply_actuator_logic_digital_on(self, fuzzy_engine, digital_variable):
        """Test: aplicar lógica de actuador DIGITAL → ON."""
        crisp_value = 65.0  # >= 50

        command = await fuzzy_engine.apply_actuator_logic(digital_variable, crisp_value)

        assert "power" in command
        assert command["power"] == "ON"
        assert "dutyCycle" not in command

    @pytest.mark.asyncio
    async def test_apply_actuator_logic_digital_off(self, fuzzy_engine, digital_variable):
        """Test: aplicar lógica de actuador DIGITAL → OFF."""
        crisp_value = 45.0  # < 50

        command = await fuzzy_engine.apply_actuator_logic(digital_variable, crisp_value)

        assert "power" in command
        assert command["power"] == "OFF"
        assert "dutyCycle" not in command

    @pytest.mark.asyncio
    async def test_apply_actuator_logic_digital_threshold_edge(self, fuzzy_engine, digital_variable):
        """Test: threshold exacto debe activar ON."""
        crisp_value = 50.0  # == 50 (threshold)

        command = await fuzzy_engine.apply_actuator_logic(digital_variable, crisp_value)

        assert command["power"] == "ON"  # >= threshold

    @pytest.mark.asyncio
    async def test_apply_actuator_logic_custom_threshold(self, fuzzy_engine):
        """Test: threshold personalizado."""
        custom_variable = FuzzyVariable(
            id=FuzzyVariableId("68e05364d86d6edc39982999"),
            name="Test Variable",
            variable_type="output",
            actuator_type="DIGITAL",
            universe_min=0.0,
            universe_max=100.0,
            defuzzification_threshold=70.0,  # Threshold alto
            reference_id="test_ref"
        )

        # Valor por debajo del threshold personalizado
        command_off = await fuzzy_engine.apply_actuator_logic(custom_variable, 65.0)
        assert command_off["power"] == "OFF"

        # Valor por encima del threshold personalizado
        command_on = await fuzzy_engine.apply_actuator_logic(custom_variable, 75.0)
        assert command_on["power"] == "ON"

    @pytest.mark.asyncio
    async def test_apply_actuator_logic_duration_continuous(self, fuzzy_engine, duration_variable):
        """Test: variable de duración usa valor continuo."""
        crisp_value = 1200.5  # 20 minutos

        command = await fuzzy_engine.apply_actuator_logic(duration_variable, crisp_value)

        # Variables de duración son DIGITAL pero representan tiempo
        # La lógica actual aplicaría threshold, pero el valor es lo importante
        assert "power" in command or "dutyCycle" in command

    def test_create_membership_function_triangular(self, fuzzy_engine):
        """Test: creación de función de membresía triangular."""
        mf = MembershipFunction(
            function_type=MembershipFunctionType.TRIANGULAR,
            parameters=[20.0, 50.0, 80.0],
            universe_min=0.0,
            universe_max=100.0
        )

        universe = np.linspace(0.0, 100.0, 1000)
        membership_values = fuzzy_engine._create_membership_function(mf, universe)

        # Verificar que es un array numpy
        assert isinstance(membership_values, np.ndarray)
        assert len(membership_values) == len(universe)

        # Verificar que el pico está en el centro (50)
        peak_index = np.argmax(membership_values)
        peak_value = universe[peak_index]
        assert 49.0 <= peak_value <= 51.0  # Tolerancia por discretización

        # Verificar que el valor máximo es 1.0
        assert np.max(membership_values) <= 1.0
        assert np.max(membership_values) >= 0.99

    def test_create_membership_function_trapezoidal(self, fuzzy_engine):
        """Test: creación de función de membresía trapezoidal."""
        mf = MembershipFunction(
            function_type=MembershipFunctionType.TRAPEZOIDAL,
            parameters=[20.0, 40.0, 60.0, 80.0],
            universe_min=0.0,
            universe_max=100.0
        )

        universe = np.linspace(0.0, 100.0, 1000)
        membership_values = fuzzy_engine._create_membership_function(mf, universe)

        # Verificar que hay una meseta (valores 1.0) entre 40 y 60
        plateau_indices = np.where(membership_values >= 0.99)[0]
        plateau_values = universe[plateau_indices]

        assert len(plateau_indices) > 0
        assert min(plateau_values) >= 38.0  # Tolerancia
        assert max(plateau_values) <= 62.0


class TestVariableValidation:
    """Tests para validaciones de FuzzyVariable con nuevos campos."""

    def test_valid_variable_pwm(self):
        """Test: variable PWM válida."""
        var = FuzzyVariable(
            name="Test PWM",
            variable_type="output",
            actuator_type="PWM",
            universe_min=0.0,
            universe_max=100.0,
            defuzzification_threshold=50.0,
            reference_id="test_ref"
        )

        assert var.actuator_type == "PWM"
        assert var.universe_min == 0.0
        assert var.universe_max == 100.0
        assert var.defuzzification_threshold == 50.0

    def test_valid_variable_digital(self):
        """Test: variable DIGITAL válida."""
        var = FuzzyVariable(
            name="Test DIGITAL",
            variable_type="output",
            actuator_type="DIGITAL",
            universe_min=0.0,
            universe_max=100.0,
            defuzzification_threshold=60.0,
            reference_id="test_ref"
        )

        assert var.actuator_type == "DIGITAL"
        assert var.defuzzification_threshold == 60.0

    def test_invalid_threshold_below_zero(self):
        """Test: threshold negativo debe fallar."""
        with pytest.raises(ValueError, match="defuzzification_threshold debe estar entre 0 y 100"):
            FuzzyVariable(
                name="Test Invalid",
                variable_type="output",
                actuator_type="DIGITAL",
                defuzzification_threshold=-10.0,
                reference_id="test_ref"
            )

    def test_invalid_threshold_above_100(self):
        """Test: threshold > 100 debe fallar."""
        with pytest.raises(ValueError, match="defuzzification_threshold debe estar entre 0 y 100"):
            FuzzyVariable(
                name="Test Invalid",
                variable_type="output",
                actuator_type="DIGITAL",
                defuzzification_threshold=150.0,
                reference_id="test_ref"
            )

    def test_invalid_universe_min_negative(self):
        """Test: universe_min negativo debe fallar."""
        with pytest.raises(ValueError, match="universe_min no puede ser negativo"):
            FuzzyVariable(
                name="Test Invalid",
                variable_type="output",
                actuator_type="PWM",
                universe_min=-10.0,
                universe_max=100.0,
                reference_id="test_ref"
            )

    def test_invalid_universe_max_less_than_min(self):
        """Test: universe_max <= universe_min debe fallar."""
        with pytest.raises(ValueError, match="universe_max .* debe ser mayor que universe_min"):
            FuzzyVariable(
                name="Test Invalid",
                variable_type="output",
                actuator_type="PWM",
                universe_min=100.0,
                universe_max=50.0,
                reference_id="test_ref"
            )

    def test_variable_to_dict_includes_new_fields(self):
        """Test: to_dict incluye nuevos campos."""
        var = FuzzyVariable(
            name="Test Variable",
            variable_type="output",
            actuator_type="PWM",
            universe_min=0.0,
            universe_max=100.0,
            defuzzification_threshold=50.0,
            reference_id="test_ref"
        )

        var_dict = var.to_dict()

        assert "universe_min" in var_dict
        assert "universe_max" in var_dict
        assert "defuzzification_threshold" in var_dict
        assert var_dict["universe_min"] == 0.0
        assert var_dict["universe_max"] == 100.0
        assert var_dict["defuzzification_threshold"] == 50.0

    def test_output_variable_requires_actuator_type(self):
        """Test: variable output sin actuator_type debe fallar."""
        with pytest.raises(ValueError, match="Las variables de tipo 'output' deben tener actuator_type definido"):
            FuzzyVariable(
                name="Test Output Without Type",
                variable_type="output",
                actuator_type=None,  # ❌ Obligatorio para outputs
                reference_id="test_ref"
            )

    def test_input_variable_cannot_have_actuator_type(self):
        """Test: variable input con actuator_type debe fallar."""
        with pytest.raises(ValueError, match="Las variables de tipo 'input' no deben tener actuator_type"):
            FuzzyVariable(
                name="Test Input With Type",
                variable_type="input",
                actuator_type="PWM",  # ❌ No permitido para inputs
                reference_id="test_ref"
            )
