from __future__ import annotations

from kink import di
from medyator import CommandHandler

from FuzzyService.Application.Features.FuzzyRules.Commands.RemoveConditionFromRuleCommand import RemoveConditionFromRuleCommand
from FuzzyService.Application.Features.FuzzyRules.DTOs.FuzzyRuleDto import FuzzyRuleDto
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyRuleId, FuzzyVariableId
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError, BusinessRuleViolationError


class RemoveConditionFromRuleHandler(CommandHandler[RemoveConditionFromRuleCommand]):
    """Handler para remover una condición de una regla difusa existente."""
    
    async def __call__(self, request: RemoveConditionFromRuleCommand) -> None:
        # 1. Obtener repositorio
        rule_repo: IFuzzyRuleRepository = di[IFuzzyRuleRepository]
        
        # 2. Verificar que la regla existe
        rule_id = FuzzyRuleId(request.rule_id)
        existing_rule = await rule_repo.get_by_id(rule_id)
        if not existing_rule:
            raise EntityNotFoundError(f"Regla con ID {request.rule_id} no encontrada")
        
        # 3. Verificar que la regla tiene más de una condición
        if len(existing_rule.conditions) <= 1:
            raise BusinessRuleViolationError(
                "No se puede eliminar la condición. Una regla debe tener al menos una condición"
            )
        
        # 4. Verificar que existe una condición para la variable especificada
        variable_id = FuzzyVariableId(request.variable_id)
        condition_found = False
        for condition in existing_rule.conditions:
            if condition.get("variableId") == variable_id:
                condition_found = True
                break
        
        if not condition_found:
            raise EntityNotFoundError(
                f"No se encontró una condición para la variable {request.variable_id} en esta regla"
            )
        
        # 5. Remover la condición usando el método de dominio
        existing_rule.remove_condition(variable_id)
        
        # 6. Guardar la regla actualizada
        updated_rule = await rule_repo.update(existing_rule)
        
        # 7. Asignar resultado al comando
        request._result = FuzzyRuleDto.from_entity(updated_rule)