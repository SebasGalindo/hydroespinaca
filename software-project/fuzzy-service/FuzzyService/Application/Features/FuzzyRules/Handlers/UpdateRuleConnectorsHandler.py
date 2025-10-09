from __future__ import annotations

from kink import di
from medyator import CommandHandler

from FuzzyService.Application.Features.FuzzyRules.Commands.UpdateRuleConnectorsCommand import UpdateRuleConnectorsCommand
from FuzzyService.Application.Features.FuzzyRules.DTOs.FuzzyRuleDto import FuzzyRuleDto
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyRuleId
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError, BusinessRuleViolationError


class UpdateRuleConnectorsHandler(CommandHandler[UpdateRuleConnectorsCommand]):
    """Handler para actualizar los conectores de una regla difusa existente."""
    
    async def __call__(self, request: UpdateRuleConnectorsCommand) -> None:
        # 1. Obtener repositorio
        rule_repo: IFuzzyRuleRepository = di[IFuzzyRuleRepository]
        
        # 2. Verificar que la regla existe
        rule_id = FuzzyRuleId(request.rule_id)
        existing_rule = await rule_repo.get_by_id(rule_id)
        if not existing_rule:
            raise EntityNotFoundError(f"Regla con ID {request.rule_id} no encontrada")
        
        # 3. Validar cardinalidad de conectores
        conditions_count = len(existing_rule.conditions)
        expected_connectors = max(0, conditions_count - 1)
        
        if len(request.connectors) != expected_connectors:
            raise BusinessRuleViolationError(
                f"Número incorrecto de conectores. Para {conditions_count} condiciones se requieren {expected_connectors} conectores, pero se proporcionaron {len(request.connectors)}"
            )
        
        # 4. Actualizar conectores usando el método de dominio
        existing_rule.set_connectors(request.connectors)
        
        # 5. Guardar la regla actualizada
        updated_rule = await rule_repo.update(existing_rule)
        
        # 6. Asignar resultado al comando
        request._result = FuzzyRuleDto.from_entity(updated_rule)