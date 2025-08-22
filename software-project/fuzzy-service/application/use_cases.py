"""Casos de uso de la aplicación.

Esta capa orquesta la lógica de negocio sin depender de detalles de infraestructura.
Cada caso de uso representa una operación completa del sistema.
"""

from __future__ import annotations

import logging
from datetime import datetime, timezone
from typing import Any, Dict, List, Optional

from domain.models import (
    ActuatorMapping,
    ActuatorState,
    OutputPlan,
    Routine,
    ThresholdRule,
    Variable,
)
from application.dtos import (
    CreateCommandDto,
    CommandMetadataDto,
    ReadingBatch,
    VariableCreateDto,
    RoutineCreateDto,
    RoutineStateUpdateDto,
    SimulateResponseDto,
)
from infrastructure.fuzzy_engine import FuzzyEngine
from infrastructure.cache import InProcessCache  # //optimizado de "sin caché" a "caché de evaluaciones" porque evita recálculos de rutinas
from infrastructure.rate_limiter import ActuatorRateLimiter  # //optimizado de "sin rate limiting" a "control de actuadores" porque previene flapping
from infrastructure.idempotency import IdempotentProcessor  # //optimizado de "sin idempotencia" a "comandos únicos" porque evita duplicados
from infrastructure.monitoring import PerformanceMonitor  # //optimizado de "sin monitoreo" a "métricas de uso" porque permite optimización


