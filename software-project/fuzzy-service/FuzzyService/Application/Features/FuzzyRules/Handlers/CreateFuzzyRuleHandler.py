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

        # 3. Crear la entidad FuzzyRule (sin consecuente - debe agregarse con UpdateRuleConsequentCommand)
        # NOTA: Las reglas ahora deben tener consecuentes directos (RuleConsequent) en lugar de
        # apuntar a rutinas. El campo 'consequent' fue eliminado del modelo.
        # Para backward compatibility temporal, creamos una regla vacía que luego debe
        # actualizarse con consecuentes directos usando UpdateRuleConsequentHandler.

        conditions = [
            {
                "variableId": FuzzyVariableId(condition.variable_id),
                "operator": LogicalOperator(condition.operator),
                "value": condition.value
            }
            for condition in request.conditions
        ]

        connectors = [RuleConnector(connector) for connector in request.connectors]

        # Si el request tiene 'consequent' (legacy), lanzar error indicando usar nuevo modelo
        if hasattr(request, 'consequent') and request.consequent:
            raise BusinessRuleViolationError(
                "El campo 'consequent' (rutina) ya no está soportado. "
                "Use 'consequents' (array de RuleConsequent) en su lugar."
            )

        # Crear regla con consecuentes directos si se proporcionan
        from FuzzyService.Domain.Entities.rule_consequent import RuleConsequent
        from FuzzyService.Domain.ValueObjects.DomainId import FuzzyTermId

        consequents = []
        if hasattr(request, 'consequents') and request.consequents:
            for cons_dto in request.consequents:
                consequent = RuleConsequent(
                    variable_id=FuzzyVariableId(cons_dto['variable_id']),
                    terms=[FuzzyTermId(t) for t in cons_dto['terms']],
                    aggregation_method=cons_dto.get('aggregation_method', 'max')
                )
                consequents.append(consequent)

        # Por ahora, si no hay consecuentes, crear lista vacía (fallará validación de FuzzyRule)
        # Esto forzará al usuario a proporcionar consecuentes válidos
        rule = FuzzyRule(
            name=request.name,
            system_id=system_id,
            description=request.description,
            conditions=conditions,
            connectors=connectors,
            consequents=consequents if consequents else [],
            created_at=datetime.now(timezone.utc)
        )
        
        # 5. Guardar en el repositorio
        saved_rule = await rule_repo.create(rule)
        
        # 6. Agregar la regla al sistema fuzzy
        system.add_rule(saved_rule.id)
        await system_repo.update(system)
        
        # 7. Asignar resultado al comando
        request._result = FuzzyRuleDto.from_entity(saved_rule)
