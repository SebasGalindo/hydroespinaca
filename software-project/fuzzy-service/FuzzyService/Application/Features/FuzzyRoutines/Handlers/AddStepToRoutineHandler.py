from __future__ import annotations

from medyator import CommandHandler
import kink

from FuzzyService.Application.Features.FuzzyRoutines.Commands.AddStepToRoutineCommand import AddStepToRoutineCommand
from FuzzyService.Application.Features.FuzzyRoutines.DTOs.FuzzyRoutineDto import FuzzyRoutineDto
from FuzzyService.Domain.Interfaces.IFuzzyRoutineRepository import IFuzzyRoutineRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyRoutineId
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError


class AddStepToRoutineHandler(CommandHandler[AddStepToRoutineCommand]):
    """Handler para agregar un paso a una rutina difusa existente."""

    async def __call__(self, request: AddStepToRoutineCommand) -> None:
        """Agrega un paso a una rutina difusa existente."""
        
        # Obtener repositorio
        routine_repository = kink.di[IFuzzyRoutineRepository]
        
        # Verificar que la rutina existe
        existing_routine = await routine_repository.get_by_id(FuzzyRoutineId(request.routine_id))
        if not existing_routine:
            raise EntityNotFoundError(f"Rutina con ID {request.routine_id} no encontrada")
        
        # Convertir DTO a entidad y agregar paso
        step_entity = request.step.to_entity()
        existing_routine.add_step(step_entity)
        
        # Guardar la rutina actualizada
        updated_routine = await routine_repository.update(existing_routine)
        
        # Asignar resultado
        request._result = FuzzyRoutineDto.from_entity(updated_routine)
