"""Implementación concreta del servicio de lógica difusa.

Esta clase implementa la interfaz IFuzzyService proporcionando
funcionalidad completa de procesamiento difuso.
"""

import logging
from typing import Dict, List, Optional

from application.interfaces.fuzzy_service import IFuzzyService
from domain.interfaces.actuator_client import IActuatorClient
from infrastructure.repositories import RoutineRepository, ActuatorStateRepository, VariableRepository
from domain.models import OutputPlan, Routine, ActuatorState
from application.dtos import ReadingBatch, CreateCommandDto, CommandMetadataDto
from application.use_cases import FuzzyEvaluationUseCase, HysteresisFilterUseCase
from infrastructure.fuzzy_engine import FuzzyEngine, FuzzyRule


class FuzzyServiceImpl(IFuzzyService):
    """Implementación concreta del servicio de lógica difusa."""
    
    def __init__(
        self,
        routine_repository: RoutineRepository,
        actuator_state_repository: ActuatorStateRepository,
        actuator_client: IActuatorClient,
        variable_repository: VariableRepository = None,
        hysteresis_delta: float = 5.0,
        cooldown_seconds: int = 30
    ):
        self.routine_repository = routine_repository
        self.actuator_state_repository = actuator_state_repository
        self.actuator_client = actuator_client
        self.variable_repository = variable_repository
        
        # Inicializar casos de uso
        self.fuzzy_evaluation = FuzzyEvaluationUseCase()
        self.hysteresis_filter = HysteresisFilterUseCase(
            hysteresis_delta=hysteresis_delta,
            cooldown_seconds=cooldown_seconds
        )
        
        # Motor de lógica difusa
        self.fuzzy_engine = self.fuzzy_evaluation.fuzzy_engine
        
        logging.info("FuzzyServiceImpl inicializado")
    
    async def process_readings(self, readings: ReadingBatch) -> List[CreateCommandDto]:
        """Procesa lecturas de sensores y genera comandos para actuadores."""
        try:
            # Obtener rutinas activas
            routines = await self.routine_repository.list_all()
            active_routines = [r for r in routines if r.active]
            
            if not active_routines:
                logging.info("No hay rutinas activas para procesar")
                return []
            
            # Evaluar con lógica difusa
            plans = await self.evaluate_readings(readings, active_routines)
            logging.debug(f"Planes generados: {len(plans)}")
            
            # Aplicar filtros de histeresis
            actuator_states = await self._get_actuator_states()
            filtered_plans = self.hysteresis_filter.filter_plans(plans, actuator_states)
            logging.debug(f"Planes filtrados: {len(filtered_plans)}")
            
            # Convertir planes a comandos
            commands = await self._convert_plans_to_commands(filtered_plans)
            
            # Actualizar estados de actuadores
            await self._update_actuator_states(filtered_plans)
            
            logging.info(f"Procesadas {len(readings.readings)} lecturas, generados {len(commands)} comandos")
            return commands
            
        except Exception as e:
            logging.error(f"Error procesando lecturas: {e}")
            return []
    
    async def evaluate_routines(self, readings: ReadingBatch) -> List[CreateCommandDto]:
        """Evalúa rutinas activas contra lecturas de sensores."""
        return await self.process_readings(readings)
    
    async def evaluate_readings(self, batch: ReadingBatch, routines: List[Routine]) -> List[OutputPlan]:
        """Evalúa lecturas usando lógica difusa y genera planes de acción."""
        return self.fuzzy_evaluation.evaluate_batch(batch, routines)
    
    async def get_fuzzy_variables_info(self) -> Dict[str, Dict]:
        """Obtiene información sobre las variables difusas configuradas."""
        # Primero intentar obtener variables del repositorio (para tests)
        if hasattr(self, 'variable_repository'):
            try:
                variables = await self.variable_repository.get_all()
                variables_info = {}
                for var in variables:
                    fuzzy_sets_info = {fs.name: {
                        "type": fs.membership_type.value,
                        "parameters": fs.parameters,
                        "description": fs.description
                    } for fs in var.fuzzy_sets}
                    
                    variables_info[var.id] = {
                        "name": var.name,
                        "unit": var.unit,
                        "range": [var.min_value, var.max_value] if var.min_value is not None and var.max_value is not None else None,
                        "fuzzy_sets": fuzzy_sets_info,
                        "description": var.description
                    }
                return variables_info
            except Exception:
                pass
        
        # Fallback al motor difuso
        variables_info = {}
        for var_name in self.fuzzy_engine.fuzzy_vars.keys():
            info = self.fuzzy_engine.get_variable_info(var_name)
            if info:
                variables_info[var_name] = info
        
        return variables_info
    
    async def simulate_evaluation(self, inputs: Dict[str, float]) -> Dict[str, float]:
        """Simula una evaluación difusa con valores de entrada específicos."""
        try:
            return self.fuzzy_engine.evaluate(inputs)
        except Exception as e:
            logging.error(f"Error en simulación difusa: {e}")
            return {"intensidad": 0.0}
    
    async def add_fuzzy_rule(self, rule_id: str, antecedents: List[tuple], 
                           consequent: tuple, weight: float = 1.0) -> bool:
        """Añade una nueva regla difusa al sistema."""
        try:
            rule = FuzzyRule(rule_id, antecedents, consequent, weight)
            self.fuzzy_engine.add_rule(rule)
            logging.info(f"Regla difusa añadida: {rule_id}")
            return True
        except Exception as e:
            logging.error(f"Error añadiendo regla difusa {rule_id}: {e}")
            return False
    
    async def remove_fuzzy_rule(self, rule_id: str) -> bool:
        """Elimina una regla difusa del sistema."""
        try:
            # Filtrar reglas para remover la especificada
            original_count = len(self.fuzzy_engine.rules)
            self.fuzzy_engine.rules = [
                rule for rule in self.fuzzy_engine.rules 
                if rule.rule_id != rule_id
            ]
            
            removed = len(self.fuzzy_engine.rules) < original_count
            if removed:
                logging.info(f"Regla difusa eliminada: {rule_id}")
            else:
                logging.warning(f"Regla difusa no encontrada: {rule_id}")
            
            return removed
        except Exception as e:
            logging.error(f"Error eliminando regla difusa {rule_id}: {e}")
            return False
    
    async def get_active_rules(self) -> List[Dict]:
        """Obtiene información sobre las reglas difusas activas."""
        rules_info = []
        
        for rule in self.fuzzy_engine.rules:
            rule_info = {
                "rule_id": rule.rule_id,
                "antecedents": rule.antecedents,
                "consequent": rule.consequent,
                "weight": rule.weight
            }
            rules_info.append(rule_info)
        
        return rules_info
    
    async def _get_actuator_states(self) -> Dict[str, ActuatorState]:
        """Obtiene los estados actuales de todos los actuadores."""
        try:
            return await self.actuator_state_repository.get_all_states()
        except Exception as e:
            logging.error(f"Error obteniendo estados de actuadores: {e}")
            return {}
    
    async def _convert_plans_to_commands(self, plans: List[OutputPlan]) -> List[CreateCommandDto]:
        """Convierte planes de acción en comandos para actuadores."""
        commands = []
        
        for plan in plans:
            try:
                command = CreateCommandDto(
                    actuatorId=plan.actuator.actuatorId,
                    target=plan.target,
                    holdSeconds=plan.hold_seconds,
                    metadata=CommandMetadataDto(
                        fuzzyRule=plan.fuzzy_rule,
                        source="fuzzy_service",
                        timestamp=None  # Se asignará automáticamente
                    )
                )
                commands.append(command)
            except Exception as e:
                logging.error(f"Error creando comando para actuador {plan.actuator.actuatorId}: {e}")
        
        return commands
    
    async def _update_actuator_states(self, plans: List[OutputPlan]) -> None:
        """Actualiza los estados de los actuadores basado en los planes ejecutados."""
        for plan in plans:
            try:
                # Obtener estado actual o crear uno nuevo
                state = await self.actuator_state_repository.get_state(plan.actuator.actuatorId)
                
                if state:
                    # Actualizar estado existente
                    state.update_state(plan.target)
                else:
                    # Crear nuevo estado
                    state = ActuatorState(
                        actuatorId=plan.actuator.actuatorId,
                        last_target=plan.target,
                        last_emitted_at=None  # Se asignará automáticamente
                    )
                
                await self.actuator_state_repository.save_state(plan.actuator.actuatorId, state)
                
            except Exception as e:
                logging.error(f"Error actualizando estado del actuador {plan.actuator.actuatorId}: {e}")
    
    async def get_actuators_status(self) -> List[Dict]:
        """Obtiene el estado actual de todos los actuadores."""
        try:
            states = await self._get_actuator_states()
            actuators_status = []
            
            for actuator_id, state in states.items():
                status_info = {
                    "actuator_id": actuator_id,
                    "last_target": state.last_target,
                    "last_emitted_at": state.last_emitted_at.isoformat() if state.last_emitted_at else None,
                    "is_in_cooldown": state.is_in_cooldown(),
                    "cooldown_remaining": state.get_cooldown_remaining_seconds()
                }
                actuators_status.append(status_info)
            
            return actuators_status
        except Exception as e:
            logging.error(f"Error obteniendo estado de actuadores: {e}")
            return []
    
    async def get_system_metrics(self) -> Dict:
        """Obtiene métricas del sistema difuso."""
        try:
            # Obtener información de reglas activas
            active_rules = await self.get_active_rules()
            
            # Obtener información de variables
            variables_info = await self.get_fuzzy_variables_info()
            
            # Obtener estado de actuadores
            actuators_status = await self.get_actuators_status()
            
            # Calcular métricas
            metrics = {
                "fuzzy_system": {
                    "total_rules": len(active_rules),
                    "total_variables": len(variables_info),
                    "engine_status": "active" if self.fuzzy_engine else "inactive"
                },
                "actuators": {
                    "total_actuators": len(actuators_status),
                    "active_actuators": len([a for a in actuators_status if a["last_target"] is not None]),
                    "in_cooldown": len([a for a in actuators_status if a["is_in_cooldown"]])
                },
                "evaluations": {
                    "total_evaluations": 0  # TODO: Implementar contador de evaluaciones
                },
                "commands": {
                    "commands_sent_today": 0  # TODO: Implementar contador de comandos
                },
                "performance": {
                    "average_response_time_ms": 0.0  # TODO: Implementar medición de tiempo
                },
                "system": {
                    "uptime_seconds": 0  # TODO: Implementar cálculo de uptime
                },
                "system_health": {
                    "fuzzy_engine_initialized": self.fuzzy_engine is not None,
                    "repositories_connected": all([
                        self.routine_repository is not None,
                        self.actuator_state_repository is not None
                    ])
                }
            }
            
            return metrics
        except Exception as e:
            logging.error(f"Error obteniendo métricas del sistema: {e}")
            return {
                "fuzzy_system": {"total_rules": 0, "total_variables": 0, "engine_status": "error"},
                "actuators": {"total_actuators": 0, "active_actuators": 0, "in_cooldown": 0},
                "evaluations": {"total_evaluations": 0},
                "commands": {"commands_sent_today": 0},
                "performance": {"average_response_time_ms": 0.0},
                "system": {"uptime_seconds": 0},
                "system_health": {"fuzzy_engine_initialized": False, "repositories_connected": False}
            }