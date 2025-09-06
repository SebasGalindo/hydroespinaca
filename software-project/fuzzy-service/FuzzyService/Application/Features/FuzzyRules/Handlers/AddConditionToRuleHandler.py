from __future__ import annotations

from kink import di
from medyator import CommandHandler

from FuzzyService.Application.Features.FuzzyRules.Commands.AddConditionToRuleCommand import AddConditionToRuleCommand
from FuzzyService.Application.Features.FuzzyRules.DTOs.FuzzyRuleDto import FuzzyRuleDto
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyRuleId, FuzzyVariableId
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError, BusinessRuleViolationError


class AddConditionToRuleHandler(CommandHandler[AddConditionToRuleCommand]):
    """Handler para agregar una condición a una regla difusa existente."""
    
    async def __call__(self, request: AddConditionToRuleCommand) -> None:
        # 1. Obtener repositorios
        rule_repo: IFuzzyRuleRepository = di[IFuzzyRuleRepository]
        variable_repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]
        system_repo: IFuzzySystemRepository = di[IFuzzySystemRepository]
        
        # 2. Verificar que la regla existe
        rule_id = FuzzyRuleId(request.rule_id)
        existing_rule = await rule_repo.get_by_id(rule_id)
        if not existing_rule:
            raise EntityNotFoundError(f"Regla con ID {request.rule_id} no encontrada")
        
        # 3. Verificar que la variable existe
        variable_id = FuzzyVariableId(request.variable_id)
        variable = await variable_repo.get_by_id(variable_id)
        if not variable:
            raise EntityNotFoundError(f"Variable con ID {request.variable_id} no encontrada")
        
        # 4. Verificar que la variable pertenece al mismo sistema que la regla
        if existing_rule.system_id:
            system = await system_repo.get_by_id(existing_rule.system_id)
            if system and not system.has_variable(variable_id):
                raise BusinessRuleViolationError(
                    f"Variable {request.variable_id} no pertenece al sistema {existing_rule.system_id}"
                )
        
        # 5. Verificar que no existe ya una condición para esta variable
        for condition in existing_rule.conditions:
            if condition.get("variableId") == variable_id:
                raise BusinessRuleViolationError(
                    f"Ya existe una condición para la variable {request.variable_id} en esta regla"
                )
        
        # 6. Validar el conector según el número de condiciones existentes
        if len(existing_rule.conditions) == 0:
            # Primera condición, no debe haber conector
            if request.connector is not None:
                raise BusinessRuleViolationError(
                    "No se debe especificar un conector para la primera condición"
                )
        else:
            # Ya hay condiciones, se requiere conector
            if request.connector is None:
                raise BusinessRuleViolationError(
                    "Se debe especificar un conector para unir con las condiciones existentes"
                )
        
        # 7. Agregar la condición usando el método de dominio
        existing_rule.add_condition(
            variable_id=variable_id,
            operator=request.operator,
            value=request.value,
            connector=request.connector
        )
        
        # 8. Guardar la regla actualizada
        updated_rule = await rule_repo.update(existing_rule)
        
        # 9. Asignar resultado al comando
        request._result = FuzzyRuleDto.from_entity(updated_rule)