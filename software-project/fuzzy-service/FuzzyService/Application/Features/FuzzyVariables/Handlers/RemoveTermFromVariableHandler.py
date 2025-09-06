from __future__ import annotations

from kink import di
from medyator import CommandHandler

from FuzzyService.Application.Features.FuzzyVariables.Commands.RemoveTermFromVariableCommand import RemoveTermFromVariableCommand
from FuzzyService.Application.Features.FuzzyVariables.DTOs.FuzzyVariableDto import FuzzyVariableDto
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyVariableId, FuzzyTermId
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError, BusinessRuleViolationError


class RemoveTermFromVariableHandler(CommandHandler[RemoveTermFromVariableCommand]):
    """Handler para remover un término de una variable difusa."""

    async def __call__(self, request: RemoveTermFromVariableCommand) -> None:
        variable_repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]
        term_repo: IFuzzyTermRepository = di[IFuzzyTermRepository]
        rule_repo: IFuzzyRuleRepository = di[IFuzzyRuleRepository]
        
        variable_id = FuzzyVariableId(request.variable_id)
        term_id = FuzzyTermId(request.term_id)

        # Verificar que la variable existe
        variable = await variable_repo.get_by_id(variable_id)
        if not variable:
            raise EntityNotFoundError("Variable no encontrada")

        # Verificar que el término existe
        term = await term_repo.get_by_id(term_id)
        if not term:
            raise EntityNotFoundError("Término no encontrado")

        # Verificar que el término está asociado a esta variable
        if not variable.has_term(term_id):
            raise ValueError("El término no está asociado a esta variable")

        # Verificar que el término no está siendo usado en reglas
        rules_using_variable = await rule_repo.get_rules_using_variable(variable_id)
        for rule in rules_using_variable:
            for condition in rule.conditions:
                if condition.get("value") == term.label:
                    raise BusinessRuleViolationError(
                        f"No se puede eliminar el término '{term.label}' porque está siendo usado en la regla '{rule.name}'"
                    )

        # Remover el término de la variable
        variable.remove_term(term_id)
        
        # Eliminar completamente el término (no solo desasociar)
        if term.variable_id == variable_id:
            await term_repo.delete(term_id)

        # Actualizar la variable
        updated_variable = await variable_repo.update(variable)
        request._result = FuzzyVariableDto.from_entity(updated_variable)