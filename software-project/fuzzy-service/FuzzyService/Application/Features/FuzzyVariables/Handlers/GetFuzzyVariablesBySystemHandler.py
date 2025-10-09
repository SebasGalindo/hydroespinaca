from __future__ import annotations

from typing import List
from kink import di
from medyator import QueryHandler

from FuzzyService.Application.Features.FuzzyVariables.DTOs.FuzzyVariableDto import FuzzyVariableDto
from FuzzyService.Application.Features.FuzzyVariables.Queries.GetFuzzyVariablesBySystemQuery import GetFuzzyVariablesBySystemQuery
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId


class GetFuzzyVariablesBySystemHandler(QueryHandler[GetFuzzyVariablesBySystemQuery, List[FuzzyVariableDto]]):
    async def __call__(self, request: GetFuzzyVariablesBySystemQuery) -> List[FuzzyVariableDto]:  # type: ignore[override]
        repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]
        system_id = FuzzySystemId(request.system_id)

        if request.role == "input":
            variables = await repo.get_input_variables_by_system(system_id)
        elif request.role == "output":
            variables = await repo.get_output_variables_by_system(system_id)
        else:
            variables = await repo.get_by_system_id(system_id, skip=request.skip, limit=request.limit)

        return [FuzzyVariableDto.from_entity(v) for v in variables]
