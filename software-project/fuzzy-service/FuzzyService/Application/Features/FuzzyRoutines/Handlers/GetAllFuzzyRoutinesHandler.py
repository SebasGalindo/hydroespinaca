from __future__ import annotations

from typing import List
from medyator import QueryHandler
from kink import di

from FuzzyService.Application.Features.FuzzyRoutines.Queries.GetAllFuzzyRoutinesQuery import GetAllFuzzyRoutinesQuery
from FuzzyService.Application.Features.FuzzyRoutines.DTOs.FuzzyRoutineDto import FuzzyRoutineDto
from FuzzyService.Domain.Interfaces.IFuzzyRoutineRepository import IFuzzyRoutineRepository


class GetAllFuzzyRoutinesHandler(QueryHandler[GetAllFuzzyRoutinesQuery, List[FuzzyRoutineDto]]):
    """Handler para obtener todas las rutinas difusas con paginación."""

    async def __call__(self, request: GetAllFuzzyRoutinesQuery) -> None:
        """Obtiene todas las rutinas difusas con paginación."""
        
        # Obtener repositorio
        routine_repository: IFuzzyRoutineRepository = di[IFuzzyRoutineRepository]
        
        # Obtener rutinas con paginación
        routines = await routine_repository.get_all(skip=request.skip, limit=request.limit)
        
        # Convertir a DTOs
        routine_dtos = [FuzzyRoutineDto.from_entity(routine) for routine in routines]
        
        # Asignar resultado
        request._result = routine_dtos
