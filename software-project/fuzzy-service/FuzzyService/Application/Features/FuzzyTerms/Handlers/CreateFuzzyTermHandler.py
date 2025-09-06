from __future__ import annotations

from kink import di
from medyator import CommandHandler

from FuzzyService.Application.Features.FuzzyTerms.Commands.CreateFuzzyTermCommand import CreateFuzzyTermCommand
from FuzzyService.Application.Features.FuzzyTerms.DTOs.FuzzyTermDto import FuzzyTermDto
from FuzzyService.Domain.Entities.fuzzy_term import FuzzyTerm
from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyVariableId
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError


class CreateFuzzyTermHandler(CommandHandler[CreateFuzzyTermCommand]):
    """Handler para crear un nuevo término difuso."""

    async def __call__(self, request: CreateFuzzyTermCommand) -> None:
        term_repo: IFuzzyTermRepository = di[IFuzzyTermRepository]
        variable_repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]

        # Verificar que la variable existe
        variable_id = FuzzyVariableId(request.variable_id)
        variable = await variable_repo.get_by_id(variable_id)
        if not variable:
            raise EntityNotFoundError(f"Variable difusa con ID {request.variable_id} no encontrada")

        # Crear el término difuso
        entity = FuzzyTerm(
            variable_id=variable_id,
            label=request.label,
            membership_function=request.membership_function.to_value_object(),
        )

        created = await term_repo.create(entity)
        
        # Agregar el ID del término a la lista de términos de la variable
        variable.add_term(created.id)
        await variable_repo.update(variable)
        
        request._result = FuzzyTermDto.from_entity(created)
