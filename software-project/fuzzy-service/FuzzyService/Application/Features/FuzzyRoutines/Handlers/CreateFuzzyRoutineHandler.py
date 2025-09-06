from __future__ import annotations

from medyator import CommandHandler
from kink import di

from FuzzyService.Application.Features.FuzzyRoutines.Commands.CreateFuzzyRoutineCommand import CreateFuzzyRoutineCommand
from FuzzyService.Application.Features.FuzzyRoutines.DTOs.FuzzyRoutineDto import FuzzyRoutineDto
from FuzzyService.Domain.Entities.fuzzy_routine import FuzzyRoutine
from FuzzyService.Domain.Interfaces.IFuzzyRoutineRepository import IFuzzyRoutineRepository
from FuzzyService.Domain.Errors.DomainErrors import BusinessRuleViolationError


class CreateFuzzyRoutineHandler(CommandHandler[CreateFuzzyRoutineCommand]):
    """Handler para crear una nueva rutina difusa."""

    async def __call__(self, request: CreateFuzzyRoutineCommand) -> None:
        """Crea una nueva rutina difusa."""
        
        # Obtener repositorio
        routine_repository: IFuzzyRoutineRepository = di[IFuzzyRoutineRepository]
        
        # Crear nueva rutina difusa
        routine = FuzzyRoutine(
            routine_name=request.routine_name,
            steps=[step.to_entity() for step in request.steps]
        )
        
        # Guardar la rutina
        saved_routine = await routine_repository.create(routine)
        
        # Asignar resultado
        request._result = FuzzyRoutineDto.from_entity(saved_routine)
