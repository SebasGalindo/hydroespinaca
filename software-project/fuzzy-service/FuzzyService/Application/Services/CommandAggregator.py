"""
Servicio de agregación de comandos para resolver duplicados por actuador.

Este servicio consolida múltiples OutputValues del mismo actuador (reference_code)
aplicando funciones de agregación configurables.
"""

import logging
from typing import List, Dict, Any, Optional
from statistics import mean

from FuzzyService.Domain.Entities.fuzzy_evaluation import OutputValue

_logger = logging.getLogger(__name__)


class CommandAggregator:
    """
    Agrega comandos de múltiples reglas para el mismo actuador.

    Cuando múltiples reglas activan el mismo actuador con diferentes valores,
    este servicio los consolida en un único comando usando funciones de agregación.
    """

    def __init__(self, aggregation_method: str = "max"):
        """
        Inicializa el agregador de comandos.

        Args:
            aggregation_method: Método de agregación ('max', 'avg', 'min', 'sum')
        """
        self.aggregation_method = aggregation_method
        valid_methods = ["max", "avg", "min", "sum"]

        if aggregation_method not in valid_methods:
            _logger.warning(
                f"Método de agregación '{aggregation_method}' no válido. "
                f"Usando 'max'. Métodos válidos: {valid_methods}"
            )
            self.aggregation_method = "max"

    def aggregate_commands(
        self,
        output_values: List[OutputValue]
    ) -> List[Dict[str, Any]]:
        """
        Agrega comandos por reference_code, consolidando duplicados.

        Args:
            output_values: Lista de OutputValues de todas las reglas activadas

        Returns:
            Lista de comandos consolidados (un comando por actuador)
        """
        if not output_values:
            return []

        # Agrupar por reference_code
        grouped = self._group_by_reference_code(output_values)

        # Agregar cada grupo
        aggregated_commands = []
        for reference_code, values in grouped.items():
            if len(values) == 1:
                # Solo un comando para este actuador, no hay agregación necesaria
                command = self._output_value_to_command(values[0])
                aggregated_commands.append(command)
            else:
                # Múltiples comandos: aplicar agregación
                _logger.info(
                    f"Agregando {len(values)} comandos para actuador '{reference_code}' "
                    f"usando método '{self.aggregation_method}'"
                )
                aggregated_command = self._aggregate_group(reference_code, values)
                aggregated_commands.append(aggregated_command)

        return aggregated_commands

    def _group_by_reference_code(
        self,
        output_values: List[OutputValue]
    ) -> Dict[str, List[OutputValue]]:
        """
        Agrupa OutputValues por reference_code.

        Args:
            output_values: Lista de OutputValues

        Returns:
            Dict {reference_code: [OutputValue, ...]}
        """
        grouped: Dict[str, List[OutputValue]] = {}

        for output_val in output_values:
            ref_code = output_val.reference_code
            if ref_code not in grouped:
                grouped[ref_code] = []
            grouped[ref_code].append(output_val)

        return grouped

    def _aggregate_group(
        self,
        reference_code: str,
        values: List[OutputValue]
    ) -> Dict[str, Any]:
        """
        Agrega múltiples OutputValues del mismo actuador en un comando consolidado.

        Args:
            reference_code: Código del actuador
            values: Lista de OutputValues para este actuador

        Returns:
            Comando consolidado
        """
        command = {
            "actuatorCode": reference_code
        }

        # Separar por tipo de actuador
        pwm_values = [v for v in values if v.dutyCycle is not None]
        digital_values = [v for v in values if v.power is not None]

        # Agregar dutyCycle (PWM)
        if pwm_values:
            duty_cycles = [v.dutyCycle for v in pwm_values]
            aggregated_duty = self._apply_aggregation_function(duty_cycles)
            command["dutyCycle"] = round(aggregated_duty, 2)
            _logger.debug(
                f"Actuador PWM '{reference_code}': {duty_cycles} → {aggregated_duty:.2f} "
                f"(método: {self.aggregation_method})"
            )

        # Agregar power (DIGITAL)
        if digital_values:
            # Para digital, usar lógica OR: si alguno es ON, el resultado es ON
            powers = [v.power for v in digital_values]
            aggregated_power = self._aggregate_digital_power(powers)
            command["power"] = aggregated_power
            _logger.debug(
                f"Actuador DIGITAL '{reference_code}': {powers} → {aggregated_power} (lógica OR)"
            )

        # Agregar duration (usar el máximo por seguridad)
        durations = [float(v.duration) for v in values]
        aggregated_duration = self._aggregate_duration(durations)
        command["duration"] = round(aggregated_duration, 2)
        _logger.debug(
            f"Duración para '{reference_code}': {durations} → {aggregated_duration:.2f}s "
            f"(método: {self.aggregation_method})"
        )

        return command

    def _apply_aggregation_function(self, values: List[float]) -> float:
        """
        Aplica la función de agregación a una lista de valores numéricos.

        Args:
            values: Lista de valores a agregar

        Returns:
            Valor agregado
        """
        if not values:
            return 0.0

        if self.aggregation_method == "max":
            return max(values)
        elif self.aggregation_method == "min":
            return min(values)
        elif self.aggregation_method == "avg":
            return mean(values)
        elif self.aggregation_method == "sum":
            # Suma limitada a 100 para duty cycles
            return min(sum(values), 100.0)
        else:
            # Fallback a max
            return max(values)

    def _aggregate_digital_power(self, powers: List[str]) -> str:
        """
        Agrega valores de power digital usando lógica OR.

        Si alguna regla dice ON, el resultado es ON.
        Solo si todas dicen OFF, el resultado es OFF.

        Args:
            powers: Lista de valores de power ("ON" o "OFF")

        Returns:
            Valor agregado de power
        """
        if not powers:
            return "OFF"

        # Lógica OR: si alguno es ON, resultado es ON
        return "ON" if any(p == "ON" for p in powers) else "OFF"

    def _aggregate_duration(self, durations: List[float]) -> float:
        """
        Agrega valores de duración.

        Por defecto, usa el método de agregación configurado.
        Para seguridad, se recomienda usar 'max' para duraciones.

        Args:
            durations: Lista de duraciones en segundos

        Returns:
            Duración agregada
        """
        if not durations:
            return 0.5  # Duración mínima por defecto

        # Para duraciones, usar siempre max para evitar ciclos muy cortos
        return max(durations)

    def _output_value_to_command(self, output_val: OutputValue) -> Dict[str, Any]:
        """
        Convierte un OutputValue a formato de comando.

        Args:
            output_val: OutputValue a convertir

        Returns:
            Comando en formato dict
        """
        command = {
            "actuatorCode": output_val.reference_code,
            "duration": float(output_val.duration)
        }

        if output_val.power is not None:
            command["power"] = output_val.power
        elif output_val.dutyCycle is not None:
            command["dutyCycle"] = float(output_val.dutyCycle)

        return command
