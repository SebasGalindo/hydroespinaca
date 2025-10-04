from __future__ import annotations

from typing import List
from kink import di
from medyator import QueryHandler

from FuzzyService.Application.Features.FuzzyVariables.DTOs.FuzzyVariableDto import FuzzyVariableDto
from FuzzyService.Application.Features.FuzzyVariables.Queries.GetAllFuzzyVariablesQuery import GetAllFuzzyVariablesQuery
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyTermId


class GetAllFuzzyVariablesHandler(QueryHandler[GetAllFuzzyVariablesQuery, List[FuzzyVariableDto]]):
    async def __call__(self, request: GetAllFuzzyVariablesQuery) -> List[FuzzyVariableDto]:  # type: ignore[override]
        repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]

        filters = {}
        if request.variable_type:
            filters["variable_type"] = request.variable_type
        if request.reference_id:
            filters["reference_id"] = request.reference_id
        if request.name_contains:
            filters["name_contains"] = request.name_contains
        if request.term_id:
            filters["term_id"] = FuzzyTermId(request.term_id)

        variables = await repo.filter_variables(filters, skip=request.skip, limit=request.limit)
        return [FuzzyVariableDto.from_entity(v) for v in variables]
