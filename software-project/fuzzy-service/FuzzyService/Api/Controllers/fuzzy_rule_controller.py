from __future__ import annotations

from typing import List, Optional

from fastapi import APIRouter, HTTPException
from kink import di
from medyator import Medyator

from FuzzyService.Application.Features.FuzzyRules.DTOs.FuzzyRuleDto import FuzzyRuleDto
from FuzzyService.Application.Features.FuzzyRules.Commands.CreateFuzzyRuleCommand import CreateFuzzyRuleCommand
from FuzzyService.Application.Features.FuzzyRules.Commands.UpdateFuzzyRuleCommand import UpdateFuzzyRuleCommand
from FuzzyService.Application.Features.FuzzyRules.Commands.DeleteFuzzyRuleCommand import DeleteFuzzyRuleCommand
from FuzzyService.Application.Features.FuzzyRules.Commands.AddConditionToRuleCommand import AddConditionToRuleCommand
from FuzzyService.Application.Features.FuzzyRules.Commands.RemoveConditionFromRuleCommand import RemoveConditionFromRuleCommand
from FuzzyService.Application.Features.FuzzyRules.Commands.UpdateRuleConnectorsCommand import UpdateRuleConnectorsCommand
from FuzzyService.Application.Features.FuzzyRules.Commands.UpdateRuleConsequentCommand import UpdateRuleConsequentCommand
from FuzzyService.Application.Features.FuzzyRules.Queries.GetAllFuzzyRulesQuery import GetAllFuzzyRulesQuery
from FuzzyService.Application.Features.FuzzyRules.Queries.GetFuzzyRuleByIdQuery import GetFuzzyRuleByIdQuery
from FuzzyService.Application.Features.FuzzyRules.Queries.GetFuzzyRulesBySystemQuery import GetFuzzyRulesBySystemQuery
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError, BusinessRuleViolationError

router = APIRouter(prefix="/api/fuzzy-rules", tags=["fuzzy-rules"])


@router.get("", response_model=List[FuzzyRuleDto])
async def list_fuzzy_rules(
    skip: int = 0,
    limit: int = 50,
    system_id: Optional[str] = None,
) -> List[FuzzyRuleDto]:
    """Obtiene todas las reglas difusas con paginación y filtros opcionales."""
    mediator: Medyator = di[Medyator]
    
    if system_id:
        query = GetFuzzyRulesBySystemQuery(
            system_id=system_id,
            skip=skip,
            limit=limit
        )
    else:
        query = GetAllFuzzyRulesQuery(
            skip=skip,
            limit=limit
        )
    
    await mediator.send(query)
    return query._result


@router.get("/{rule_id}", response_model=FuzzyRuleDto)
async def get_fuzzy_rule_by_id(rule_id: str) -> FuzzyRuleDto:
    """Obtiene una regla difusa por su ID."""
    mediator: Medyator = di[Medyator]
    
    try:
        query = GetFuzzyRuleByIdQuery(rule_id=rule_id)
        result = await mediator.send(query)
        
        if result is None:
            raise HTTPException(status_code=404, detail=f"Regla con ID {rule_id} no encontrada")
        
        return result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e


# Endpoints granulares para gestión de condiciones, conectores y consecuente

@router.post("/{rule_id}/conditions", response_model=FuzzyRuleDto, status_code=201)
async def add_condition_to_rule(
    rule_id: str,
    variable_id: str,
    operator: str,
    value: str,
    connector: Optional[str] = None,
) -> FuzzyRuleDto:
    """Agrega una nueva condición a una regla difusa existente."""
    mediator: Medyator = di[Medyator]
    
    try:
        command = AddConditionToRuleCommand(
            rule_id=rule_id,
            variable_id=variable_id,
            operator=operator,
            value=value,
            connector=connector
        )
        await mediator.send(command)
        return command.result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e
    except (ValueError, BusinessRuleViolationError) as e:
        raise HTTPException(status_code=400, detail=str(e)) from e


@router.delete("/{rule_id}/conditions/{variable_id}", response_model=FuzzyRuleDto)
async def remove_condition_from_rule(
    rule_id: str,
    variable_id: str,
) -> FuzzyRuleDto:
    """Remueve una condición de una regla difusa existente."""
    mediator: Medyator = di[Medyator]
    
    try:
        command = RemoveConditionFromRuleCommand(
            rule_id=rule_id,
            variable_id=variable_id
        )
        await mediator.send(command)
        return command.result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e
    except (ValueError, BusinessRuleViolationError) as e:
        raise HTTPException(status_code=400, detail=str(e)) from e


@router.put("/{rule_id}/connectors", response_model=FuzzyRuleDto)
async def update_rule_connectors(
    rule_id: str,
    connectors: List[str],
) -> FuzzyRuleDto:
    """Actualiza los conectores de una regla difusa existente."""
    mediator: Medyator = di[Medyator]
    
    try:
        command = UpdateRuleConnectorsCommand(
            rule_id=rule_id,
            connectors=connectors
        )
        await mediator.send(command)
        return command.result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e
    except (ValueError, BusinessRuleViolationError) as e:
        raise HTTPException(status_code=400, detail=str(e)) from e


@router.put("/{rule_id}/consequent", response_model=FuzzyRuleDto)
async def update_rule_consequent(
    rule_id: str,
    consequent: str,
) -> FuzzyRuleDto:
    """Actualiza el consecuente de una regla difusa existente."""
    mediator: Medyator = di[Medyator]
    
    try:
        command = UpdateRuleConsequentCommand(
            rule_id=rule_id,
            consequent=consequent
        )
        await mediator.send(command)
        return command.result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e
    except (ValueError, BusinessRuleViolationError) as e:
        raise HTTPException(status_code=400, detail=str(e)) from e


@router.post("", response_model=FuzzyRuleDto, status_code=201)
async def create_fuzzy_rule(command: CreateFuzzyRuleCommand) -> FuzzyRuleDto:
    """Crea una nueva regla difusa."""
    mediator: Medyator = di[Medyator]
    
    try:
        await mediator.send(command)
        return command._result
    except BusinessRuleViolationError as e:
        raise HTTPException(status_code=400, detail=str(e)) from e
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e


@router.put("/{rule_id}", response_model=FuzzyRuleDto)
async def update_fuzzy_rule(rule_id: str, command: UpdateFuzzyRuleCommand) -> FuzzyRuleDto:
    """Actualiza una regla difusa existente."""
    mediator: Medyator = di[Medyator]
    
    try:
        command.rule_id = rule_id
        await mediator.send(command)
        return command._result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e
    except BusinessRuleViolationError as e:
        raise HTTPException(status_code=400, detail=str(e)) from e


@router.delete("/{rule_id}", response_model=bool)
async def delete_fuzzy_rule(rule_id: str) -> bool:
    """Elimina una regla difusa."""
    mediator: Medyator = di[Medyator]
    
    try:
        command = DeleteFuzzyRuleCommand(rule_id=rule_id)
        await mediator.send(command)
        return command._result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e


@router.get("/system/{system_id}", response_model=List[FuzzyRuleDto])
async def get_fuzzy_rules_by_system(
    system_id: str,
    skip: int = 0,
    limit: int = 50,
) -> List[FuzzyRuleDto]:
    """Obtiene todas las reglas difusas de un sistema específico."""
    mediator: Medyator = di[Medyator]
    
    try:
        query = GetFuzzyRulesBySystemQuery(
            system_id=system_id,
            skip=skip,
            limit=limit
        )
        await mediator.send(query)
        return query._result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e
