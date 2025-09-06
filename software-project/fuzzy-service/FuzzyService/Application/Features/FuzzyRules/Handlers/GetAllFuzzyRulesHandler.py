from __future__ import annotations

from typing import List
from kink import di
from medyator import QueryHandler

from FuzzyService.Application.Features.FuzzyRules.Queries.GetAllFuzzyRulesQuery import GetAllFuzzyRulesQuery
from FuzzyService.Application.Features.FuzzyRules.DTOs.FuzzyRuleDto import FuzzyRuleDto
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository


class GetAllFuzzyRulesHandler(QueryHandler[GetAllFuzzyRulesQuery, List[FuzzyRuleDto]]):
    """Handler para obtener todas las reglas difusas con filtros opcionales."""
    
    async def __call__(self, request: GetAllFuzzyRulesQuery) -> List[FuzzyRuleDto]:  # type: ignore[override]
        """Maneja la consulta de todas las reglas difusas."""
        
        repo: IFuzzyRuleRepository = di[IFuzzyRuleRepository]
        
        if request.system_id:
            # Filtrar por sistema específico
            system_id = FuzzySystemId(request.system_id)
            rules = await repo.get_by_system_id(system_id)
        else:
            # Obtener todas las reglas
            rules = await repo.get_all()
        
        # Aplicar paginación
        paginated_rules = rules[request.skip:request.skip + request.limit]
        
        return [FuzzyRuleDto.from_entity(rule) for rule in paginated_rules]
