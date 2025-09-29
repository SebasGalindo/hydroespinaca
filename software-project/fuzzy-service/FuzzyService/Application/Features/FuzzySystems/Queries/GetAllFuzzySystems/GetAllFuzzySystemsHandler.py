from __future__ import annotations

from typing import List
from kink import di
from medyator import QueryHandler

from FuzzyService.Application.Features.FuzzySystems.DTOs.FuzzySystemDto import FuzzySystemDto
from FuzzyService.Application.Features.FuzzySystems.Queries.GetAllFuzzySystems.GetAllFuzzySystemsQuery import GetAllFuzzySystemsQuery
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Enums import FuzzySystemStatus


class GetAllFuzzySystemsHandler(QueryHandler[GetAllFuzzySystemsQuery, List[FuzzySystemDto]]):
    async def __call__(self, request: GetAllFuzzySystemsQuery) -> List[FuzzySystemDto]:  # type: ignore[override]
        repo: IFuzzySystemRepository = di[IFuzzySystemRepository]

        filters = {}
        if request.isActive is not None:
            filters["status"] = FuzzySystemStatus.ACTIVE if request.isActive else FuzzySystemStatus.INACTIVE
        if request.name_contains:
            filters["name_contains"] = request.name_contains
        if request.created_after:
            filters["created_after"] = request.created_after
        if request.created_before:
            filters["created_before"] = request.created_before
        if request.order_by_created_at_desc:
            filters["order_by"] = {"created_at": -1}
        else:
            filters["order_by"] = {"created_at": 1}

        systems = await repo.filter_systems(filters, skip=request.skip, limit=request.limit)
        return [FuzzySystemDto.from_entity(s) for s in systems]
