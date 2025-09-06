from __future__ import annotations

from medyator import CommandHandler
from kink import di

from FuzzyService.Application.Features.FuzzySystems.Commands.DeleteFuzzySystem.DeleteFuzzySystemCommand import DeleteFuzzySystemCommand
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError


class DeleteFuzzySystemHandler(CommandHandler[DeleteFuzzySystemCommand]):
    """Handler para el comando de eliminación de un sistema difuso."""

    async def __call__(self, request: DeleteFuzzySystemCommand) -> None:
        repo: IFuzzySystemRepository = di[IFuzzySystemRepository]
        system_id = FuzzySystemId(request.id)

        success = await repo.delete(system_id)
        if not success:
            raise EntityNotFoundError("Sistema no encontrado")
        
        # Asignar el resultado al campo result del Command
        request._result = success
