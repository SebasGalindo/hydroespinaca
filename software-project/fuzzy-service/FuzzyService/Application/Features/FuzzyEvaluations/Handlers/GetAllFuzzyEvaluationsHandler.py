from __future__ import annotations

from typing import List

from FuzzyService.Application.Features.FuzzyEvaluations.Queries.GetAllFuzzyEvaluationsQuery import (
    GetAllFuzzyEvaluationsQuery,
    GetAllFuzzyEvaluationsResponse
)
from FuzzyService.Application.Features.FuzzyEvaluations.DTOs.FuzzyEvaluationDto import FuzzyEvaluationDto
from FuzzyService.Domain.Interfaces.IFuzzyEvaluationRepository import IFuzzyEvaluationRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId
from kink import di


class GetAllFuzzyEvaluationsHandler:
    """Handler para obtener todas las evaluaciones fuzzy con filtros y paginación."""
    
    async def __call__(self, query: GetAllFuzzyEvaluationsQuery) -> GetAllFuzzyEvaluationsResponse:
        """Ejecuta la query para obtener evaluaciones fuzzy."""
        
        # Obtener repositorio
        evaluation_repo: IFuzzyEvaluationRepository = di[IFuzzyEvaluationRepository]
        
        # Determinar qué método usar basado en los filtros
        if query.system_id:
            # Filtrar por sistema específico
            system_id = FuzzySystemId(query.system_id)
            evaluations = await evaluation_repo.get_by_system_id(
                system_id=system_id,
                skip=query.skip,
                limit=query.limit
            )
            total_count = await evaluation_repo.count_by_system(system_id)
        elif query.start_date and query.end_date:
            # Filtrar por rango de fechas
            evaluations = await evaluation_repo.get_by_date_range(
                start_date=query.start_date,
                end_date=query.end_date,
                skip=query.skip,
                limit=query.limit
            )
            # Para el conteo total con fechas, usamos filter_evaluations
            filters = {'start_date': query.start_date, 'end_date': query.end_date}
            all_filtered = await evaluation_repo.filter_evaluations(filters=filters, skip=0, limit=10000)
            total_count = len(all_filtered)
        else:
            # Obtener todas las evaluaciones
            evaluations = await evaluation_repo.get_all(
                skip=query.skip,
                limit=query.limit
            )
            total_count = await evaluation_repo.count_total()
        
        # Convertir a DTOs
        evaluation_dtos = [FuzzyEvaluationDto.from_entity(eval) for eval in evaluations]
        
        # Crear respuesta
        return GetAllFuzzyEvaluationsResponse.create(
            evaluations=evaluation_dtos,
            total_count=total_count,
            page=query.page,
            page_size=query.page_size
        )