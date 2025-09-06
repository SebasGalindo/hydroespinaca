from __future__ import annotations

from kink import di
from medyator import CommandHandler

from FuzzyService.Application.Features.FuzzyVariables.Commands.UpdateFuzzyVariableCommand import UpdateFuzzyVariableCommand
from FuzzyService.Application.Features.FuzzyVariables.DTOs.FuzzyVariableDto import FuzzyVariableDto
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyVariableId, FuzzyTermId
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError


class UpdateFuzzyVariableHandler(CommandHandler[UpdateFuzzyVariableCommand]):
    """Handler para actualizar una variable difusa existente."""

    async def __call__(self, request: UpdateFuzzyVariableCommand) -> None:
        repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]
        var_id = FuzzyVariableId(request.id)

        entity = await repo.get_by_id(var_id)
        if entity is None:
            raise EntityNotFoundError("Variable no encontrada")

        # Aplicar cambios según lo enviado en el comando
        if request.name is not None and request.name != entity.name:
            entity.name = request.name
        if request.description is not None:
            entity.description = request.description
        if request.variable_type is not None:
            entity.variable_type = request.variable_type
        if request.device_id is not None:
            entity.device_id = request.device_id
        if request.terms is not None:
            entity.terms = [FuzzyTermId(t) for t in request.terms]

        updated = await repo.update(entity)
        request._result = FuzzyVariableDto.from_entity(updated)
