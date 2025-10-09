from __future__ import annotations

from medyator import CommandHandler
from kink import di

from FuzzyService.Application.Features.FuzzySystems.DTOs.FuzzySystemDto import FuzzySystemDto
from FuzzyService.Application.Features.FuzzySystems.Commands.CreateFuzzySystem.CreateFuzzySystemCommand import CreateFuzzySystemCommand
from FuzzyService.Domain.Entities.fuzzy_system import FuzzySystem
from FuzzyService.Domain.Enums import FuzzySystemStatus
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository


class CreateFuzzySystemHandler(CommandHandler[CreateFuzzySystemCommand]):
    """Handler del comando CreateFuzzySystemCommand."""

    async def __call__(self, request: CreateFuzzySystemCommand) -> None:
        # Resolver repositorio desde el contenedor DI en tiempo de ejecución
        repo: IFuzzySystemRepository = di[IFuzzySystemRepository]

        # Determinar estado inicial (entrada ya validada por Pydantic)
        status = FuzzySystemStatus.ACTIVE if request.isActive else FuzzySystemStatus.DRAFT

        # Construir entidad de dominio (la entidad aplica sus propias invariantes)
        entity = FuzzySystem(
            name=request.name,
            status=status,
        )

        # Persistir y mapear a DTO (repositorio mapea DuplicateKeyError -> DuplicateEntityError)
        created = await repo.create(entity)
        
        mapped_fuzzy_system = FuzzySystemDto.from_entity(created)
        
        # Asignar el resultado al campo result del Command
        request._result = mapped_fuzzy_system
