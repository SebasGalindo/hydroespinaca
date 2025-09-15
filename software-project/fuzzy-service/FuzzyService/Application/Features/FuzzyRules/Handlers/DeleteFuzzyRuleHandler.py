from __future__ import annotations

from medyator import CommandHandler
from kink import di

from FuzzyService.Application.Features.FuzzyRules.Commands.DeleteFuzzyRuleCommand import DeleteFuzzyRuleCommand
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError

class DeleteFuzzyRuleHandler(CommandHandler[DeleteFuzzyRuleCommand]):
    """Handler para eliminar una regla difusa."""

    async def __call__(self, request: DeleteFuzzyRuleCommand) -> None:
        """Elimina una regla difusa."""
        
        
        # Obtener repositorios
        rule_repository = di[IFuzzyRuleRepository]
        system_repository = di[IFuzzySystemRepository]
        
        # Verificar que la regla existe
        existing_rule = await rule_repository.get_by_id(request.rule_id)
        if not existing_rule:
            raise EntityNotFoundError(f"Regla con ID {request.rule_id} no encontrada")
        
        # Si la regla tiene system_id, removerla del sistema
        if existing_rule.system_id:
            system = await system_repository.get_by_id(existing_rule.system_id)
            if system:
                system.remove_rule(request.rule_id)
                await system_repository.update(system)
        
        # Eliminar la regla
        success = await rule_repository.delete(request.rule_id)
        
        # Asignar resultado
        request._result = success
