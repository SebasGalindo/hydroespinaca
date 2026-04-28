from __future__ import annotations

from typing import List

from FuzzyService.Application.Features.FuzzyEvaluations.Queries.GetFuzzyEvaluationsBySystemQuery import (
    GetFuzzyEvaluationsBySystemQuery,
    GetFuzzyEvaluationsBySystemResponse
)
from FuzzyService.Application.Features.FuzzyEvaluations.DTOs.FuzzyEvaluationDto import FuzzyEvaluationDto
from FuzzyService.Domain.Interfaces.IFuzzyEvaluationRepository import IFuzzyEvaluationRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId
from kink import di


class GetFuzzyEvaluationsBySystemHandler:
    """Handler para obtener evaluaciones fuzzy de un sistema específico."""
    
    async def __call__(self, query: GetFuzzyEvaluationsBySystemQuery) -> GetFuzzyEvaluationsBySystemResponse:
        """Ejecuta la query para obtener evaluaciones fuzzy por sistema."""
        
        # Obtener repositorio
        evaluation_repo: IFuzzyEvaluationRepository = di[IFuzzyEvaluationRepository]
        
        # Crear ID de dominio
        system_id = FuzzySystemId(query.system_id)
        
        # Obtener evaluaciones del sistema. Aplicamos el filtro por fecha si
        # cualquiera de los dos extremos está presente — antes se requerían
        # ambos y el filtro nunca se activaba cuando el cliente solo enviaba
        # `start_date`.
        if query.start_date or query.end_date:
            filters: dict = {'system_id': query.system_id}
            if query.start_date:
                filters['start_date'] = query.start_date
            if query.end_date:
                filters['end_date'] = query.end_date
            evaluations = await evaluation_repo.filter_evaluations(
                filters=filters,
                skip=query.skip,
                limit=query.limit
            )
            total_count = await evaluation_repo.count_filtered_evaluations(filters)
        else:
            # Sin filtros de fecha, usar get_by_system_id
            evaluations = await evaluation_repo.get_by_system_id(
                system_id=system_id,
                skip=query.skip,
                limit=query.limit
            )
            total_count = await evaluation_repo.count_by_system(system_id)
        
        # Convertir a DTOs
        evaluation_dtos = [FuzzyEvaluationDto.from_entity(eval) for eval in evaluations]
        
        # Crear respuesta
        return GetFuzzyEvaluationsBySystemResponse.create(
            evaluations=evaluation_dtos,
            system_id=query.system_id,
            total_count=total_count,
            page=query.page,
            page_size=query.page_size
        )