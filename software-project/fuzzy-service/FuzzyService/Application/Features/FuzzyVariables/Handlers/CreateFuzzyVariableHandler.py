from __future__ import annotations

from kink import di
from medyator import CommandHandler

from FuzzyService.Application.Features.FuzzyVariables.Commands.CreateFuzzyVariableCommand import CreateFuzzyVariableCommand
from FuzzyService.Application.Features.FuzzyVariables.DTOs.FuzzyVariableDto import FuzzyVariableDto
from FuzzyService.Domain.Entities.fuzzy_variable import FuzzyVariable
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyTermId


class CreateFuzzyVariableHandler(CommandHandler[CreateFuzzyVariableCommand]):
    """Handler para crear una nueva variable difusa."""

    async def __call__(self, request: CreateFuzzyVariableCommand) -> None:
        repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]

        entity = FuzzyVariable(
            name=request.name,
            description=request.description or "",
            variable_type=request.variable_type,
            device_id=request.device_id,
            terms=[FuzzyTermId(t) for t in (request.terms or [])],
        )

        created = await repo.create(entity)
        request._result = FuzzyVariableDto.from_entity(created)
