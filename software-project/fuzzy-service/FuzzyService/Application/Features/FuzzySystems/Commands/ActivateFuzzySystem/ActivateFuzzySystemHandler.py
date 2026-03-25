from __future__ import annotations

import logging

from medyator import CommandHandler
from kink import di

from FuzzyService.Application.Features.FuzzySystems.DTOs.FuzzySystemDto import FuzzySystemDto
from FuzzyService.Application.Features.FuzzySystems.Commands.ActivateFuzzySystem.ActivateFuzzySystemCommand import ActivateFuzzySystemCommand
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId
from FuzzyService.Domain.Enums.EntityStatus import FuzzySystemStatus
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError, BusinessRuleViolationError

_logger = logging.getLogger(__name__)


class ActivateFuzzySystemHandler(CommandHandler[ActivateFuzzySystemCommand]):
    """Handler del comando ActivateFuzzySystemCommand.
    
    Lógica de activación exclusiva:
    1. Obtener el sistema por ID (404 si no existe).
    2. Si ya está ACTIVE → no-op, retornar el sistema tal cual.
    3. Validar que el sistema tiene variables de entrada, salida y reglas.
    4. Desactivar todos los sistemas ACTIVE → INACTIVE.
    5. Activar el sistema indicado → ACTIVE.
    6. Retornar el sistema actualizado.
    """

    async def __call__(self, request: ActivateFuzzySystemCommand) -> None:
        repo: IFuzzySystemRepository = di[IFuzzySystemRepository]
        system_id = FuzzySystemId(request.id)

        # 1. Obtener el sistema
        system = await repo.get_by_id(system_id)
        if system is None:
            raise EntityNotFoundError(f"Sistema difuso con id '{request.id}' no encontrado")

        # 2. Si ya está activo → no-op
        if system.status == FuzzySystemStatus.ACTIVE:
            _logger.info("Sistema '%s' ya está activo, no-op", system.name)
            request._result = FuzzySystemDto.from_entity(system)
            return

        # 3. Validar que el sistema puede ser activado
        if not system.input_variable_ids:
            raise BusinessRuleViolationError(
                "El sistema debe tener al menos una variable de entrada para ser activado"
            )
        if not system.output_variable_ids:
            raise BusinessRuleViolationError(
                "El sistema debe tener al menos una variable de salida para ser activado"
            )
        if not system.rule_ids:
            raise BusinessRuleViolationError(
                "El sistema debe tener al menos una regla para ser activado"
            )

        # 4. Desactivar todos los sistemas activos
        deactivated_count = await repo.deactivate_all_active()
        _logger.info("Desactivados %d sistema(s) activo(s) antes de activar '%s'", deactivated_count, system.name)

        # 5. Activar el sistema indicado
        updated = await repo.update_status(system_id, FuzzySystemStatus.ACTIVE)
        _logger.info("Sistema '%s' activado exitosamente", updated.name)

        # 6. Retornar el sistema actualizado
        request._result = FuzzySystemDto.from_entity(updated)
