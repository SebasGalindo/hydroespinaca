"""
Tests unitarios para CommandAggregator.

Verifica que el agregador consolide correctamente múltiples comandos
del mismo actuador usando diferentes métodos de agregación.
"""

import pytest
from FuzzyService.Application.Services.CommandAggregator import CommandAggregator
from FuzzyService.Domain.Entities.fuzzy_evaluation import OutputValue


class TestCommandAggregator:
    """Tests para el servicio CommandAggregator."""

    def test_single_command_no_aggregation(self):
        """Test: Un solo comando no requiere agregación."""
        aggregator = CommandAggregator(aggregation_method="max")

        output_values = [
            OutputValue(
                reference_code="bomba-riego",
                dutyCycle=75.0,
                duration=10.0
            )
        ]

        commands = aggregator.aggregate_commands(output_values)

        assert len(commands) == 1
        assert commands[0]["actuatorCode"] == "bomba-riego"
        assert commands[0]["dutyCycle"] == 75.0
        assert commands[0]["duration"] == 10.0

    def test_duplicate_pwm_max_aggregation(self):
        """Test: Agregar comandos PWM duplicados con método max."""
        aggregator = CommandAggregator(aggregation_method="max")

        output_values = [
            OutputValue(
                reference_code="calefactor-agua",
                dutyCycle=80.0,
                duration=5.0
            ),
            OutputValue(
                reference_code="calefactor-agua",
                dutyCycle=95.0,
                duration=8.0
            )
        ]

        commands = aggregator.aggregate_commands(output_values)

        # Debe haber solo 1 comando consolidado
        assert len(commands) == 1
        assert commands[0]["actuatorCode"] == "calefactor-agua"
        # Debe usar el máximo dutyCycle
        assert commands[0]["dutyCycle"] == 95.0
        # Debe usar el máximo duration
        assert commands[0]["duration"] == 8.0

    def test_duplicate_pwm_avg_aggregation(self):
        """Test: Agregar comandos PWM duplicados con método avg."""
        aggregator = CommandAggregator(aggregation_method="avg")

        output_values = [
            OutputValue(
                reference_code="ventilador",
                dutyCycle=60.0,
                duration=10.0
            ),
            OutputValue(
                reference_code="ventilador",
                dutyCycle=80.0,
                duration=15.0
            )
        ]

        commands = aggregator.aggregate_commands(output_values)

        assert len(commands) == 1
        assert commands[0]["actuatorCode"] == "ventilador"
        # Promedio: (60 + 80) / 2 = 70
        assert commands[0]["dutyCycle"] == 70.0
        # Duration siempre usa max: max(10, 15) = 15
        assert commands[0]["duration"] == 15.0

    def test_duplicate_pwm_min_aggregation(self):
        """Test: Agregar comandos PWM duplicados con método min."""
        aggregator = CommandAggregator(aggregation_method="min")

        output_values = [
            OutputValue(
                reference_code="bomba-aireacion",
                dutyCycle=40.0,
                duration=12.0
            ),
            OutputValue(
                reference_code="bomba-aireacion",
                dutyCycle=70.0,
                duration=8.0
            )
        ]

        commands = aggregator.aggregate_commands(output_values)

        assert len(commands) == 1
        assert commands[0]["actuatorCode"] == "bomba-aireacion"
        # Mínimo: min(40, 70) = 40
        assert commands[0]["dutyCycle"] == 40.0
        # Duration siempre usa max: max(12, 8) = 12
        assert commands[0]["duration"] == 12.0

    def test_duplicate_digital_or_aggregation(self):
        """Test: Agregar comandos digitales duplicados con lógica OR."""
        aggregator = CommandAggregator(aggregation_method="max")

        output_values = [
            OutputValue(
                reference_code="luz-crecimiento",
                power="ON",
                duration=7200.0  # 2 horas
            ),
            OutputValue(
                reference_code="luz-crecimiento",
                power="OFF",
                duration=3600.0  # 1 hora
            )
        ]

        commands = aggregator.aggregate_commands(output_values)

        assert len(commands) == 1
        assert commands[0]["actuatorCode"] == "luz-crecimiento"
        # Lógica OR: ON si alguno es ON
        assert commands[0]["power"] == "ON"
        # Duration máximo
        assert commands[0]["duration"] == 7200.0

    def test_duplicate_digital_all_off(self):
        """Test: Agregar comandos digitales todos OFF resulta en OFF."""
        aggregator = CommandAggregator(aggregation_method="max")

        output_values = [
            OutputValue(
                reference_code="humidificador",
                power="OFF",
                duration=5.0
            ),
            OutputValue(
                reference_code="humidificador",
                power="OFF",
                duration=10.0
            )
        ]

        commands = aggregator.aggregate_commands(output_values)

        assert len(commands) == 1
        assert commands[0]["actuatorCode"] == "humidificador"
        # Todos OFF → resultado OFF
        assert commands[0]["power"] == "OFF"
        assert commands[0]["duration"] == 10.0

    def test_multiple_actuators_no_duplicates(self):
        """Test: Múltiples actuadores diferentes, sin duplicados."""
        aggregator = CommandAggregator(aggregation_method="max")

        output_values = [
            OutputValue(
                reference_code="bomba-riego",
                dutyCycle=80.0,
                duration=10.0
            ),
            OutputValue(
                reference_code="ventilador",
                dutyCycle=60.0,
                duration=15.0
            ),
            OutputValue(
                reference_code="calefactor-aire",
                power="ON",
                duration=20.0
            )
        ]

        commands = aggregator.aggregate_commands(output_values)

        # Debe haber 3 comandos (uno por actuador)
        assert len(commands) == 3

        # Verificar que cada actuador está presente
        actuator_codes = {cmd["actuatorCode"] for cmd in commands}
        assert actuator_codes == {"bomba-riego", "ventilador", "calefactor-aire"}

    def test_mixed_duplicates_and_unique(self):
        """Test: Mix de actuadores duplicados y únicos."""
        aggregator = CommandAggregator(aggregation_method="max")

        output_values = [
            # Calefactor: duplicado
            OutputValue(
                reference_code="calefactor-agua",
                dutyCycle=80.0,
                duration=5.0
            ),
            OutputValue(
                reference_code="calefactor-agua",
                dutyCycle=95.0,
                duration=8.0
            ),
            # Ventilador: único
            OutputValue(
                reference_code="ventilador",
                dutyCycle=70.0,
                duration=12.0
            ),
            # Bomba: duplicado
            OutputValue(
                reference_code="bomba-riego",
                power="ON",
                duration=10.0
            ),
            OutputValue(
                reference_code="bomba-riego",
                power="OFF",
                duration=5.0
            )
        ]

        commands = aggregator.aggregate_commands(output_values)

        # Debe haber 3 comandos únicos
        assert len(commands) == 3

        # Verificar calefactor (max de dutyCycle)
        calefactor = next(c for c in commands if c["actuatorCode"] == "calefactor-agua")
        assert calefactor["dutyCycle"] == 95.0
        assert calefactor["duration"] == 8.0

        # Verificar ventilador (sin cambios)
        ventilador = next(c for c in commands if c["actuatorCode"] == "ventilador")
        assert ventilador["dutyCycle"] == 70.0
        assert ventilador["duration"] == 12.0

        # Verificar bomba (lógica OR → ON)
        bomba = next(c for c in commands if c["actuatorCode"] == "bomba-riego")
        assert bomba["power"] == "ON"
        assert bomba["duration"] == 10.0

    def test_empty_output_values(self):
        """Test: Lista vacía de output_values retorna lista vacía."""
        aggregator = CommandAggregator(aggregation_method="max")
        commands = aggregator.aggregate_commands([])
        assert commands == []

    def test_invalid_aggregation_method_fallback(self):
        """Test: Método de agregación inválido usa 'max' como fallback."""
        aggregator = CommandAggregator(aggregation_method="invalid_method")

        # El constructor debe usar 'max' como fallback
        assert aggregator.aggregation_method == "max"

        output_values = [
            OutputValue(
                reference_code="test-actuator",
                dutyCycle=50.0,
                duration=5.0
            ),
            OutputValue(
                reference_code="test-actuator",
                dutyCycle=80.0,
                duration=10.0
            )
        ]

        commands = aggregator.aggregate_commands(output_values)
        assert len(commands) == 1
        # Debe usar max como fallback
        assert commands[0]["dutyCycle"] == 80.0

    def test_three_way_duplicate_max(self):
        """Test: Tres comandos para el mismo actuador con método max."""
        aggregator = CommandAggregator(aggregation_method="max")

        output_values = [
            OutputValue(
                reference_code="calefactor-agua",
                dutyCycle=70.0,
                duration=5.0
            ),
            OutputValue(
                reference_code="calefactor-agua",
                dutyCycle=96.33,  # Valor del problema reportado
                duration=8.0
            ),
            OutputValue(
                reference_code="calefactor-agua",
                dutyCycle=85.0,
                duration=6.0
            )
        ]

        commands = aggregator.aggregate_commands(output_values)

        # Debe consolidar en 1 solo comando
        assert len(commands) == 1
        assert commands[0]["actuatorCode"] == "calefactor-agua"
        # Debe usar el máximo: 96.33
        assert commands[0]["dutyCycle"] == 96.33
        # Duration máximo: 8.0
        assert commands[0]["duration"] == 8.0


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
