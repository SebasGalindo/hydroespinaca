from __future__ import annotations

from medyator import CommandHandler
import kink

from FuzzyService.Application.Features.FuzzyRoutines.Commands.DeleteFuzzyRoutineCommand import DeleteFuzzyRoutineCommand
from FuzzyService.Domain.Interfaces.IFuzzyRoutineRepository import IFuzzyRoutineRepository
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyRoutineId


class DeleteFuzzyRoutineHandler(CommandHandler[DeleteFuzzyRoutineCommand]):
    """Handler para eliminar una rutina difusa."""

    async def __call__(self, request: DeleteFuzzyRoutineCommand) -> None:
        """Elimina una rutina difusa."""
        
        # Obtener repositorio
        routine_repository = kink.di[IFuzzyRoutineRepository]
        
        # Verificar que la rutina existe
        existing_routine = await routine_repository.get_by_id(FuzzyRoutineId(request.routine_id))
        if existing_routine is None:
            raise EntityNotFoundError(f"Rutina con ID {request.routine_id} no encontrada")
        
        # Eliminar la rutina
        success = await routine_repository.delete(FuzzyRoutineId(request.routine_id))
        
        # Asignar resultado
        request._result = success
