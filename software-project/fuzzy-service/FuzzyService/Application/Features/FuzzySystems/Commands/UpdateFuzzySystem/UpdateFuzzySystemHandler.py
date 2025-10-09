from __future__ import annotations

from medyator import CommandHandler
from kink import di

from FuzzyService.Application.Features.FuzzySystems.DTOs.FuzzySystemDto import FuzzySystemDto
from FuzzyService.Application.Features.FuzzySystems.Commands.UpdateFuzzySystem.UpdateFuzzySystemCommand import UpdateFuzzySystemCommand
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId, FuzzyVariableId
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError


class UpdateFuzzySystemHandler(CommandHandler[UpdateFuzzySystemCommand]):
    """Handler del comando UpdateFuzzySystemCommand."""

    async def __call__(self, request: UpdateFuzzySystemCommand) -> None:
        repo: IFuzzySystemRepository = di[IFuzzySystemRepository]
        
        if not request.id:
            raise ValueError("ID del sistema es requerido")
            
        system_id = FuzzySystemId(request.id)

        # Cargar el sistema para asegurar existencia y aplicar cambios
        system = await repo.get_by_id(system_id)
        if system is None:
            raise EntityNotFoundError("Sistema no encontrado")

        # Aplicar cambios (validaciones de entrada ya hechas por Pydantic)
        if request.name is not None and request.name != system.name:
            system.name = request.name
        if request.status is not None:
            system.status = request.status
        if request.defuzzification_method is not None:
            system.defuzzification_method = request.defuzzification_method
        if request.operators is not None:
            system.operators = request.operators
        if request.input_variable_ids is not None:
            system.input_variable_ids = [FuzzyVariableId(vid) for vid in request.input_variable_ids]
        if request.output_variable_ids is not None:
            system.output_variable_ids = [FuzzyVariableId(vid) for vid in request.output_variable_ids]

        # Persistir cambios (repositorio valida referencias y unicidad por índice único)
        updated = await repo.update(system)
        mapped_fuzzy_system = FuzzySystemDto.from_entity(updated)
        
        # Asignar el resultado al campo result del Command
        request._result = mapped_fuzzy_system
