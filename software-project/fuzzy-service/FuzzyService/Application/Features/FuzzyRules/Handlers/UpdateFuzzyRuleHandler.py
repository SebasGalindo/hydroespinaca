from __future__ import annotations

from typing import TYPE_CHECKING
from medyator import CommandHandler
from kink import di

from FuzzyService.Application.Features.FuzzyRules.Commands.UpdateFuzzyRuleCommand import UpdateFuzzyRuleCommand
from FuzzyService.Application.Features.FuzzyRules.DTOs.FuzzyRuleDto import FuzzyRuleDto
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError, BusinessRuleViolationError
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyVariableId

if TYPE_CHECKING:
    from kink import Container


class UpdateFuzzyRuleHandler(CommandHandler[UpdateFuzzyRuleCommand]):
    """Handler para actualizar una regla difusa existente."""

    async def __call__(self, request: UpdateFuzzyRuleCommand) -> None:
        """Actualiza una regla difusa existente."""
        
        # Validar que rule_id esté presente
        if not request.rule_id:
            raise ValueError("rule_id es requerido para actualizar una regla")
        
        # Obtener repositorios
        rule_repository = di[IFuzzyRuleRepository]
        system_repository = di[IFuzzySystemRepository]
        variable_repository = di[IFuzzyVariableRepository]

        # Verificar que la regla existe
        existing_rule = await rule_repository.get_by_id(request.rule_id)
        if not existing_rule:
            raise EntityNotFoundError(f"Regla con ID {request.rule_id} no encontrada")

        # Actualizar campos si se proporcionan
        if request.name is not None:
            existing_rule.name = request.name

        if request.description is not None:
            existing_rule.update_description(request.description)

        # Actualizar condiciones y conectores si se proporcionan
        if request.conditions is not None:
            # Verificar que todas las variables existen y pertenecen al sistema
            system = await system_repository.get_by_id(existing_rule.system_id)
            if not system:
                raise EntityNotFoundError(f"Sistema con ID {existing_rule.system_id} no encontrado")

            for condition_dto in request.conditions:
                variable = await variable_repository.get_by_id(condition_dto.variable_id)
                if not variable:
                    raise EntityNotFoundError(f"Variable con ID {condition_dto.variable_id} no encontrada")

                # Verificar que la variable pertenece al sistema
                variable_id = FuzzyVariableId(condition_dto.variable_id)
                if not system.has_variable(variable_id):
                    raise BusinessRuleViolationError(
                        f"Variable {condition_dto.variable_id} no pertenece al sistema {existing_rule.system_id}"
                    )

            # Limpiar condiciones existentes y agregar las nuevas
            existing_rule.conditions = []
            for condition_dto in request.conditions:
                existing_rule.add_condition(
                    variable_id=condition_dto.variable_id,
                    operator=condition_dto.operator,
                    value=condition_dto.value
                )

        # Actualizar conectores si se proporcionan
        if request.connectors is not None:
            existing_rule.set_connectors(request.connectors)

        # NOTA: El campo 'consequent' (rutina) fue eliminado.
        # Para actualizar consecuentes, usar UpdateRuleConsequentHandler
        if hasattr(request, 'consequent') and request.consequent is not None:
            raise BusinessRuleViolationError(
                "El campo 'consequent' (rutina) ya no está soportado. "
                "Use UpdateRuleConsequentCommand para actualizar consecuentes directos."
            )
        
        # Guardar la regla actualizada
        updated_rule = await rule_repository.update(existing_rule)
        
        # Asignar resultado
        request._result = FuzzyRuleDto.from_entity(updated_rule)
