from __future__ import annotations

from kink import di
from medyator import CommandHandler

from FuzzyService.Application.Features.FuzzyRules.Commands.UpdateRuleConsequentCommand import UpdateRuleConsequentCommand
from FuzzyService.Application.Features.FuzzyRules.DTOs.FuzzyRuleDto import FuzzyRuleDto
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyRuleId, FuzzyRoutineId
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository
from FuzzyService.Domain.Interfaces.IFuzzyRoutineRepository import IFuzzyRoutineRepository
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError


class UpdateRuleConsequentHandler(CommandHandler[UpdateRuleConsequentCommand]):
    """Handler para actualizar el consecuente de una regla difusa existente."""
    
    async def __call__(self, request: UpdateRuleConsequentCommand) -> None:
        # 1. Obtener repositorios
        rule_repo: IFuzzyRuleRepository = di[IFuzzyRuleRepository]
        routine_repo: IFuzzyRoutineRepository = di[IFuzzyRoutineRepository]
        
        # 2. Verificar que la regla existe
        rule_id = FuzzyRuleId(request.rule_id)
        existing_rule = await rule_repo.get_by_id(rule_id)
        if not existing_rule:
            raise EntityNotFoundError(f"Regla con ID {request.rule_id} no encontrada")
        
        # 3. Verificar que la rutina consecuente existe
        consequent_id = FuzzyRoutineId(request.consequent)
        routine = await routine_repo.get_by_id(consequent_id)
        if not routine:
            raise EntityNotFoundError(f"Rutina con ID {request.consequent} no encontrada")
        
        # 4. Actualizar el consecuente
        existing_rule.consequent = consequent_id
        
        # 5. Guardar la regla actualizada
        updated_rule = await rule_repo.update(existing_rule)
        
        # 6. Asignar resultado al comando
        request._result = FuzzyRuleDto.from_entity(updated_rule)