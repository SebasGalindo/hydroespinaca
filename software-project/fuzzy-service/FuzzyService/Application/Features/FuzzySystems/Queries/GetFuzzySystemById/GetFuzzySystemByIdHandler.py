from __future__ import annotations

from kink import di
from medyator import QueryHandler

from FuzzyService.Application.Features.FuzzySystems.DTOs.FuzzySystemDto import FuzzySystemDto
from FuzzyService.Application.Features.FuzzySystems.Queries.GetFuzzySystemById.GetFuzzySystemByIdQuery import GetFuzzySystemByIdQuery
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError


class GetFuzzySystemByIdHandler(QueryHandler[GetFuzzySystemByIdQuery, FuzzySystemDto]):
    async def __call__(self, request: GetFuzzySystemByIdQuery) -> FuzzySystemDto:  # type: ignore[override]
        repo: IFuzzySystemRepository = di[IFuzzySystemRepository]

        entity = await repo.get_by_id(FuzzySystemId(request.id))
        if entity is None:
            raise EntityNotFoundError(f"No se encontró el sistema con id '{request.id}'")

        return FuzzySystemDto.from_entity(entity)
