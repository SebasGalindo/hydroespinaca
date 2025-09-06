from __future__ import annotations

from medyator import CommandHandler
from kink import di

from FuzzyService.Application.Features.FuzzySystems.DTOs.FuzzySystemDto import FuzzySystemDto
from FuzzyService.Application.Features.FuzzySystems.Commands.UpdateFuzzySystemStatus.UpdateFuzzySystemStatusCommand import UpdateFuzzySystemStatusCommand
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository


class UpdateFuzzySystemStatusHandler(CommandHandler[UpdateFuzzySystemStatusCommand]):
    """Handler del comando UpdateFuzzySystemStatusCommand."""

    async def __call__(self, request: UpdateFuzzySystemStatusCommand) -> None:
        repo: IFuzzySystemRepository = di[IFuzzySystemRepository]
        system_id = FuzzySystemId(request.id)

        # Actualizar estado usando el método específico del repositorio
        updated = await repo.update_status(system_id, request.status)
        mapped_fuzzy_system = FuzzySystemDto.from_entity(updated)
        
        # Asignar el resultado al campo result del Command
        request._result = mapped_fuzzy_system
