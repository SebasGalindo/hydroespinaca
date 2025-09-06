from __future__ import annotations

from FuzzyService.Application.Features.FuzzyEvaluations.Queries.GetFuzzyEvaluationByIdQuery import (
    GetFuzzyEvaluationByIdQuery,
    GetFuzzyEvaluationByIdResponse
)
from FuzzyService.Application.Features.FuzzyEvaluations.DTOs.FuzzyEvaluationDto import FuzzyEvaluationDto
from FuzzyService.Domain.Interfaces.IFuzzyEvaluationRepository import IFuzzyEvaluationRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyEvaluationId
from kink import di


class GetFuzzyEvaluationByIdHandler:
    """Handler para obtener una evaluación fuzzy específica por su ID."""
    
    async def __call__(self, query: GetFuzzyEvaluationByIdQuery) -> GetFuzzyEvaluationByIdResponse:
        """Ejecuta la query para obtener una evaluación fuzzy por ID."""
        
        # Obtener repositorio
        evaluation_repo: IFuzzyEvaluationRepository = di[IFuzzyEvaluationRepository]
        
        # Crear ID de dominio
        evaluation_id = FuzzyEvaluationId(query.evaluation_id)
        
        # Buscar evaluación
        evaluation = await evaluation_repo.get_by_id(evaluation_id)
        
        # Verificar si existe
        if evaluation is None:
            return GetFuzzyEvaluationByIdResponse.create_not_found()
        
        # Convertir a DTO
        evaluation_dto = FuzzyEvaluationDto.from_entity(evaluation)
        
        # Crear respuesta
        return GetFuzzyEvaluationByIdResponse.create_found(evaluation_dto)