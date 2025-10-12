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
from FuzzyService.Application.Features.ActuatorIntegration.Commands.SendRoutinesToActuatorCommand import (
    SendRoutinesToActuatorCommand, RoutinePayload, StepPayload
)
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

            # Convertir a formato de dominio (RuleActivation)
            activated_rules = []
            for rule_data in activated_rules_data:
                rule_id = rule_data.get("rule_id")
                firing_strength = rule_data.get("firing_strength", 0.0)
                output_values_data = rule_data.get("output_values", [])

                # Convertir output_values a OutputValue
                output_values = []
                for ov in output_values_data:
                    try:
                        output_values.append(OutputValue(
                            actuator_id=ov.get("actuator_id"),
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

            # Crear input_values para la evaluación
            input_values = [
                InputValue(sensor_id=ref_code, value=value)
                for ref_code, value in sensor_readings_by_ref_code.items()
            ]

            # Unificar output_values (emparejar Control+Duración) ANTES de guardar
            unified_activated_rules = self._unify_output_values_in_rules(
                activated_rules, output_variables, routines_payload
            )

            fuzzy_evaluation = FuzzyEvaluation(
                system_id=fuzzy_system.id,
                timestamp=datetime.now(timezone.utc),
                inputs=input_values,
                activated_rules=unified_activated_rules
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
            # No lanzamos excepción para no interrumpir el flujo

    def _unify_output_values_in_rules(
        self,
        activated_rules: List[RuleActivation],
        output_variables: List[FuzzyVariable],
        routines_payload: List[Dict[str, Any]]
    ) -> List[RuleActivation]:
        """Unifica output_values emparejando Control+Duración en cada regla.

        Args:
            activated_rules: Reglas activadas con output_values separados
            output_variables: Lista de variables de salida
            routines_payload: Variables defuzzificadas con crisp_value

        Returns:
            Lista de RuleActivation con output_values unificados
        """
        output_vars_dict = {str(v.id): v for v in output_variables}
        unified_rules = []

        for rule_activation in activated_rules:
            # Emparejar Control + Duración para esta regla
            pairs = self._pair_control_duration_variables(
                rule_activation.output_values,
                output_vars_dict,
                routines_payload
            )

            # Crear OutputValues unificados
            unified_outputs = []
            for pair in pairs:
                control_val = pair["control_val"]
                duration_seconds = pair["duration_seconds"]

                # Determinar duración final
                if duration_seconds is not None and duration_seconds > 0:
                    final_duration = float(duration_seconds)
                else:
                    # Fallback a duración del control
                    final_duration = float(control_val.duration) if control_val.duration > 0 else 0.5

                # Crear OutputValue unificado
                unified_output = OutputValue(
                    actuator_id=control_val.actuator_id,
                    power=control_val.power,
                    dutyCycle=control_val.dutyCycle,
                    duration=final_duration
                )
                unified_outputs.append(unified_output)

            # Crear RuleActivation con outputs unificados
            unified_rule = RuleActivation(
                rule_id=rule_activation.rule_id,
                firing_strength=rule_activation.firing_strength,
                output_values=unified_outputs
            )
            unified_rules.append(unified_rule)

        _logger.info(
            f"Output values unificados: {len(activated_rules)} reglas, "
            f"total outputs antes={sum(len(r.output_values) for r in activated_rules)}, "
            f"después={sum(len(r.output_values) for r in unified_rules)}"
        )

        return unified_rules

    def _pair_control_duration_variables(
        self,
        output_values: List[OutputValue],
        output_variables_dict: Dict[str, FuzzyVariable],
        routines_payload: List[Dict[str, Any]]
    ) -> List[Dict[str, Any]]:
        """Empareja variables de Control con Duración usando actuator_code.

        Args:
            output_values: Valores defuzzificados de control
            output_variables_dict: Dict de variables por ID
            routines_payload: Variables defuzzificadas con crisp_value (incluye duraciones)

        Returns:
            Lista de pares {control_id, control_val, duration_val}
        """
        values_by_id = {str(ov.actuator_id): ov for ov in output_values}

        # Crear índice de duraciones por variable_id desde routines_payload
        durations_by_id = {}
        for routine in routines_payload:
            var_id = routine.get("variable_id")
            var_name = routine.get("variable_name", "")
            if "Duración" in var_name or "DURACION" in var_name.upper():
                durations_by_id[var_id] = routine.get("crisp_value", 0.5)

        pairs = []

        # Debug: mostrar output_values recibidos
        _logger.info(f"🔍 Output values recibidos del motor fuzzy: {len(output_values)}")
        for ov in output_values:
            var = output_variables_dict.get(str(ov.actuator_id))
            var_name = var.name if var else "UNKNOWN"
            _logger.info(f"  - {var_name} (ID: {ov.actuator_id}): power={ov.power}, duty={ov.dutyCycle}, duration={ov.duration}")

        # Agrupar variables por actuator_code
        variables_by_actuator = {}
        for var_id, var in output_variables_dict.items():
            if var.actuator_code:  # Usar actuator_code para agrupar
                if var.actuator_code not in variables_by_actuator:
                    variables_by_actuator[var.actuator_code] = {}

                # Identificar tipo de variable
                is_control = var.actuator_type in ["PWM", "DIGITAL"] and ("Control" in var.name or "Potencia" in var.name) and "Duración" not in var.name
                is_duration = "Duración" in var.name or "Duration" in var.name

                if is_control:
                    variables_by_actuator[var.actuator_code]["control"] = (var_id, var)
                elif is_duration:
                    variables_by_actuator[var.actuator_code]["duration"] = (var_id, var)

        _logger.info(f"📊 Variables agrupadas por actuator_code: {len(variables_by_actuator)} actuadores")

        # Emparejar por actuator_code
        for actuator_code, group_vars in variables_by_actuator.items():
            control_info = group_vars.get("control")
            duration_info = group_vars.get("duration")

            if control_info:
                control_id, control_var = control_info
                control_val = values_by_id.get(control_id)

                duration_val = None
                duration_seconds = None
                if duration_info:
                    duration_id, duration_var = duration_info
                    duration_crisp_value = durations_by_id.get(duration_id)

                    if duration_crisp_value is not None:
                        # Guardar el valor de duración en segundos (no como OutputValue)
                        duration_seconds = float(duration_crisp_value)
                        _logger.info(f"  ✅ {actuator_code}: Control '{control_var.name}' + Duración '{duration_var.name}' ({duration_seconds:.1f}s)")
                    else:
                        _logger.warning(f"  ⚠️  {actuator_code}: Control encontrado pero Duración sin valor defuzzificado")
                else:
                    _logger.warning(f"  ⚠️  {actuator_code}: Control encontrado pero NO hay variable Duración definida")

                if control_val:
                    pairs.append({
                        "control_id": control_id,
                        "control_val": control_val,
                        "duration_seconds": duration_seconds  # Usar valor directo, no OutputValue
                    })

        return pairs

    async def _send_to_actuator_service(
        self,
        fuzzy_evaluation: FuzzyEvaluation,
        output_variables: List[FuzzyVariable]
    ) -> None:
        """Envía payload al actuator service con emparejamiento Control+Duración.

        Args:
            fuzzy_evaluation: Evaluación fuzzy
            output_variables: Lista de variables de salida
        """
        try:
            # Crear diccionario de variables por ID para consultas rápidas
            output_vars_dict = {str(v.id): v for v in output_variables}

            routines = []

            for rule_activation in fuzzy_evaluation.activated_rules:
                if not rule_activation.output_values:
                    continue

                # Emparejar Control + Duración
                pairs = self._pair_control_duration_variables(
                    rule_activation.output_values,
                    output_vars_dict
                )

                steps = []

                for pair in pairs:
                    control_val = pair["control_val"]
                    duration_val = pair["duration_val"]

                    # Obtener la variable de control para extraer su reference_id
                    control_var = output_vars_dict.get(pair["control_id"])
                    if not control_var:
                        _logger.warning(f"Variable de control no encontrada: {pair['control_id']}")
                        continue

                    # Determinar si es ON o OFF
                    is_on = False
                    if control_val.power is not None:
                        is_on = (control_val.power == "ON")
                    elif control_val.dutyCycle is not None:
                        is_on = (control_val.dutyCycle > 0.0)

                    # Construir step con reference_code (código del control_output en actuator-service)
                    step_dict = {
                        "outputVariable": str(control_var.reference_code)
                    }

                    # Aplicar reglas según estado
                    if is_on:
                        # ON: usar duración calculada de la variable de Duración
                        if duration_val and duration_val.duration > 0:
                            step_dict["duration"] = float(duration_val.duration)
                        else:
                            # Fallback: usar duración del control si existe
                            step_dict["duration"] = float(control_val.duration) if control_val.duration > 0 else 0.5
                    else:
                        # OFF: duración instantánea (0.0)
                        step_dict["duration"] = 0.0

                    # Agregar power o dutyCycle
                    if control_val.power is not None:
                        step_dict["power"] = control_val.power
                    elif control_val.dutyCycle is not None:
                        step_dict["dutyCycle"] = float(control_val.dutyCycle)

                    steps.append(StepPayload(**step_dict))

                if steps:
                    routines.append(RoutinePayload(
                        routineId=str(rule_activation.rule_id),
                        steps=steps
                    ))

            if routines:
                # Log payload para debugging (temporal)
                routines_dict = [
                    {
                        "routineId": r.routineId,
                        "steps": [
                            {
                                "outputVariable": s.outputVariable,
                                **({"power": s.power} if s.power is not None else {}),
                                **({"dutyCycle": s.dutyCycle} if s.dutyCycle is not None else {}),
                                "duration": s.duration
                            }
                            for s in r.steps
                        ]
                    }
                    for r in routines
                ]
                _logger.info(
                    f"📤 Payload enviado al actuator-service:\n{json.dumps(routines_dict, indent=2)}"
                )

                mediator: Medyator = di[Medyator]
                await mediator.send(SendRoutinesToActuatorCommand(routines=routines))
            else:
                _logger.warning("No se generaron rutinas tras filtrado OFF")

        except Exception as e:
            _logger.error(f"Error al enviar payload: {e}")
