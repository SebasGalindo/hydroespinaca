from __future__ import annotations

from kink import di
from medyator import QueryHandler

from FuzzyService.Application.Features.FuzzyTerms.DTOs.FuzzyTermDto import FuzzyTermDto
from FuzzyService.Application.Features.FuzzyTerms.Queries.GetFuzzyTermByIdQuery import GetFuzzyTermByIdQuery
from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyTermId
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError


class GetFuzzyTermByIdHandler(QueryHandler[GetFuzzyTermByIdQuery, FuzzyTermDto]):
    async def __call__(self, request: GetFuzzyTermByIdQuery) -> FuzzyTermDto:  # type: ignore[override]
        repo: IFuzzyTermRepository = di[IFuzzyTermRepository]

        entity = await repo.get_by_id(FuzzyTermId(request.id))
        if entity is None:
            raise EntityNotFoundError(f"No se encontró el término con id '{request.id}'")

        return FuzzyTermDto.from_entity(entity)
