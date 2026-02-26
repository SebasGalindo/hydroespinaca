from __future__ import annotations

from typing import TYPE_CHECKING, List
from medyator import QueryHandler
from kink import inject

from FuzzyService.Application.Features.FuzzyRules.Queries.GetFuzzyRulesBySystemQuery import GetFuzzyRulesBySystemQuery
from FuzzyService.Application.Features.FuzzyRules.DTOs.FuzzyRuleDto import FuzzyRuleDto
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError

if TYPE_CHECKING:
    from kink import Container


class GetFuzzyRulesBySystemHandler(QueryHandler[GetFuzzyRulesBySystemQuery, List[FuzzyRuleDto]]):
    """Handler para obtener todas las reglas difusas de un sistema específico."""

    async def __call__(self, request: GetFuzzyRulesBySystemQuery) -> List[FuzzyRuleDto]:
        """Obtiene todas las reglas difusas de un sistema específico."""
        from kink import di
        
        # Obtener repositorios
        rule_repository = di[IFuzzyRuleRepository]
        system_repository = di[IFuzzySystemRepository]
        
        # Verificar que el sistema existe
        system = await system_repository.get_by_id(request.system_id)
        if not system:
            raise EntityNotFoundError(f"Sistema con ID {request.system_id} no encontrado")
        
        # Obtener reglas del sistema con paginación
        rules = await rule_repository.get_by_system_id(
            system_id=request.system_id,
            skip=request.skip,
            limit=request.limit
        )
        
        # Convertir a DTOs
        rule_dtos = [FuzzyRuleDto.from_entity(rule) for rule in rules]
        
        # Asignar resultado y retornar
        request._result = rule_dtos
        return rule_dtos
