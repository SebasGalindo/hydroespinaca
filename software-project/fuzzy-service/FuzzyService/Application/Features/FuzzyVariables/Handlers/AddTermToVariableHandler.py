from __future__ import annotations

from kink import di
from medyator import CommandHandler

from FuzzyService.Application.Features.FuzzyVariables.Commands.AddTermToVariableCommand import AddTermToVariableCommand
from FuzzyService.Application.Features.FuzzyVariables.DTOs.FuzzyVariableDto import FuzzyVariableDto
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyVariableId, FuzzyTermId
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError


class AddTermToVariableHandler(CommandHandler[AddTermToVariableCommand]):
    """Handler para agregar un término a una variable difusa."""

    async def __call__(self, request: AddTermToVariableCommand) -> None:
        variable_repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]
        term_repo: IFuzzyTermRepository = di[IFuzzyTermRepository]
        
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

        # Verificar que el término no esté ya asociado a otra variable
        if term.variable_id and term.variable_id != variable_id:
            raise ValueError(f"El término ya está asociado a otra variable: {term.variable_id}")

        # Agregar el término a la variable
        variable.add_term(term_id)
        
        # Si el término no tenía variable_id, asignársela
        if not term.variable_id:
            term.variable_id = variable_id
            await term_repo.update(term)

        # Actualizar la variable
        updated_variable = await variable_repo.update(variable)
        request._result = FuzzyVariableDto.from_entity(updated_variable)