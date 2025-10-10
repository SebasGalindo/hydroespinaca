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

        _logger.info(f"Sistema fuzzy encontrado: {fuzzy_system.name} (ID: {fuzzy_system.id})")

        # 2. Cargar variables de entrada del sistema
        input_variables = await self._load_input_variables(fuzzy_system)
        _logger.info(f"Variables de entrada cargadas: {len(input_variables)}")

        # 3. Cargar variables de salida del sistema
        output_variables = await self._load_output_variables(fuzzy_system)
        _logger.info(f"Variables de salida cargadas: {len(output_variables)}")

        # 4. Cargar reglas del sistema
        rules = await self._load_rules(fuzzy_system)
        _logger.info(f"Reglas cargadas: {len(rules)}")

        # 5. Mapear lecturas a variables de entrada usando reference_id
        input_data = await self._map_readings_to_variables(request.readings, input_variables)

        if not input_data:
            _logger.warning("No se encontraron variables de entrada que coincidan con las lecturas")
            return

        _logger.info(f"Variables de entrada mapeadas: {len(input_data)}")

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

            _logger.info(f"📊 Total sistemas encontrados: {len(systems)}")

            # Buscar el primer sistema activo
            for system in systems:
                _logger.info(
                    f"🔍 Sistema: {system.name}, "
                    f"status={system.status}, "
                    f"type(status)={type(system.status)}, "
                    f"status.value={system.status.value if hasattr(system.status, 'value') else 'N/A'}, "
                    f"comparación con ACTIVE: {system.status == FuzzySystemStatus.ACTIVE}"
                )

                if system.status == FuzzySystemStatus.ACTIVE:
                    _logger.info(f"✅ Sistema activo encontrado: {system.name} (ID: {system.id})")
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
        """Mapea lecturas de sensores a variables de entrada usando reference_id.

        Args:
            readings: Lecturas de sensores
            variables: Variables de entrada del sistema

        Returns:
            Diccionario {variable_id: valor}
        """
        # Crear un índice de variables por reference_id
        variables_by_ref_id = {
            str(var.reference_id): var
            for var in variables
            if var.reference_id
        }

        input_data = {}

        for reading in readings:
            sensor_id = str(reading.sensor_id)

            if sensor_id in variables_by_ref_id:
                variable = variables_by_ref_id[sensor_id]
                input_data[str(variable.id)] = reading.value
                _logger.debug(
                    f"Mapeado: {variable.name} (ref={sensor_id}) = {reading.value}"
                )
            else:
                _logger.debug(
                    f"Lectura ignorada: sensor_id={sensor_id} no coincide con ninguna variable"
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
            sensor_readings_by_ref_id = {}  # {reference_id: valor}

            for var_id, value in input_data.items():
                var = await input_var_repo.get_by_id(FuzzyVariableId(var_id))  # type: ignore[attr-defined]
                if var:
                    input_variables.append(var)
                    # Mapear a reference_id para el fuzzy engine
                    sensor_readings_by_ref_id[str(var.reference_id)] = value
                    _logger.debug(f"Mapeado para evaluación: {var.name} (fuzzy_id={var_id}, ref_id={var.reference_id}) = {value}")

            # Combinar todas las variables
            all_variables = input_variables + output_variables

            # Cargar todos los términos
            all_terms = []
            for var in all_variables:
                terms = await term_repo.get_by_variable_id(var.id)  # type: ignore[attr-defined]
                if terms:
                    all_terms.extend(terms)

            # Ejecutar evaluación completa
            _logger.info("Ejecutando evaluación fuzzy completa...")
            result = await fuzzy_engine.complete_fuzzy_evaluation(
                system=fuzzy_system,
                variables=all_variables,
                terms=all_terms,
                rules=rules,
                sensor_readings=sensor_readings_by_ref_id  # Usar reference_ids, no fuzzy variable IDs
            )

            # Extraer resultados de la evaluación
            rule_evaluation = result.get("rule_evaluation", {})
            activated_rules_data = rule_evaluation.get("activated_rules", [])

            _logger.debug(f"📊 rule_evaluation keys: {rule_evaluation.keys()}")
            _logger.debug(f"📊 activated_rules_data count: {len(activated_rules_data)}")

            # Log detallado de cada regla activada
            for i, rd in enumerate(activated_rules_data):
                _logger.debug(
                    f"  Regla {i+1}: rule_id={rd.get('rule_id')}, "
                    f"firing_strength={rd.get('firing_strength')}, "
                    f"outputs={len(rd.get('output_values', []))}"
                )
                for j, ov in enumerate(rd.get('output_values', [])):
                    _logger.debug(f"    Output {j+1}: {ov}")

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
                InputValue(sensor_id=ref_id, value=value)
                for ref_id, value in sensor_readings_by_ref_id.items()
            ]

            # Unificar output_values (emparejar Control+Duración) ANTES de guardar
            unified_activated_rules = self._unify_output_values_in_rules(
                activated_rules, output_variables
            )

            fuzzy_evaluation = FuzzyEvaluation(
                system_id=fuzzy_system.id,
                timestamp=datetime.now(timezone.utc),
                inputs=input_values,
                activated_rules=unified_activated_rules
            )

            _logger.info(
                f"Evaluación completada. Reglas activadas: {len(unified_activated_rules)}, "
                f"Total outputs unificados: {sum(len(r.output_values) for r in unified_activated_rules)}"
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
        output_variables: List[FuzzyVariable]
    ) -> List[RuleActivation]:
        """Unifica output_values emparejando Control+Duración en cada regla.

        Args:
            activated_rules: Reglas activadas con output_values separados
            output_variables: Lista de variables de salida

        Returns:
            Lista de RuleActivation con output_values unificados
        """
        output_vars_dict = {str(v.id): v for v in output_variables}
        unified_rules = []

        for rule_activation in activated_rules:
            # Emparejar Control + Duración para esta regla
            pairs = self._pair_control_duration_variables(
                rule_activation.output_values,
                output_vars_dict
            )

            # Crear OutputValues unificados
            unified_outputs = []
            for pair in pairs:
                control_val = pair["control_val"]
                duration_val = pair["duration_val"]

                # Determinar duración final
                if duration_val and duration_val.duration > 0:
                    final_duration = float(duration_val.duration)
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
        output_variables_dict: Dict[str, FuzzyVariable]
    ) -> List[Dict[str, Any]]:
        """Empareja variables de Control con Duración.

        Args:
            output_values: Valores defuzzificados
            output_variables_dict: Dict de variables por ID

        Returns:
            Lista de pares {control_id, control_val, duration_val}
        """
        values_by_id = {str(ov.actuator_id): ov for ov in output_values}
        pairs = []
        processed_duration_ids = set()

        _logger.debug(f"🔍 Emparejando variables. values_by_id keys: {list(values_by_id.keys())}")
        _logger.debug(f"🔍 Variables disponibles: {[(vid, var.name) for vid, var in output_variables_dict.items()]}")

        for var_id, output_val in values_by_id.items():
            var = output_variables_dict.get(var_id)
            if not var:
                _logger.debug(f"⚠️ Variable no encontrada para ID: {var_id}")
                continue

            var_name = str(var.name)  # Asegurar que sea string

            _logger.debug(f"📝 Procesando variable: {var_name} (ID: {var_id})")

            # Detectar variable de Control o Potencia
            is_control = var_name.startswith("Control ")
            is_potencia = "Potencia" in var_name

            if is_control or is_potencia:
                # Extraer nombre del actuador
                if is_control:
                    actuator_name = var_name.replace("Control ", "")
                else:
                    actuator_name = var_name.replace("Potencia del ", "").replace("Potencia de ", "").replace("Potencia ", "")

                # Mapeo de nombres de actuadores a sus nombres de duración
                # Esto maneja las inconsistencias de nomenclatura
                duration_mappings = {
                    "Ventilador": "Duración de Ventilación",
                    "Calefactor Aire": "Duración de Calefacción de Aire",
                    "Calefactor Agua": "Duración de Calefacción de Agua",
                    "Bomba Riego": "Duración de Riego",
                    "Bomba Aireacion": "Duración de Aireación",
                    "Luz": "Duración de Luz"
                }

                # Intentar primero con el mapeo específico
                duration_var_name = duration_mappings.get(actuator_name)

                # Si no está en el mapeo, usar el formato genérico
                if not duration_var_name:
                    duration_var_name = f"Duración de {actuator_name}"

                _logger.debug(f"🔍 Buscando duración: '{duration_var_name}' para actuador '{actuator_name}'")

                duration_var_id = None

                for dvid, dvar in output_variables_dict.items():
                    dvar_name = str(dvar.name)
                    _logger.debug(f"  Comparando con: '{dvar_name}'")
                    if dvar_name == duration_var_name:
                        duration_var_id = dvid
                        _logger.debug(f"✅ Duración encontrada: {duration_var_name} (ID: {dvid})")
                        break

                duration_val = values_by_id.get(duration_var_id) if duration_var_id else None

                if not duration_val:
                    _logger.warning(f"⚠️ No se encontró valor de duración para '{duration_var_name}' (actuador: '{actuator_name}')")

                pairs.append({
                    "control_id": var_id,
                    "control_val": output_val,
                    "duration_val": duration_val
                })

                if duration_var_id:
                    processed_duration_ids.add(duration_var_id)
            else:
                _logger.debug(f"⏭️ Variable '{var_name}' no es Control ni Potencia, saltando")

        _logger.debug(f"✅ Emparejamiento completado. Pares generados: {len(pairs)}")
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

                    # Construir step con reference_id (apunta al control_output en actuator-service)
                    step_dict = {
                        "outputVariable": str(control_var.reference_id)
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

                    _logger.debug(
                        f"Step creado: {control_var.name} "
                        f"(fuzzy_id={pair['control_id']}, ref_id={control_var.reference_id}), "
                        f"power={step_dict.get('power')}, duty={step_dict.get('dutyCycle')}, "
                        f"duration={step_dict['duration']}"
                    )

                    steps.append(StepPayload(**step_dict))

                if steps:
                    routines.append(RoutinePayload(
                        routineId=str(rule_activation.rule_id),
                        steps=steps
                    ))

            if routines:
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
                    f"Payload a enviar (emparejado y filtrado):\n{json.dumps(routines_dict, indent=2)}"
                )

                mediator: Medyator = di[Medyator]
                await mediator.send(SendRoutinesToActuatorCommand(routines=routines))

                _logger.info(f"Payload enviado. Rutinas: {len(routines)}")
            else:
                _logger.warning("No se generaron rutinas tras filtrado OFF")

        except Exception as e:
            _logger.error(f"Error al enviar payload: {e}")
