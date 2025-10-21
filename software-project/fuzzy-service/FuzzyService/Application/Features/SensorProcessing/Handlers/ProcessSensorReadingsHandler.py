"""Handler para procesar lecturas de sensores y ejecutar evaluación fuzzy."""

from __future__ import annotations

import logging
import json
from typing import List, Dict, Any, Optional
from datetime import datetime,timezone

from kink import di
from medyator import CommandHandler

from ..Commands.ProcessSensorReadingsCommand import ProcessSensorReadingsCommand, SensorReading
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository
from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository
from FuzzyService.Domain.Interfaces.IFuzzyEngine import IFuzzyEngine
from FuzzyService.Domain.Interfaces.IFuzzyEvaluationRepository import IFuzzyEvaluationRepository
from FuzzyService.Domain.Entities.fuzzy_system import FuzzySystem
from FuzzyService.Domain.Entities.fuzzy_variable import FuzzyVariable
from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule
from FuzzyService.Domain.Entities.fuzzy_term import FuzzyTerm
from FuzzyService.Domain.Entities.fuzzy_evaluation import FuzzyEvaluation, InputValue, OutputValue, RuleActivation
from FuzzyService.Application.Services.CommandAggregator import CommandAggregator
from medyator import Medyator
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId, FuzzyVariableId, FuzzyRuleId
from FuzzyService.Domain.Enums.EntityStatus import FuzzySystemStatus
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError, BusinessRuleViolationError

_logger = logging.getLogger(__name__)


