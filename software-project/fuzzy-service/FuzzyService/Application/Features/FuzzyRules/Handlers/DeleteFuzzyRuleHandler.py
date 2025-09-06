from __future__ import annotations

from typing import TYPE_CHECKING
from medyator import CommandHandler
from kink import inject

from FuzzyService.Application.Features.FuzzyRules.Commands.DeleteFuzzyRuleCommand import DeleteFuzzyRuleCommand
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError

if TYPE_CHECKING:
    from kink import Container


class DeleteFuzzyRuleHandler(CommandHandler[DeleteFuzzyRuleCommand]):
    """Handler para eliminar una regla difusa."""

    async def __call__(self, request: DeleteFuzzyRuleCommand) -> None:
        """Elimina una regla difusa."""
        from kink import di
        
        # Obtener repositorio
        rule_repository = di[IFuzzyRuleRepository]
        
        # Verificar que la regla existe
        existing_rule = await rule_repository.get_by_id(request.rule_id)
        if not existing_rule:
            raise EntityNotFoundError(f"Regla con ID {request.rule_id} no encontrada")
        
        # Eliminar la regla
        success = await rule_repository.delete(request.rule_id)
        
        # Asignar resultado
        request._result = success
