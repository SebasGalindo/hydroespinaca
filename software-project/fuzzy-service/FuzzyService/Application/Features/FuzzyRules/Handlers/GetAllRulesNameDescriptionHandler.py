from __future__ import annotations

from typing import List, Dict, Any
from kink import di
from medyator import QueryHandler

from FuzzyService.Application.Features.FuzzyRules.Queries.GetAllRulesNameDescriptionQuery import GetAllRulesNameDescriptionQuery
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository


class GetAllRulesNameDescriptionHandler(QueryHandler[GetAllRulesNameDescriptionQuery, List[Dict[str, Any]]]):
    """Handler para obtener todas las reglas difusas con solo id, nombre y descripción."""
    
    async def __call__(self, request: GetAllRulesNameDescriptionQuery) -> List[Dict[str, Any]]:  # type: ignore[override]
        """Maneja la consulta de reglas simplificadas."""
        
        repo: IFuzzyRuleRepository = di[IFuzzyRuleRepository]
        
        # Obtener reglas simplificadas desde el repositorio
        rules = await repo.get_all_rules_name_description(
            skip=request.skip,
            limit=request.limit
        )
        
        request._result = rules
        return rules
