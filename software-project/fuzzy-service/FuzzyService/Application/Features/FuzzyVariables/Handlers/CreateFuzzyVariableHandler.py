from __future__ import annotations

from kink import di
from medyator import CommandHandler

from FuzzyService.Application.Features.FuzzyVariables.Commands.CreateFuzzyVariableCommand import CreateFuzzyVariableCommand
from FuzzyService.Application.Features.FuzzyVariables.DTOs.FuzzyVariableDto import FuzzyVariableDto
from FuzzyService.Domain.Entities.fuzzy_variable import FuzzyVariable
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyTermId, FuzzySystemId
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError


class CreateFuzzyVariableHandler(CommandHandler[CreateFuzzyVariableCommand]):
    """Handler para crear una nueva variable difusa."""

    async def __call__(self, request: CreateFuzzyVariableCommand) -> None:
        variable_repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]
        system_repo: IFuzzySystemRepository = di[IFuzzySystemRepository]
        
        # Validar que el sistema fuzzy existe
        system_id = FuzzySystemId(request.system_id)
        system = await system_repo.get_by_id(system_id)
        if not system:
            raise EntityNotFoundError(f"Sistema fuzzy con ID {request.system_id} no encontrado")

        # Crear la variable
        entity = FuzzyVariable(
            name=request.name,
            description=request.description or "",
            variable_type=request.variable_type,
            device_id=request.device_id,
            terms=[FuzzyTermId(t) for t in (request.terms or [])],
        )

        created = await variable_repo.create(entity)
        
        # Agregar la variable al sistema fuzzy según su tipo
        if request.variable_type == "input":
            system.add_input_variable(created.id)
        else:  # output
            system.add_output_variable(created.id)
        
        # Actualizar el sistema
        await system_repo.update(system)
        
        request._result = FuzzyVariableDto.from_entity(created)
