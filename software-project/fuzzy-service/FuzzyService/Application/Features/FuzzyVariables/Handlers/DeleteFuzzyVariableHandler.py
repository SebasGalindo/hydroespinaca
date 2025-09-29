from __future__ import annotations

from kink import di
from medyator import CommandHandler

from FuzzyService.Application.Features.FuzzyVariables.Commands.DeleteFuzzyVariableCommand import DeleteFuzzyVariableCommand
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyVariableId
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError


class DeleteFuzzyVariableHandler(CommandHandler[DeleteFuzzyVariableCommand]):
    """Handler para eliminar una variable difusa por Id."""

    async def __call__(self, request: DeleteFuzzyVariableCommand) -> None:
        variable_repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]
        system_repo: IFuzzySystemRepository = di[IFuzzySystemRepository]
        term_repo: IFuzzyTermRepository = di[IFuzzyTermRepository]
        var_id = FuzzyVariableId(request.id)

        # Verificar que la variable existe antes de proceder
        variable = await variable_repo.get_by_id(var_id)
        if not variable:
            raise EntityNotFoundError("Variable no encontrada")

        # Obtener sistemas que referencian esta variable
        input_systems = await system_repo.get_systems_with_input_variable(var_id)
        output_systems = await system_repo.get_systems_with_output_variable(var_id)
        
        # Remover la variable de los sistemas donde aparece como entrada
        for system in input_systems:
            system.remove_input_variable(var_id)
            await system_repo.update(system)
        
        # Remover la variable de los sistemas donde aparece como salida
        for system in output_systems:
            system.remove_output_variable(var_id)
            await system_repo.update(system)

        # Eliminar todos los términos asociados a la variable
        terms = await term_repo.get_by_variable_id(var_id)
        for term in terms:
            # Primero remover el término de la variable
            variable.remove_term(term.id)
            await variable_repo.update(variable)
            # Luego eliminar el término
            await term_repo.delete(term.id)

        # Ahora eliminar la variable
        success = await variable_repo.delete(var_id)
        if not success:
            raise EntityNotFoundError("Variable no encontrada")

        request._result = success
