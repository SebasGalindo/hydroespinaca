from __future__ import annotations

from medyator import QueryHandler
from kink import di

from FuzzyService.Application.Features.FuzzyRoutines.Queries.GetFuzzyRoutineByIdQuery import GetFuzzyRoutineByIdQuery
from FuzzyService.Application.Features.FuzzyRoutines.DTOs.FuzzyRoutineDto import FuzzyRoutineDto
from FuzzyService.Domain.Interfaces.IFuzzyRoutineRepository import IFuzzyRoutineRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyRoutineId


class GetFuzzyRoutineByIdHandler(QueryHandler[GetFuzzyRoutineByIdQuery, FuzzyRoutineDto]):
    """Handler para obtener una rutina difusa por ID."""

    async def __call__(self, request: GetFuzzyRoutineByIdQuery) -> None:
        """Obtiene una rutina difusa por ID."""
        
        # Obtener repositorio
        routine_repository: IFuzzyRoutineRepository = di[IFuzzyRoutineRepository]
        
        # Obtener rutina por ID
        routine = await routine_repository.get_by_id(FuzzyRoutineId(request.routine_id))
        
        # Verificar si existe y convertir a DTO
        if routine is None:
            request._result = None
        else:
            routine_dto = FuzzyRoutineDto.from_entity(routine)
            request._result = routine_dto
