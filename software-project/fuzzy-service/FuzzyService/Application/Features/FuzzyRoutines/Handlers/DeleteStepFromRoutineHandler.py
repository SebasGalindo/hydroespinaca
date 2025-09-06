from __future__ import annotations

from medyator import CommandHandler
import kink

from FuzzyService.Application.Features.FuzzyRoutines.Commands.DeleteStepFromRoutineCommand import DeleteStepFromRoutineCommand
from FuzzyService.Application.Features.FuzzyRoutines.DTOs.FuzzyRoutineDto import FuzzyRoutineDto
from FuzzyService.Domain.Interfaces.IFuzzyRoutineRepository import IFuzzyRoutineRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyRoutineId
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError


class DeleteStepFromRoutineHandler(CommandHandler[DeleteStepFromRoutineCommand]):
    """Handler para eliminar un paso específico de una rutina difusa."""

    async def __call__(self, request: DeleteStepFromRoutineCommand) -> None:
        """Elimina un paso específico de una rutina difusa."""
        
        # Obtener repositorio
        routine_repository = kink.di[IFuzzyRoutineRepository]
        
        # Verificar que la rutina existe
        existing_routine = await routine_repository.get_by_id(FuzzyRoutineId(request.routine_id))
        if not existing_routine:
            raise EntityNotFoundError(f"Rutina con ID {request.routine_id} no encontrada")
        
        # Verificar que el paso existe
        step_exists = any(step.step_id == request.step_id for step in existing_routine.steps)
        if not step_exists:
            raise EntityNotFoundError(
                f"Paso con ID {request.step_id} no encontrado en la rutina {request.routine_id}"
            )
        
        # Eliminar el paso
        existing_routine.remove_step(request.step_id)
        
        # Guardar la rutina actualizada
        updated_routine = await routine_repository.update(existing_routine)
        
        # Asignar resultado
        request._result = FuzzyRoutineDto.from_entity(updated_routine)
