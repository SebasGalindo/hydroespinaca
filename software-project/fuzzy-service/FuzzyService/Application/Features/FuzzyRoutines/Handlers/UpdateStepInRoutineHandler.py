from __future__ import annotations

from medyator import CommandHandler
import kink

from FuzzyService.Application.Features.FuzzyRoutines.Commands.UpdateStepInRoutineCommand import UpdateStepInRoutineCommand
from FuzzyService.Application.Features.FuzzyRoutines.DTOs.FuzzyRoutineDto import FuzzyRoutineDto
from FuzzyService.Domain.Interfaces.IFuzzyRoutineRepository import IFuzzyRoutineRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyRoutineId
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError, BusinessRuleViolationError


class UpdateStepInRoutineHandler(CommandHandler[UpdateStepInRoutineCommand]):
    """Handler para actualizar un paso específico en una rutina difusa."""

    async def __call__(self, request: UpdateStepInRoutineCommand) -> None:
        """Actualiza un paso específico en una rutina difusa."""
        
        # Obtener repositorio
        routine_repository = kink.di[IFuzzyRoutineRepository]
        
        # Verificar que la rutina existe
        existing_routine = await routine_repository.get_by_id(FuzzyRoutineId(request.routine_id))
        if not existing_routine:
            raise EntityNotFoundError(f"Rutina con ID {request.routine_id} no encontrada")
        
        # Buscar el paso a actualizar
        step_to_update = None
        for step in existing_routine.steps:
            if step.step_id == request.step_id:
                step_to_update = step
                break
        
        if not step_to_update:
            raise EntityNotFoundError(
                f"Paso con ID {request.step_id} no encontrado en la rutina {request.routine_id}"
            )
        
        # Actualizar campos del paso si se proporcionan
        if request.condition is not None:
            step_to_update.condition = request.condition
        
        if request.power_tag_id is not None:
            step_to_update.power_tag_id = request.power_tag_id
        
        if request.duration_tag_id is not None:
            step_to_update.duration_tag_id = request.duration_tag_id
        
        # Guardar la rutina actualizada
        updated_routine = await routine_repository.update(existing_routine)
        
        # Asignar resultado
        request._result = FuzzyRoutineDto.from_entity(updated_routine)
