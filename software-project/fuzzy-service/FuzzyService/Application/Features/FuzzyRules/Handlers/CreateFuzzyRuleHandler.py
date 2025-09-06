from __future__ import annotations

from typing import Optional
from kink import di
from medyator import CommandHandler
from datetime import datetime, timezone

from FuzzyService.Application.Features.FuzzyRules.Commands.CreateFuzzyRuleCommand import CreateFuzzyRuleCommand
from FuzzyService.Application.Features.FuzzyRules.DTOs.FuzzyRuleDto import FuzzyRuleDto
from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyRuleId, FuzzySystemId, FuzzyVariableId, FuzzyRoutineId
from FuzzyService.Domain.Enums import LogicalOperator, RuleConnector
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.Interfaces.IFuzzyRoutineRepository import IFuzzyRoutineRepository
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError, BusinessRuleViolationError


class CreateFuzzyRuleHandler(CommandHandler[CreateFuzzyRuleCommand]):
    """Handler para crear una nueva regla difusa."""
    
    async def __call__(self, request: CreateFuzzyRuleCommand) -> None:
        """Maneja la creación de una nueva regla difusa."""
        
        rule_repo: IFuzzyRuleRepository = di[IFuzzyRuleRepository]
        system_repo: IFuzzySystemRepository = di[IFuzzySystemRepository]
        variable_repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]
        
        # 1. Validar que el sistema existe
        system_id = FuzzySystemId(request.system_id)
        system = await system_repo.get_by_id(system_id)
        if not system:
            raise EntityNotFoundError(f"Sistema difuso con ID {request.system_id} no encontrado")
        
        # 2. Validar que todas las variables existen
        variable_ids = [condition.variable_id for condition in request.conditions]
        for var_id_str in variable_ids:
            var_id = FuzzyVariableId(var_id_str)
            variable = await variable_repo.get_by_id(var_id)
            if not variable:
                raise EntityNotFoundError(f"Variable difusa con ID {var_id_str} no encontrada")
        
        # 3. Validar que la rutina consecuente existe (si se proporciona)
        consequent_id = None
        if request.consequent:
            try:
                routine_repo: IFuzzyRoutineRepository = di[IFuzzyRoutineRepository]
                consequent_id = FuzzyRoutineId(request.consequent)
                routine = await routine_repo.get_by_id(consequent_id)
                if not routine:
                    raise EntityNotFoundError(f"Rutina difusa con ID {request.consequent} no encontrada")
            except:
                # Si no hay repositorio de rutinas, ignoramos la validación por ahora
                consequent_id = FuzzyRoutineId(request.consequent) if request.consequent else None
        
        # 4. Crear la entidad FuzzyRule
        conditions = [
            {
                "variableId": FuzzyVariableId(condition.variable_id),
                "operator": LogicalOperator(condition.operator),
                "value": condition.value
            }
            for condition in request.conditions
        ]
        
        connectors = [RuleConnector(connector) for connector in request.connectors]
        
        rule = FuzzyRule(
            name=request.name,
            system_id=system_id,
            description=request.description,
            conditions=conditions,
            connectors=connectors,
            consequent=consequent_id,
            created_at=datetime.now(timezone.utc)
        )
        
        # 5. Guardar en el repositorio
        saved_rule = await rule_repo.create(rule)
        
        # 6. Asignar resultado al comando
        request._result = FuzzyRuleDto.from_entity(saved_rule)
