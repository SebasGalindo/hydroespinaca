from __future__ import annotations

from kink import di
from medyator import CommandHandler

from FuzzyService.Application.Features.FuzzyVariables.Commands.UpdateFuzzyVariableCommand import UpdateFuzzyVariableCommand
from FuzzyService.Application.Features.FuzzyVariables.DTOs.FuzzyVariableDto import FuzzyVariableDto
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.Interfaces.ISensorService import ISensorService
from FuzzyService.Domain.Interfaces.IActuatorService import IActuatorService
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyVariableId, FuzzyTermId
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError, InvalidReferenceException


class UpdateFuzzyVariableHandler(CommandHandler[UpdateFuzzyVariableCommand]):
    """Handler para actualizar una variable difusa existente con validación de reference_id."""

    async def __call__(self, request: UpdateFuzzyVariableCommand) -> None:
        repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]
        sensor_service: ISensorService = di[ISensorService]
        actuator_service: IActuatorService = di[IActuatorService]
        var_id = FuzzyVariableId(request.id)

        entity = await repo.get_by_id(var_id)
        if entity is None:
            raise EntityNotFoundError("Variable no encontrada")

        # Determinar el tipo de variable (puede estar siendo actualizado)
        new_variable_type = request.variable_type if request.variable_type is not None else entity.variable_type
        new_reference_id = request.reference_id if request.reference_id is not None else entity.reference_id

        # Si se cambia reference_id o variable_type, validar la nueva referencia
        if request.reference_id is not None or request.variable_type is not None:
            if new_reference_id:
                if new_variable_type == "input":
                    # Validar contra sensor-service mediante GET /api/variables/{id}
                    exists = await sensor_service.validate_variable_exists(new_reference_id)
                    if not exists:
                        raise InvalidReferenceException(
                            reference_id=new_reference_id,
                            service="sensor-service /api/variables/{id}",
                            variable_type=new_variable_type
                        )
                elif new_variable_type == "output":
                    # Validar contra actuator-service mediante GET /api/outputs/{id}
                    exists = await actuator_service.validate_output_exists(new_reference_id)
                    if not exists:
                        raise InvalidReferenceException(
                            reference_id=new_reference_id,
                            service="actuator-service /api/outputs/{id}",
                            variable_type=new_variable_type
                        )

        # Aplicar cambios según lo enviado en el comando
        if request.name is not None and request.name != entity.name:
            entity.name = request.name
        if request.description is not None:
            entity.description = request.description
        if request.variable_type is not None:
            entity.variable_type = request.variable_type
        if request.reference_id is not None:
            entity.reference_id = request.reference_id
        if request.terms is not None:
            entity.terms = [FuzzyTermId(t) for t in request.terms]

        updated = await repo.update(entity)
        request._result = FuzzyVariableDto.from_entity(updated)
