from __future__ import annotations

from kink import di
from medyator import QueryHandler

from FuzzyService.Application.Features.FuzzyVariables.DTOs.FuzzyVariableDto import FuzzyVariableDto
from FuzzyService.Application.Features.FuzzyVariables.Queries.GetFuzzyVariableByIdQuery import GetFuzzyVariableByIdQuery
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyVariableId
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError


class GetFuzzyVariableByIdHandler(QueryHandler[GetFuzzyVariableByIdQuery, FuzzyVariableDto]):
    async def __call__(self, request: GetFuzzyVariableByIdQuery) -> FuzzyVariableDto:  # type: ignore[override]
        repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]

        entity = await repo.get_by_id(FuzzyVariableId(request.id))
        if entity is None:
            raise EntityNotFoundError(f"No se encontró la variable con id '{request.id}'")

        return FuzzyVariableDto.from_entity(entity)
