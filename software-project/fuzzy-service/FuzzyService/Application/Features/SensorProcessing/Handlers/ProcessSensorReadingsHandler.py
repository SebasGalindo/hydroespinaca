"""Handler para procesar lecturas de sensores y ejecutar evaluación fuzzy."""

from __future__ import annotations

import logging
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
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId, FuzzyVariableId
from FuzzyService.Domain.Enums.EntityStatus import FuzzySystemStatus
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError, BusinessRuleViolationError

_logger = logging.getLogger(__name__)


class ProcessSensorReadingsHandler(CommandHandler[ProcessSensorReadingsCommand]):
    """Handler para procesar lecturas de sensores y ejecutar evaluación fuzzy.
    
    Responsabilidades:
    1. Buscar el sistema fuzzy activo
    2. Cargar variables y reglas del sistema
    3. Identificar variables activas basándose en device_id de las lecturas
    4. Preparar datos para evaluación fuzzy (paso futuro)
    """
    
    async def __call__(self, request: ProcessSensorReadingsCommand) -> None:
        """Procesa las lecturas de sensores.
        
        Args:
            request: Comando con las lecturas de sensores
            
        Returns:
            Result con información del procesamiento
        """
        try:
            _logger.info(
                f"Iniciando procesamiento de {len(request.readings)} lecturas "
                f"del ESP32 '{request.esp32_id}'"
            )
            
            # 1. Buscar sistema fuzzy activo
            active_system = await self._find_active_fuzzy_system()
            if not active_system:
                error_msg = "No se encontró un sistema fuzzy activo"
                _logger.warning(error_msg)
                raise EntityNotFoundError(error_msg)
            
            _logger.info(f"Sistema fuzzy activo encontrado: {active_system.name} (ID: {active_system.id})")
            
            # 2. Cargar variables del sistema
            system_variables = await self._load_system_variables(active_system.id)
            if not system_variables:
                error_msg = f"El sistema fuzzy '{active_system.name}' no tiene variables configuradas"
                _logger.warning(error_msg)
                raise BusinessRuleViolationError(error_msg)
            
            _logger.info(f"Cargadas {len(system_variables)} variables del sistema fuzzy")
            
            # 3. Cargar reglas del sistema
            system_rules = await self._load_system_rules(active_system.id)
            _logger.info(f"Cargadas {len(system_rules)} reglas del sistema fuzzy")
            
            # 4. Identificar variables activas
            active_variables = await self._identify_active_variables(
                system_variables, 
                request.readings
            )
            
            if not active_variables:
                error_msg = "No se encontraron variables activas que coincidan con las lecturas"
                _logger.warning(error_msg)
                raise BusinessRuleViolationError(error_msg)
            
            _logger.info(
                f"Identificadas {len(active_variables)} variables activas: "
                f"{[var.name for var in active_variables]}"
            )
            
            # 5. Realizar evaluación fuzzy completa (Pasos 3-5 del flujo)
            fuzzy_evaluation_results = await self._perform_complete_fuzzy_evaluation(
                active_system, active_variables, system_rules, request.readings
            )
            
            # 6. Crear y guardar el fuzzy evaluation en la base de datos
            fuzzy_evaluation = await self._create_and_save_fuzzy_evaluation(
                active_system, request.readings, fuzzy_evaluation_results
            )
            
            _logger.info(f"Fuzzy evaluation guardado con ID: {fuzzy_evaluation.id}")
            
            # 7. Establecer el resultado en el comando usando el patrón result
            request.result = fuzzy_evaluation.model_dump()
            
        except Exception as e:
            error_msg = f"Error al procesar lecturas de sensores: {str(e)}"
            _logger.error(error_msg, exc_info=True)
            raise
    
    async def _find_active_fuzzy_system(self) -> Optional[FuzzySystem]:
        """Busca el sistema fuzzy activo.
        
        Returns:
            Sistema fuzzy activo o None si no se encuentra
        """
        try:
            system_repo: IFuzzySystemRepository = di[IFuzzySystemRepository]
            
            # Buscar sistemas con estado ACTIVE
            active_systems = await system_repo.get_by_status(FuzzySystemStatus.ACTIVE)
            
            if not active_systems:
                _logger.warning("No se encontraron sistemas fuzzy activos")
                return None
            
            if len(active_systems) > 1:
                _logger.warning(
                    f"Se encontraron {len(active_systems)} sistemas activos, "
                    f"usando el primero: {active_systems[0].name}"
                )
            
            return active_systems[0]
            
        except Exception as e:
            _logger.error(f"Error al buscar sistema fuzzy activo: {e}")
            return None
    
    async def _load_system_variables(self, system_id: FuzzySystemId) -> List[FuzzyVariable]:
        """Carga todas las variables del sistema fuzzy.
        
        Args:
            system_id: ID del sistema fuzzy
            
        Returns:
            Lista de variables del sistema
        """
        try:
            variable_repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]
            return await variable_repo.get_by_system_id(system_id)
            
        except Exception as e:
            _logger.error(f"Error al cargar variables del sistema {system_id}: {e}")
            return []
    
    async def _load_system_rules(self, system_id: FuzzySystemId) -> List[FuzzyRule]:
        """Carga todas las reglas del sistema fuzzy.
        
        Args:
            system_id: ID del sistema fuzzy
            
        Returns:
            Lista de reglas del sistema
        """
        try:
            rule_repo: IFuzzyRuleRepository = di[IFuzzyRuleRepository]
            return await rule_repo.get_by_system_id(system_id)
            
        except Exception as e:
            _logger.error(f"Error al cargar reglas del sistema {system_id}: {e}")
            return []
    
    async def _identify_active_variables(
        self, 
        system_variables: List[FuzzyVariable], 
        readings: List[SensorReading]
    ) -> List[FuzzyVariable]:
        """Identifica las variables activas basándose en las lecturas.
        
        Una variable es activa si su device_id coincide con algún sensor_id
        de las lecturas recibidas.
        
        Args:
            system_variables: Variables del sistema fuzzy
            readings: Lecturas de sensores recibidas
            
        Returns:
            Lista de variables activas
        """
        try:
            # Crear set de sensor_ids para búsqueda eficiente
            sensor_ids = {reading.sensor_id for reading in readings}
            
            # Filtrar variables que tienen device_id en las lecturas
            active_variables = [
                var for var in system_variables 
                if var.device_id and var.device_id in sensor_ids
            ]
            
            # Log detallado de coincidencias
            for var in active_variables:
                matching_reading = next(
                    reading for reading in readings 
                    if reading.sensor_id == var.device_id
                )
                _logger.info(
                    f"Variable activa: '{var.name}' (device_id: {var.device_id}) "
                    f"-> valor: {matching_reading.value}"
                )
            
            # Log de variables sin coincidencias
            inactive_variables = [
                var for var in system_variables 
                if not var.device_id or var.device_id not in sensor_ids
            ]
            
            if inactive_variables:
                inactive_names = [var.name for var in inactive_variables]
                _logger.debug(
                    f"Variables sin lecturas correspondientes: {inactive_names}"
                )
            
            return active_variables
            
        except Exception as e:
            _logger.error(f"Error al identificar variables activas: {e}")
            return []
    
    async def _perform_complete_fuzzy_evaluation(
        self, 
        active_system: FuzzySystem,
        active_variables: List[FuzzyVariable], 
        system_rules: List[FuzzyRule],
        readings: List[SensorReading]
    ) -> Dict[str, Any]:
        """
        Realiza la evaluación fuzzy completa (Pasos 3-5 del flujo):
        - Fuzzificación de variables de entrada
        - Evaluación de reglas fuzzy
        - Defuzzificación de variables de salida
        
        Args:
            active_system: Sistema fuzzy activo
            active_variables: Variables que tienen lecturas correspondientes
            system_rules: Reglas del sistema fuzzy
            readings: Lecturas de sensores recibidas
            
        Returns:
            Diccionario con los resultados de la evaluación completa
        """
        try:
            _logger.info("Iniciando evaluación fuzzy completa")
            
            # Cargar términos fuzzy de las variables activas
            terms = await self._load_terms_for_variables(active_variables)
            if not terms:
                error_msg = "No se encontraron términos fuzzy para las variables activas"
                _logger.warning(error_msg)
                raise BusinessRuleViolationError(error_msg)
            
            _logger.info(f"Cargados {len(terms)} términos fuzzy")
            
            # Obtener motor fuzzy inyectado
            fuzzy_engine: IFuzzyEngine = di[IFuzzyEngine]
            
            # Preparar datos de entrada para la evaluación
            sensor_data = {reading.sensor_id: reading.value for reading in readings}
            
            # Realizar evaluación fuzzy completa
            evaluation_results = await fuzzy_engine.complete_fuzzy_evaluation(
                system=active_system,
                variables=active_variables,
                terms=terms,
                rules=system_rules,
                sensor_readings=sensor_data
            )
            
            _logger.info(
                f"Evaluación fuzzy completada. Resultados: {evaluation_results}"
            )
            
            return evaluation_results
            
        except Exception as e:
            error_msg = f"Error durante la evaluación fuzzy: {str(e)}"
            _logger.error(error_msg)
            raise BusinessRuleViolationError(error_msg)
    
    async def _load_terms_for_variables(self, variables: List[FuzzyVariable]) -> List[FuzzyTerm]:
        """
        Carga todos los términos fuzzy asociados a las variables proporcionadas.
        
        Args:
            variables: Lista de variables fuzzy
            
        Returns:
            Lista de términos fuzzy de todas las variables
        """
        try:
            term_repo: IFuzzyTermRepository = di[IFuzzyTermRepository]
            all_terms = []
            
            for variable in variables:
                variable_terms = await term_repo.get_by_variable_id(variable.id)
                all_terms.extend(variable_terms)
                _logger.debug(
                    f"Cargados {len(variable_terms)} términos para variable '{variable.name}'"
                )
            
            return all_terms
            
        except Exception as e:
            _logger.error(f"Error al cargar términos fuzzy: {e}")
            return []
    
    async def _create_and_save_fuzzy_evaluation(
        self, 
        system: FuzzySystem, 
        readings: List[SensorReading], 
        evaluation_results: Dict[str, Any]
    ) -> FuzzyEvaluation:
        """
        Crea y guarda un fuzzy evaluation en la base de datos.
        
        Args:
            system: Sistema fuzzy utilizado
            readings: Lecturas de sensores procesadas
            evaluation_results: Resultados de la evaluación fuzzy
            
        Returns:
            FuzzyEvaluation guardado en la base de datos
        """
        try:
            # Construir inputs del fuzzy evaluation
            inputs = [
                InputValue(
                    sensor_id=reading.sensor_id,
                    value=reading.value
                )
                for reading in readings
            ]
            
            # Construir reglas activadas con la estructura correcta
            activated_rules = []
            
            # El resultado viene en evaluation_results["rule_evaluation"]["activated_rules"]
            rule_evaluation = evaluation_results.get("rule_evaluation", {})
            if "activated_rules" in rule_evaluation:
                for rule_data in rule_evaluation["activated_rules"]:
                    # Construir output_values para cada regla activada
                    output_values = []
                    if "output_values" in rule_data:
                        for output_data in rule_data["output_values"]:
                            output_values.append(OutputValue(
                                actuator_id=output_data.get("actuator_id", ""),
                                power=output_data.get("power", 0.0),
                                duration=output_data.get("duration", 0.0)
                            ))
                    
                    activated_rules.append(RuleActivation(
                        rule_id=rule_data.get("rule_id", ""),
                        firing_strength=rule_data.get("firing_strength", 0.0),
                        output_values=output_values
                    ))
            
            # Crear el fuzzy evaluation
            fuzzy_evaluation = FuzzyEvaluation(
                system_id=str(system.id),
                timestamp=datetime.now(timezone.utc),
                inputs=inputs,
                activated_rules=activated_rules
            )
            
            # Guardar en la base de datos
            evaluation_repo: IFuzzyEvaluationRepository = di[IFuzzyEvaluationRepository]
            saved_evaluation = await evaluation_repo.create(fuzzy_evaluation)
            
            _logger.info(
                f"Fuzzy evaluation creado y guardado exitosamente. "
                f"ID: {saved_evaluation.id}, Sistema: {system.name}"
            )
            
            # Enviar payload al actuator service
            await self._send_to_actuator_service(saved_evaluation)
            
            return saved_evaluation
            
        except Exception as e:
            error_msg = f"Error al crear y guardar fuzzy evaluation: {str(e)}"
            _logger.error(error_msg)
            raise BusinessRuleViolationError(error_msg)
    
    async def _send_to_actuator_service(self, fuzzy_evaluation: FuzzyEvaluation) -> None:
        """
        Envía el payload al actuator service basado en la evaluación fuzzy.
        
        Args:
            fuzzy_evaluation: Evaluación fuzzy con las reglas activadas y outputs
        """
        try:
            routines = []
            
            # Convertir cada regla activada en una rutina para el actuator service
            for rule_activation in fuzzy_evaluation.activated_rules:
                steps = []
                
                # Convertir cada output_value en un step
                for output_value in rule_activation.output_values:
                    step = StepPayload(
                        actuator={"$oid": output_value.actuator_id},
                        power=output_value.power,
                        duration=output_value.duration
                    )
                    steps.append(step)
                
                # Crear la rutina si tiene steps
                if steps:
                    routine = RoutinePayload(
                        routineId=rule_activation.rule_id,
                        steps=steps
                    )
                    routines.append(routine)
            
            # Enviar al actuator service si hay rutinas
            if routines:
                command = SendRoutinesToActuatorCommand(
                    routines=routines
                )
                
                # Enviar comando a través del mediador
                mediator: Medyator = di[Medyator]
                await mediator.send(command)
                
                _logger.info(
                    f"Payload enviado al actuator service exitosamente. "
                    f"Evaluación: {fuzzy_evaluation.id}, Rutinas: {len(routines)}"
                )
            else:
                _logger.warning(
                    f"No se encontraron rutinas para enviar al actuator service. "
                    f"Evaluación: {fuzzy_evaluation.id}"
                )
                
        except Exception as e:
            error_msg = f"Error al enviar payload al actuator service: {str(e)}"
            _logger.error(error_msg)
            # No lanzamos excepción para no interrumpir el flujo principal
            # El fuzzy evaluation ya fue guardado exitosamente