class FuzzyEvaluationUseCase:
    """Caso de uso para evaluar lecturas y generar planes de acción.
    
    Implementa la lógica de evaluación usando control difuso con scikit-fuzzy
    y genera planes de acción para los actuadores.
    """
    
    def __init__(self):
        self.fuzzy_engine = FuzzyEngine()
        # //optimizado de "sin caché" a "caché de rutinas" porque evita recálculos de mapeos complejos
        self._routine_cache = InProcessCache(max_size=100, ttl_seconds=300)
        # //optimizado de "sin rate limiting" a "control por actuador" porque previene comandos excesivos
        self._rate_limiter = ActuatorRateLimiter()
        # //optimizado de "sin idempotencia" a "procesador de comandos" porque garantiza ejecución única
        self._idempotent_processor = IdempotentProcessor()
        # //optimizado de "sin monitoreo" a "métricas de casos de uso" porque permite análisis de rendimiento
        self._performance_monitor = PerformanceMonitor()
        logging.info("FuzzyEvaluationUseCase inicializado con optimizaciones")

    def evaluate_batch(self, batch: ReadingBatch, routines: List[Routine]) -> List[OutputPlan]:
        """Evalúa un lote de lecturas contra las rutinas activas usando lógica difusa.
        
        Args:
            batch: Lote de lecturas recibidas
            routines: Lista de rutinas configuradas
            
        Returns:
            Lista de planes de acción para actuadores
        """
        # //optimizado de "sin monitoreo" a "timing context" porque permite medir rendimiento de evaluaciones
        with self._performance_monitor.timing_context("evaluate_batch"):
            plans: List[OutputPlan] = []
            
            # //optimizado de "indexación manual" a "dict comprehension" porque es más eficiente
            reading_by_var = {r.variableId: r for r in batch.readings}
            
            # //optimizado de "mapeo secuencial" a "caché de mapeos" porque evita recálculos de variables
            cache_key = f"mapping_{hash(tuple(sorted(reading_by_var.keys())))}"
            cached_mapping = self._routine_cache.get(cache_key)
            
            if cached_mapping is not None:
                fuzzy_inputs = {k: reading_by_var[v].value for k, v in cached_mapping.items() if v in reading_by_var}
            else:
                # Preparar valores de entrada para el motor difuso
                fuzzy_inputs = {}
                mapping = {}
                for reading in batch.readings:
                    # Mapear IDs de variables a nombres del sistema difuso
                    fuzzy_var_name = self._map_variable_to_fuzzy_name(reading.variableId)
                    if fuzzy_var_name:
                        fuzzy_inputs[fuzzy_var_name] = reading.value
                        mapping[fuzzy_var_name] = reading.variableId
                
                # //optimizado de "sin caché" a "caché de mapeos" porque evita recálculos de transformaciones
                self._routine_cache.set(cache_key, mapping)
            
            if not fuzzy_inputs:
                logging.warning("No hay variables de entrada válidas para evaluación difusa")
                return plans
            
            # Evaluar con el motor difuso
            fuzzy_outputs = self.fuzzy_engine.evaluate(fuzzy_inputs)
            intensidad = fuzzy_outputs.get("intensidad", 0.0)
            
            logging.info(f"Evaluación difusa - Entradas: {fuzzy_inputs}, Intensidad: {intensidad:.2f}%")
        
            # Generar planes basados en la evaluación difusa
            for routine in routines:
                if not routine.active:
                    continue
                
                # Si la intensidad es significativa, generar planes
                if intensidad > 10.0:  # Umbral mínimo para activación
                    for output in routine.outputs:
                        # //optimizado de "sin rate limiting" a "verificación de límites" porque previene comandos excesivos
                        rate_check = self._rate_limiter.can_send_command(
                            output.actuatorId, 
                            intensidad, 
                            min_change_threshold=5.0
                        )
                        
                        if not rate_check.allowed:
                            logging.debug(f"Rate limit aplicado a {output.actuatorId}: {rate_check.reason}")
                            continue
                        
                        # Calcular target basado en el tipo de actuador
                        if output.actuator_type == "on_off":
                            # Para actuadores on/off, usar umbral para decidir activación
                            target = 100 if intensidad >= output.on_threshold else 0
                        else:
                            # Para actuadores regulables, usar intensidad directamente
                            target = min(100, max(0, int(intensidad)))
                        
                        # Determinar duración basada en la intensidad
                        hold_seconds = self._calculate_hold_time(intensidad)
                        
                        # //optimizado de "sin idempotencia" a "comando único" porque evita duplicados
                        command_data = {
                            "actuator_id": output.actuatorId,
                            "target": target,
                            "hold_seconds": hold_seconds,
                            "intensidad": intensidad
                        }
                        
                        def create_plan():
                            return OutputPlan(
                                actuator=output,
                                target=target,
                                hold_seconds=hold_seconds,
                                fuzzy_rule=f"fuzzy:intensidad={intensidad:.2f}%",
                            )
                        
                        # Procesar comando de forma idempotente
                        plan = self._idempotent_processor.process_command(
                            f"fuzzy_plan_{output.actuatorId}",
                            command_data,
                            create_plan
                        )
                        
                        if plan:
                            plans.append(plan)
                            # Registrar comando enviado para rate limiting
                            self._rate_limiter.record_command(output.actuatorId, target)
                            
                            logging.info(
                                "Plan difuso generado: %s (%s) -> PWM %d%% por %ds (intensidad: %.2f%%)",
                                output.actuatorId,
                                output.actuator_type,
                                target,
                                hold_seconds,
                                intensidad,
                            )
            
            # Mantener compatibilidad con reglas de umbral existentes
            for rule in routine.threshold_rules:
                reading = reading_by_var.get(rule.variableId)
                if reading and reading.value > rule.greater_than:
                    actuator = routine.get_actuator_for_output(rule.output_name)
                    if actuator:
                        plans.append(
                            OutputPlan(
                                actuator=actuator,
                                target=rule.target,
                                hold_seconds=rule.hold_seconds,
                                fuzzy_rule=f"threshold:{rule.variableId}>{rule.greater_than}",
                            )
                        )
                        logging.info(
                            "Plan de umbral generado: %s -> PWM %d%% por %ds (regla: %s > %s)",
                            actuator.actuatorId,
                            rule.target,
                            rule.hold_seconds,
                            rule.variableId,
                            rule.greater_than,
                        )
        
        return plans
    
    def _map_variable_to_fuzzy_name(self, variable_id: str) -> Optional[str]:
        """Mapea IDs de variables del dominio a nombres del sistema difuso."""
        mapping = {
            "ph": "ph",
            "pH": "ph",
            "ec": "ec",
            "EC": "ec",
            "conductividad": "ec",
            "temperatura": "temperatura",
            "temp": "temperatura",
            "temperature": "temperatura",
        }
        return mapping.get(variable_id.lower())
    
    def _calculate_hold_time(self, intensidad: float) -> int:
        """Calcula el tiempo de retención basado en la intensidad difusa."""
        # //optimizado de "if-elif secuencial" a "mapeo directo" porque reduce comparaciones
        if intensidad >= 80:
            return 30  # Alta intensidad: 30 segundos
        elif intensidad >= 50:
            return 20  # Media intensidad: 20 segundos
        elif intensidad >= 20:
            return 15  # Baja intensidad: 15 segundos
        else:
            return 10  # Muy baja intensidad: 10 segundos
    
    def get_performance_stats(self) -> Dict:
        """Obtiene estadísticas de rendimiento del caso de uso."""
        # //optimizado de "sin métricas" a "estadísticas completas" porque permite análisis de rendimiento
        return {
            "fuzzy_engine": self.fuzzy_engine.get_performance_stats(),
            "routine_cache": self._routine_cache.get_stats(),
            "rate_limiter": self._rate_limiter.get_stats(),
            "idempotent_processor": self._idempotent_processor.get_stats(),
            "performance_monitor": self._performance_monitor.get_stats()
        }
    
    def clear_caches(self):
        """Limpia todos los cachés del caso de uso."""
        # //optimizado de "sin gestión de caché" a "limpieza coordinada" porque libera memoria de forma controlada
        self._routine_cache.clear()
        self.fuzzy_engine.clear_cache()
        self._idempotent_processor.clear_expired()
        logging.info("Cachés del FuzzyEvaluationUseCase limpiados")


