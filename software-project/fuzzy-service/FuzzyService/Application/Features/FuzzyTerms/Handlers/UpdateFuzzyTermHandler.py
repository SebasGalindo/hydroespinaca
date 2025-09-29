from __future__ import annotations

from kink import di
from medyator import CommandHandler

from FuzzyService.Application.Features.FuzzyTerms.Commands.UpdateFuzzyTermCommand import UpdateFuzzyTermCommand
from FuzzyService.Application.Features.FuzzyTerms.DTOs.FuzzyTermDto import FuzzyTermDto
from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyTermId
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError


class UpdateFuzzyTermHandler(CommandHandler[UpdateFuzzyTermCommand]):
    """Handler para actualizar un término difuso existente."""

    async def __call__(self, request: UpdateFuzzyTermCommand) -> None:
        repo: IFuzzyTermRepository = di[IFuzzyTermRepository]
        term_id = FuzzyTermId(request.id)

        entity = await repo.get_by_id(term_id)
        if entity is None:
            raise EntityNotFoundError("Término no encontrado")

        # Aplicar cambios según lo enviado en el comando
        if request.label is not None and request.label != entity.label:
            entity.label = request.label
        if request.membership_function is not None:
            entity.membership_function = request.membership_function.to_value_object()

        updated = await repo.update(entity)
        request._result = FuzzyTermDto.from_entity(updated)
