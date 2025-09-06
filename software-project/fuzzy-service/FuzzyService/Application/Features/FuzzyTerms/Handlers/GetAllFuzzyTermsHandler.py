from __future__ import annotations

from typing import List
from kink import di
from medyator import QueryHandler

from FuzzyService.Application.Features.FuzzyTerms.DTOs.FuzzyTermDto import FuzzyTermDto
from FuzzyService.Application.Features.FuzzyTerms.Queries.GetAllFuzzyTermsQuery import GetAllFuzzyTermsQuery
from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyVariableId


class GetAllFuzzyTermsHandler(QueryHandler[GetAllFuzzyTermsQuery, List[FuzzyTermDto]]):
    async def __call__(self, request: GetAllFuzzyTermsQuery) -> List[FuzzyTermDto]:  # type: ignore[override]
        repo: IFuzzyTermRepository = di[IFuzzyTermRepository]

        if request.variable_id:
            terms = await repo.get_by_variable_id(FuzzyVariableId(request.variable_id))
        else:
            terms = await repo.get_all()
        return [FuzzyTermDto.from_entity(t) for t in terms]
