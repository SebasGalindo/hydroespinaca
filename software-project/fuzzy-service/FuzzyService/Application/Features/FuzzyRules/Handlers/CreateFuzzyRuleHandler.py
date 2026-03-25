from __future__ import annotations

from typing import Optional
from kink import di
from medyator import CommandHandler
from datetime import datetime, timezone

from FuzzyService.Application.Features.FuzzyRules.Commands.CreateFuzzyRuleCommand import CreateFuzzyRuleCommand
from FuzzyService.Application.Features.FuzzyRules.DTOs.FuzzyRuleDto import FuzzyRuleDto
from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyRuleId, FuzzySystemId, FuzzyVariableId
from FuzzyService.Domain.Enums import LogicalOperator, RuleConnector
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
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

        # 3. Construir condiciones
        conditions = [
            {
                "variableId": FuzzyVariableId(condition.variable_id),
                "operator": LogicalOperator(condition.operator),
                "value": condition.value
            }
            for condition in request.conditions
        ]

        connectors = [RuleConnector(connector) for connector in request.connectors]

        # 4. Construir consecuentes Mamdani
        from FuzzyService.Domain.Entities.rule_consequent import RuleConsequent
        from FuzzyService.Domain.ValueObjects.DomainId import FuzzyTermId

        consequents = [
            RuleConsequent(
                variable_id=FuzzyVariableId(cons_dto.variable_id),
                terms=[FuzzyTermId(t) for t in cons_dto.terms],
                aggregation_method=cons_dto.aggregation_method
            )
            for cons_dto in request.consequents
        ]

        rule = FuzzyRule(
            name=request.name,
            system_id=system_id,
            description=request.description,
            conditions=conditions,
            connectors=connectors,
            consequents=consequents,
            created_at=datetime.now(timezone.utc)
        )
        
        # 5. Guardar en el repositorio
        saved_rule = await rule_repo.create(rule)
        
        # 6. Agregar la regla al sistema fuzzy
        system.add_rule(saved_rule.id)
        await system_repo.update(system)
        
        # 7. Asignar resultado al comando
        request._result = FuzzyRuleDto.from_entity(saved_rule)
