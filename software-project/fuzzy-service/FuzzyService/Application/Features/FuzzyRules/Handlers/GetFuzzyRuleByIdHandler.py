from __future__ import annotations

from kink import di
from medyator import QueryHandler

from FuzzyService.Application.Features.FuzzyRules.Queries.GetFuzzyRuleByIdQuery import GetFuzzyRuleByIdQuery
from FuzzyService.Application.Features.FuzzyRules.DTOs.FuzzyRuleDto import FuzzyRuleDto
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyRuleId
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError


class GetFuzzyRuleByIdHandler(QueryHandler[GetFuzzyRuleByIdQuery, FuzzyRuleDto]):
    """Handler para obtener una regla difusa por su ID."""
    
    async def __call__(self, request: GetFuzzyRuleByIdQuery) -> FuzzyRuleDto:  # type: ignore[override]
        """Maneja la consulta de una regla difusa por ID."""
        
        repo: IFuzzyRuleRepository = di[IFuzzyRuleRepository]
        
        rule_id = FuzzyRuleId(request.rule_id)
        rule = await repo.get_by_id(rule_id)
        
        if not rule:
            raise EntityNotFoundError(f"No se encontró la regla con id '{request.rule_id}'")
        
        return FuzzyRuleDto.from_entity(rule)
