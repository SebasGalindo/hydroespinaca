from __future__ import annotations

from medyator import CommandHandler
from kink import di

from FuzzyService.Application.Features.FuzzyRoutines.Commands.UpdateFuzzyRoutineCommand import UpdateFuzzyRoutineCommand
from FuzzyService.Application.Features.FuzzyRoutines.DTOs.FuzzyRoutineDto import FuzzyRoutineDto
from FuzzyService.Domain.Interfaces.IFuzzyRoutineRepository import IFuzzyRoutineRepository
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError, BusinessRuleViolationError
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyRoutineId


class UpdateFuzzyRoutineHandler(CommandHandler[UpdateFuzzyRoutineCommand]):
    """Handler para actualizar una rutina difusa existente."""

    async def __call__(self, request: UpdateFuzzyRoutineCommand) -> None:
        """Actualiza una rutina difusa existente."""
        
        # Obtener repositorio
        routine_repository: IFuzzyRoutineRepository = di[IFuzzyRoutineRepository]
        
        # Verificar que la rutina existe
        existing_routine = await routine_repository.get_by_id(FuzzyRoutineId(request.routine_id))
        if existing_routine is None:
            raise EntityNotFoundError(f"Rutina con ID {request.routine_id} no encontrada")

        # Actualizar los campos proporcionados
        if request.routine_name:
            existing_routine.routine_name = request.routine_name
        if request.steps:
            # Convertir DTOs a entidades
            step_entities = [step_dto.to_entity() for step_dto in request.steps]
            existing_routine.steps = step_entities

        # Guardar los cambios
        updated_routine = await routine_repository.update(existing_routine)
        
        # Asignar resultado
        request._result = FuzzyRoutineDto.from_entity(updated_routine)