class ProcessSensorReadingsHandler(CommandHandler[ProcessSensorReadingsCommand]):
    """Handler para procesar lecturas de sensores y ejecutar evaluación fuzzy.

    Responsabilidades:
    1. Buscar el sistema fuzzy activo
    2. Cargar variables y reglas del sistema
    3. Identificar variables activas basándose en reference_id de las lecturas
    4. Preparar datos para evaluación fuzzy (paso futuro)
    """

    async def __call__(self, request: ProcessSensorReadingsCommand) -> None:
        """Procesa las lecturas de sensores.

        Args:
            request: Comando con las lecturas de sensores

        Raises:
            EntityNotFoundError: Si no se encuentra el sistema fuzzy activo
            BusinessRuleViolationError: Si hay errores de validación
        """
        _logger.info(f"Procesando {len(request.readings)} lecturas de sensores")

        # 1. Obtener el sistema fuzzy activo
        fuzzy_system = await self._get_active_fuzzy_system()

        if not fuzzy_system:
            raise EntityNotFoundError("No se encontró un sistema fuzzy activo")

        # 2. Cargar variables de entrada del sistema
        input_variables = await self._load_input_variables(fuzzy_system)

        # 3. Cargar variables de salida del sistema
        output_variables = await self._load_output_variables(fuzzy_system)

        # 4. Cargar reglas del sistema
        rules = await self._load_rules(fuzzy_system)

        # 5. Mapear lecturas a variables de entrada usando reference_id
        input_data = await self._map_readings_to_variables(request.readings, input_variables)

        if not input_data:
            _logger.warning("No se encontraron variables de entrada que coincidan con las lecturas")
            return

        # 6. Ejecutar evaluación fuzzy
        fuzzy_evaluation = await self._execute_fuzzy_evaluation(
            fuzzy_system,
            input_data,
            output_variables,
            rules
        )

        # 7. Guardar evaluación en repositorio
        await self._save_evaluation(fuzzy_evaluation)

        # 8. Enviar al actuator service
        await self._send_to_actuator_service(fuzzy_evaluation, output_variables)

        # 9. Establecer resultado del comando
        request.result = {
            "status": "success",
            "evaluation_id": str(fuzzy_evaluation.id) if fuzzy_evaluation.id else None,
            "rules_activated": len(fuzzy_evaluation.activated_rules),
            "outputs_generated": sum(len(r.output_values) for r in fuzzy_evaluation.activated_rules)
        }

        _logger.info(f"Procesamiento completado exitosamente. Evaluación ID: {fuzzy_evaluation.id}")

    async def _get_active_fuzzy_system(self) -> Optional[FuzzySystem]:
        """Obtiene el sistema fuzzy activo.

        Returns:
            Sistema fuzzy activo o None si no existe
        """
        try:
            repo: IFuzzySystemRepository = di[IFuzzySystemRepository]
            systems = await repo.get_all()  # type: ignore[attr-defined]

            # Buscar el primer sistema activo
            for system in systems:
                if system.status == FuzzySystemStatus.ACTIVE:
                    return system

            _logger.warning("⚠️ No se encontró ningún sistema con status=ACTIVE")
            return None

        except Exception as e:
            _logger.error(f"Error al obtener sistema fuzzy activo: {e}", exc_info=True)
            raise

    async def _load_input_variables(self, fuzzy_system: FuzzySystem) -> List[FuzzyVariable]:
        """Carga las variables de entrada del sistema fuzzy.

        Args:
            fuzzy_system: Sistema fuzzy del cual cargar variables

        Returns:
            Lista de variables de entrada
        """
        try:
            repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]
            variables = []

            for var_id in fuzzy_system.input_variable_ids:
                variable = await repo.get_by_id(var_id)  # type: ignore[attr-defined]
                if variable:
                    variables.append(variable)

            return variables

        except Exception as e:
            _logger.error(f"Error al cargar variables de entrada: {e}")
            raise

    async def _load_output_variables(self, fuzzy_system: FuzzySystem) -> List[FuzzyVariable]:
        """Carga las variables de salida del sistema fuzzy.

        Args:
            fuzzy_system: Sistema fuzzy del cual cargar variables

        Returns:
            Lista de variables de salida
        """
        try:
            repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]
            variables = []

            for var_id in fuzzy_system.output_variable_ids:
                variable = await repo.get_by_id(var_id)  # type: ignore[attr-defined]
                if variable:
                    variables.append(variable)

            return variables

        except Exception as e:
            _logger.error(f"Error al cargar variables de salida: {e}")
            raise

    async def _load_rules(self, fuzzy_system: FuzzySystem) -> List[FuzzyRule]:
        """Carga las reglas del sistema fuzzy.

        Args:
            fuzzy_system: Sistema fuzzy del cual cargar reglas

        Returns:
            Lista de reglas
        """
        try:
            repo: IFuzzyRuleRepository = di[IFuzzyRuleRepository]
            rules = await repo.get_by_system_id(fuzzy_system.id)  # type: ignore[attr-defined]
            return rules if rules else []

        except Exception as e:
            _logger.error(f"Error al cargar reglas: {e}")
            raise

    async def _map_readings_to_variables(
        self,
        readings: List[SensorReading],
        variables: List[FuzzyVariable]
    ) -> Dict[str, float]:
        """Mapea lecturas de sensores a variables de entrada usando reference_code.

        Args:
            readings: Lecturas de sensores
            variables: Variables de entrada del sistema

        Returns:
            Diccionario {variable_id: valor}
        """
        # Crear un índice de variables por reference_code (código estable del sensor)
        variables_by_ref_code = {
            str(var.reference_code): var
            for var in variables
            if var.reference_code
        }

        input_data = {}

        for reading in readings:
            sensor_code = str(reading.sensor_id)  # sensor_id contiene el variableCode (ej: "T_AMB")

            if sensor_code in variables_by_ref_code:
                variable = variables_by_ref_code[sensor_code]
                input_data[str(variable.id)] = reading.value
                _logger.debug(
                    f"Mapeado: {variable.name} (reference_code={sensor_code}) = {reading.value}"
                )
            else:
                _logger.debug(
                    f"Lectura ignorada: sensor_code={sensor_code} no coincide con ninguna variable (disponibles: {list(variables_by_ref_code.keys())})"
                )

        return input_data

    async def _execute_fuzzy_evaluation(
        self,
        fuzzy_system: FuzzySystem,
        input_data: Dict[str, float],
        output_variables: List[FuzzyVariable],
        rules: List[FuzzyRule]
    ) -> FuzzyEvaluation:
        """Ejecuta la evaluación fuzzy.

        Args:
            fuzzy_system: Sistema fuzzy
            input_data: Datos de entrada {variable_id: valor}
            output_variables: Variables de salida
            rules: Reglas a evaluar

        Returns:
            Evaluación fuzzy con resultados
        """
        try:
            # Obtener el motor fuzzy desde DI
            fuzzy_engine: IFuzzyEngine = di[IFuzzyEngine]

            # Cargar términos necesarios para todas las variables
            term_repo: IFuzzyTermRepository = di[IFuzzyTermRepository]
            input_var_repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]

            # Cargar variables de entrada completas
            input_variables = []
            sensor_readings_by_ref_code = {}  # {reference_code: valor}

            for var_id, value in input_data.items():
                var = await input_var_repo.get_by_id(FuzzyVariableId(var_id))  # type: ignore[attr-defined]
                if var:
                    input_variables.append(var)
                    # Mapear a reference_code para el fuzzy engine
                    sensor_readings_by_ref_code[str(var.reference_code)] = value

            # Combinar todas las variables
            all_variables = input_variables + output_variables

            # Cargar todos los términos
            all_terms = []
            for var in all_variables:
                terms = await term_repo.get_by_variable_id(var.id)  # type: ignore[attr-defined]
                if terms:
                    all_terms.extend(terms)

            # Ejecutar evaluación completa
            result = await fuzzy_engine.complete_fuzzy_evaluation(
                system=fuzzy_system,
                variables=all_variables,
                terms=all_terms,
                rules=rules,
                sensor_readings=sensor_readings_by_ref_code  # Usar reference_codes, no fuzzy variable IDs
            )

            # Extraer resultados de la evaluación
            rule_evaluation = result.get("rule_evaluation", {})
            activated_rules_data = rule_evaluation.get("activated_rules", [])
            routines_payload = result.get("routines_payload", [])

            # Crear input_values para la evaluación
            input_values = [
                InputValue(sensor_id=ref_code, value=value)
                for ref_code, value in sensor_readings_by_ref_code.items()
            ]

            # Crear RuleActivation por cada regla activada
            activated_rules = []
            for rule_data in activated_rules_data:
                rule_id = rule_data.get("rule_id")
                firing_strength = rule_data.get("firing_strength", 0.0)
                output_values_data = rule_data.get("output_values", [])

                # Convertir output_values a OutputValue con reference_code
                output_values = []
                for ov in output_values_data:
                    try:
                        output_values.append(OutputValue(
                            reference_code=ov.get("reference_code"),
                            power=ov.get("power"),
                            dutyCycle=ov.get("dutyCycle"),
                            duration=ov.get("duration", 0.5)
                        ))
                    except Exception as e:
                        _logger.warning(f"Error creando OutputValue: {e}, datos: {ov}")
                        continue

                activated_rules.append(RuleActivation(
                    rule_id=rule_id,
                    firing_strength=firing_strength,
                    output_values=output_values
                ))

            fuzzy_evaluation = FuzzyEvaluation(
                system_id=fuzzy_system.id,
                timestamp=datetime.now(timezone.utc),
                inputs=input_values,
                activated_rules=activated_rules
            )

            return fuzzy_evaluation

        except Exception as e:
            _logger.error(f"Error al ejecutar evaluación fuzzy: {e}", exc_info=True)
            raise

    async def _save_evaluation(self, fuzzy_evaluation: FuzzyEvaluation) -> None:
        """Guarda la evaluación en el repositorio.

        Args:
            fuzzy_evaluation: Evaluación a guardar
        """
        try:
            repo: IFuzzyEvaluationRepository = di[IFuzzyEvaluationRepository]
            await repo.create(fuzzy_evaluation)  # type: ignore[attr-defined]
            _logger.info(f"Evaluación guardada con ID: {fuzzy_evaluation.id}")

        except Exception as e:
            _logger.error(f"Error al guardar evaluación: {e}")


    async def _send_to_actuator_service(
        self,
        fuzzy_evaluation: FuzzyEvaluation,
        output_variables: List[FuzzyVariable]
    ) -> None:
        """Envía comandos al actuator service en el nuevo formato.

        Args:
            fuzzy_evaluation: Evaluación fuzzy con reglas activadas
            output_variables: Lista de variables de salida
        """
        try:
            if not fuzzy_evaluation.activated_rules:
                _logger.warning("No hay reglas activadas")
                return

            # Recolectar todos los output_values de todas las reglas
            all_output_values = []
            for rule_activation in fuzzy_evaluation.activated_rules:
                all_output_values.extend(rule_activation.output_values)

            if not all_output_values:
                _logger.warning("No se generaron output_values")
                return

            _logger.debug(
                f"Recolectados {len(all_output_values)} output_values de "
                f"{len(fuzzy_evaluation.activated_rules)} reglas activadas"
            )

            # Agregar comandos usando el CommandAggregator para consolidar duplicados
            aggregator = CommandAggregator(aggregation_method="max")
            commands = aggregator.aggregate_commands(all_output_values)

            _logger.info(
                f"Comandos consolidados: {len(all_output_values)} output_values → "
                f"{len(commands)} comandos únicos"
            )

            if commands:
                # Log payload para debugging
                _logger.info(
                    f"📤 Comandos enviados al actuator-service:\n"
                    f"{json.dumps({'commands': commands}, indent=2)}"
                )

                # Enviar al actuator service con el nuevo formato
                mediator: Medyator = di[Medyator]
                from FuzzyService.Application.Features.ActuatorIntegration.Commands.SendCommandsToActuatorCommand import (
                    SendCommandsToActuatorCommand
                )
                await mediator.send(SendCommandsToActuatorCommand(commands=commands))
            else:
                _logger.warning("No se generaron comandos tras la agregación")

        except Exception as e:
            _logger.error(f"Error al enviar comandos: {e}", exc_info=True)
