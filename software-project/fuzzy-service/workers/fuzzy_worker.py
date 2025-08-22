"""Worker principal para procesamiento de lecturas y generación de comandos.

Maneja el flujo completo desde la recepción de lecturas MQTT hasta
el envío de comandos al actuator-service.
"""

import asyncio
import logging
from datetime import datetime, timedelta, timezone
from typing import Dict, List

from application.dtos import (
    CommandMetadataDto,
    CreateCommandDto,
    ReadingBatch,
)
from application.use_cases import (
    FuzzyEvaluationUseCase,
    HysteresisFilterUseCase,
)
from domain.models import ActuatorState, OutputPlan
from infrastructure.actuator_client import ActuatorServiceClient
from infrastructure.config import settings
from infrastructure.repositories import (
    ActuatorStateRepository,
    RoutineRepository,
)


class FuzzyWorker:
    """Worker principal para procesamiento de lógica difusa.
    
    Coordina la evaluación de lecturas, filtrado por histeresis/cooldown,
    generación de planes de descenso gradual y envío de comandos.
    """
    
    def __init__(
        self,
        routine_repository: RoutineRepository,
        actuator_state_repository: ActuatorStateRepository,
        actuator_client: ActuatorServiceClient,
    ):
        self.routine_repository = routine_repository
        self.actuator_state_repository = actuator_state_repository
        self.actuator_client = actuator_client
        
        # Inicializar casos de uso
        self.evaluation_use_case = FuzzyEvaluationUseCase()
        self.hysteresis_use_case = HysteresisFilterUseCase(
            hysteresis_delta=settings.hysteresis_delta_percent,
            cooldown_seconds=settings.cooldown_seconds
        )
        
        # Estado interno
        self._scheduled_commands: Dict[str, List[CreateCommandDto]] = {}
        self._running = False
        
        logging.info("FuzzyWorker inicializado")
    
    async def process_reading_batch(self, batch: ReadingBatch) -> None:
        """Procesa un lote de lecturas recibido vía MQTT.
        
        Args:
            batch: Lote de lecturas de sensores
        """
        try:
            logging.info(
                "Procesando lote: esp32=%s, readings=%d",
                batch.esp32Id,
                len(batch.readings)
            )
            
            # 1. Obtener rutinas activas
            routines = await self.routine_repository.list_active()
            if not routines:
                logging.debug("No hay rutinas activas")
                return
            
            # 2. Evaluar lecturas contra rutinas
            plans = self.evaluation_use_case.evaluate_batch(batch, routines)
            if not plans:
                logging.debug("No se generaron planes de acción")
                return
            
            # 3. Aplicar filtros de histeresis y cooldown
            actuator_states = await self.actuator_state_repository.get_all_states()
            filtered_plans = self.hysteresis_use_case.filter_plans(plans, actuator_states)
            
            if not filtered_plans:
                logging.debug("Todos los planes fueron filtrados por histeresis/cooldown")
                return
            
            # 4. Generar y programar comandos de descenso gradual
            for plan in filtered_plans:
                await self._schedule_ramp_commands(plan, batch.esp32Id)
                
                # 5. Actualizar estado del actuador
                await self._update_actuator_state(plan)
            
        except Exception as e:
            logging.error(
                "Error procesando lote esp32=%s: %s",
                batch.esp32Id,
                str(e)
            )
    
    async def _schedule_ramp_commands(self, plan: OutputPlan, esp32_id: str) -> None:
        """Genera y programa comandos de descenso gradual.
        
        Args:
            plan: Plan de acción para el actuador
            esp32_id: ID del ESP32 que originó las lecturas
        """
        actuator_id = plan.actuator.actuatorId
        
        # Generar secuencia de comandos
        commands = self._generate_ramp_commands(plan, esp32_id)
        
        # Programar comandos
        self._scheduled_commands[actuator_id] = commands
        
        logging.info(
            "Programados %d comandos para actuator=%s (target=%d%%, hold=%ds)",
            len(commands),
            actuator_id,
            plan.target,
            plan.hold_seconds
        )
        
        # Iniciar ejecución asíncrona
        asyncio.create_task(self._execute_ramp_commands(actuator_id))
    
    def _generate_ramp_commands(self, plan: OutputPlan, esp32_id: str) -> List[CreateCommandDto]:
        """Genera la secuencia de comandos para descenso gradual.
        
        Args:
            plan: Plan de acción
            esp32_id: ID del ESP32
            
        Returns:
            Lista de comandos programados
        """
        commands: List[CreateCommandDto] = []
        
        # Calcular número de pasos
        total_steps = max(1, plan.hold_seconds // settings.ramp_step_seconds)
        step_duration_ms = (plan.hold_seconds * 1000) // total_steps
        
        # Generar pasos de descenso
        for step in range(total_steps):
            # Calcular target para este paso (descenso lineal)
            progress = step / (total_steps - 1) if total_steps > 1 else 1.0
            current_target = plan.target * (1.0 - progress)
            
            # Redondear a múltiplos de 5 y asegurar mínimo
            rounded_target = max(0, round(current_target / 5) * 5)
            
            # Crear comando
            command = CreateCommandDto(
                ActuatorId=plan.actuator.actuatorId,
                Esp32Id=esp32_id,
                Action=f"pwm:{rounded_target}",
                DurationMs=step_duration_ms,
                Trigger="Fuzzy",
                Metadata=CommandMetadataDto(
                    Source="fuzzy-service",
                    FuzzyRule=plan.fuzzy_rule,
                    Inputs={"step": step + 1, "total_steps": total_steps}
                )
            )
            
            commands.append(command)
        
        # Asegurar que el último comando sea PWM 0
        if commands and not commands[-1].Action.endswith(":0"):
            final_command = CreateCommandDto(
                ActuatorId=plan.actuator.actuatorId,
                Esp32Id=esp32_id,
                Action="pwm:0",
                DurationMs=1000,  # 1 segundo final
                Trigger="Fuzzy",
                Metadata=CommandMetadataDto(
                    Source="fuzzy-service",
                    FuzzyRule=plan.fuzzy_rule,
                    Inputs={"step": "final", "total_steps": total_steps}
                )
            )
            commands.append(final_command)
        
        return commands
    
    async def _execute_ramp_commands(self, actuator_id: str) -> None:
        """Ejecuta la secuencia de comandos programados.
        
        Args:
            actuator_id: ID del actuador
        """
        commands = self._scheduled_commands.get(actuator_id, [])
        if not commands:
            return
        
        try:
            for i, command in enumerate(commands):
                # Enviar comando
                success = await self.actuator_client.send_command_with_retry(command)
                
                if success:
                    logging.info(
                        "Comando enviado (%d/%d): %s -> %s",
                        i + 1,
                        len(commands),
                        actuator_id,
                        command.Action
                    )
                else:
                    logging.error(
                        "Falló comando (%d/%d): %s -> %s",
                        i + 1,
                        len(commands),
                        actuator_id,
                        command.Action
                    )
                
                # Esperar antes del siguiente comando (excepto el último)
                if i < len(commands) - 1:
                    await asyncio.sleep(settings.ramp_step_seconds)
                    
        except Exception as e:
            logging.error(
                "Error ejecutando comandos para actuator=%s: %s",
                actuator_id,
                str(e)
            )
        finally:
            # Limpiar comandos programados
            self._scheduled_commands.pop(actuator_id, None)
    
    async def _update_actuator_state(self, plan: OutputPlan) -> None:
        """Actualiza el estado del actuador después de generar un plan.
        
        Args:
            plan: Plan ejecutado
        """
        actuator_id = plan.actuator.actuatorId
        
        # Crear nuevo estado
        new_state = ActuatorState(
            last_target=plan.target,
            last_emitted_at=datetime.now(timezone.utc)
        )
        
        # Guardar estado
        await self.actuator_state_repository.save_state(actuator_id, new_state)
        
        logging.debug(
            "Estado actualizado: actuator=%s, target=%d%%, timestamp=%s",
            actuator_id,
            plan.target,
            new_state.last_emitted_at.isoformat()
        )
    
    async def start(self) -> None:
        """Inicia el worker."""
        self._running = True
        logging.info("FuzzyWorker iniciado")
    
    async def stop(self) -> None:
        """Detiene el worker."""
        self._running = False
        
        # Cancelar comandos pendientes
        for actuator_id in list(self._scheduled_commands.keys()):
            self._scheduled_commands.pop(actuator_id, None)
            
        logging.info("FuzzyWorker detenido")
    
    @property
    def is_running(self) -> bool:
        """Indica si el worker está ejecutándose."""
        return self._running