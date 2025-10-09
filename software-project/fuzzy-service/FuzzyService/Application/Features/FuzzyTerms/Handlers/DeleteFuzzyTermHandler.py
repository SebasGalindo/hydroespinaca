from __future__ import annotations

from kink import di
from medyator import CommandHandler

from FuzzyService.Application.Features.FuzzyTerms.Commands.DeleteFuzzyTermCommand import DeleteFuzzyTermCommand
from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyTermId
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError


class DeleteFuzzyTermHandler(CommandHandler[DeleteFuzzyTermCommand]):
    """Handler para eliminar un término difuso por Id."""

    async def __call__(self, request: DeleteFuzzyTermCommand) -> None:
        term_repo: IFuzzyTermRepository = di[IFuzzyTermRepository]
        variable_repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]
        term_id = FuzzyTermId(request.id)

        # Obtener el término antes de eliminarlo para conocer su variable_id
        term = await term_repo.get_by_id(term_id)
        if not term:
            raise EntityNotFoundError("Término no encontrado")

        # Primero remover el ID del término de la lista de términos de la variable
        if term.variable_id:
            variable = await variable_repo.get_by_id(term.variable_id)
            if variable:
                variable.remove_term(term_id)
                await variable_repo.update(variable)

        # Luego eliminar el término
        success = await term_repo.delete(term_id)
        if not success:
            raise EntityNotFoundError("Término no encontrado")

        request._result = success