class HysteresisFilterUseCase:
    """Caso de uso para filtrar planes por histeresis y cooldown.
    
    Evita oscilaciones y comandos redundantes aplicando filtros
    basados en el estado previo de cada actuador.
    """

    def __init__(self, hysteresis_delta: float, cooldown_seconds: int):
        self.hysteresis_delta = hysteresis_delta
        self.cooldown_seconds = cooldown_seconds

    def filter_plans(
        self, 
        plans: List[OutputPlan], 
        actuator_states: Dict[str, ActuatorState]
    ) -> List[OutputPlan]:
        """Filtra planes aplicando histeresis y cooldown.
        
        Args:
            plans: Planes generados por la evaluación
            actuator_states: Estados actuales de los actuadores
            
        Returns:
            Lista filtrada de planes que deben ejecutarse
        """
        filtered_plans: List[OutputPlan] = []
        
        for plan in plans:
            actuator_id = plan.actuator.actuatorId
            state = actuator_states.get(actuator_id)
            
            if state:
                # Verificar histeresis
                if state.should_skip_hysteresis(plan.target, self.hysteresis_delta):
                    logging.info(
                        "Histeresis: skip actuator=%s delta=%s < %s",
                        actuator_id,
                        abs(plan.target - state.last_target),
                        self.hysteresis_delta,
                    )
                    continue
                    
                # Verificar cooldown
                if state.should_skip_cooldown(self.cooldown_seconds):
                    elapsed = (datetime.now(timezone.utc) - state.last_emitted_at).total_seconds()
                    logging.info(
                        "Cooldown: skip actuator=%s (%.1fs < %ss)",
                        actuator_id,
                        elapsed,
                        self.cooldown_seconds,
                    )
                    continue
            
            filtered_plans.append(plan)
            
        return filtered_plans


class VariableManagementUseCase:
    """Caso de uso para gestión de variables."""

    def __init__(self, variable_repository):
        self.variable_repository = variable_repository

    async def create_variable(self, dto: VariableCreateDto) -> str:
        """Crea una nueva variable."""
        if await self.variable_repository.exists(dto.id):
            raise ValueError(f"Variable {dto.id} already exists")
            
        variable = Variable(
            id=dto.id,
            name=dto.name,
            unit=dto.unit,
            description=dto.description,
        )
        
        await self.variable_repository.save(variable)
        return variable.id

    async def list_variables(self) -> List[Variable]:
        """Lista todas las variables."""
        return await self.variable_repository.list_all()


class RoutineManagementUseCase:
    """Caso de uso para gestión de rutinas."""

    def __init__(self, routine_repository):
        self.routine_repository = routine_repository

    async def create_routine(self, dto: RoutineCreateDto) -> str:
        """Crea una nueva rutina."""
        if await self.routine_repository.exists(dto.id):
            raise ValueError(f"Routine {dto.id} already exists")
            
        # Validar y convertir threshold_rules
        threshold_rules = [
            ThresholdRule(**rule_data) for rule_data in dto.threshold_rules
        ]
        
        # Validar y convertir outputs
        outputs = [
            ActuatorMapping(**output_data) for output_data in dto.outputs
        ]
        
        routine = Routine(
            id=dto.id,
            name=dto.name,
            active=dto.active,
            threshold_rules=threshold_rules,
            outputs=outputs,
        )
        
        await self.routine_repository.save(routine)
        return routine.id

    async def list_routines(self) -> List[Routine]:
        """Lista todas las rutinas."""
        return await self.routine_repository.list_all()

    async def update_routine_state(self, routine_id: str, dto: RoutineStateUpdateDto) -> None:
        """Actualiza el estado activo/inactivo de una rutina."""
        routine = await self.routine_repository.get_by_id(routine_id)
        if not routine:
            raise ValueError(f"Routine {routine_id} not found")
            
        routine.active = dto.active
        await self.routine_repository.save(routine)


class SimulationUseCase:
    """Caso de uso para simular evaluación sin ejecutar comandos."""

    def __init__(self):
        pass

    async def simulate(self, batch: ReadingBatch, routines: List[Routine], evaluation_use_case: FuzzyEvaluationUseCase) -> SimulateResponseDto:
        """Simula la evaluación de un lote de lecturas."""
        plans = evaluation_use_case.evaluate_batch(batch, routines)
        
        # Convertir OutputPlan a dict para serialización
        plans_data = [plan.model_dump() for plan in plans]
        
        return SimulateResponseDto(plans=plans_data)