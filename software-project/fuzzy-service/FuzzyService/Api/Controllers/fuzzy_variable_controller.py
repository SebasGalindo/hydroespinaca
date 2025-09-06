from __future__ import annotations

from typing import List, Optional

from fastapi import APIRouter, HTTPException
from kink import di
from medyator import Medyator

from FuzzyService.Application.Features.FuzzyVariables.DTOs.FuzzyVariableDto import FuzzyVariableDto
from FuzzyService.Application.Features.FuzzyVariables.Commands.CreateFuzzyVariableCommand import CreateFuzzyVariableCommand
from FuzzyService.Application.Features.FuzzyVariables.Commands.UpdateFuzzyVariableCommand import UpdateFuzzyVariableCommand
from FuzzyService.Application.Features.FuzzyVariables.Commands.DeleteFuzzyVariableCommand import DeleteFuzzyVariableCommand
from FuzzyService.Application.Features.FuzzyVariables.Commands.AddTermToVariableCommand import AddTermToVariableCommand
from FuzzyService.Application.Features.FuzzyVariables.Commands.RemoveTermFromVariableCommand import RemoveTermFromVariableCommand
from FuzzyService.Application.Features.FuzzyVariables.Queries.GetAllFuzzyVariablesQuery import GetAllFuzzyVariablesQuery
from FuzzyService.Application.Features.FuzzyVariables.Queries.GetFuzzyVariableByIdQuery import GetFuzzyVariableByIdQuery
from FuzzyService.Domain.Errors.DomainErrors import (
    EntityNotFoundError,
    DuplicateEntityError,
)

router = APIRouter(prefix="/api/fuzzy-variables", tags=["fuzzy-variables"])


@router.get("", response_model=List[FuzzyVariableDto])
async def list_fuzzy_variables(
    skip: int = 0,
    limit: int = 50,
    variable_type: Optional[str] = None,
    device_id: Optional[str] = None,
    name_contains: Optional[str] = None,
    term_id: Optional[str] = None,
    order_by_created_at_desc: bool = True,
) -> List[FuzzyVariableDto]:
    mediator: Medyator = di[Medyator]
    query = GetAllFuzzyVariablesQuery(
        skip=skip,
        limit=limit,
        variable_type=variable_type,
        device_id=device_id,
        name_contains=name_contains,
        term_id=term_id,
        order_by_created_at_desc=order_by_created_at_desc,
    )
    result = await mediator.send(query)
    return result


@router.get("/{id}", response_model=FuzzyVariableDto)
async def get_fuzzy_variable_by_id(id: str) -> FuzzyVariableDto:
    mediator: Medyator = di[Medyator]
    try:
        dto = await mediator.send(GetFuzzyVariableByIdQuery(id=id))
        return dto
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e


@router.post("", response_model=FuzzyVariableDto, status_code=201)
async def create_fuzzy_variable(command: CreateFuzzyVariableCommand) -> FuzzyVariableDto:
    mediator: Medyator = di[Medyator]
    try:
        await mediator.send(command)
        return command._result
    except DuplicateEntityError as e:
        raise HTTPException(status_code=409, detail=str(e)) from e


@router.put("/{id}", response_model=FuzzyVariableDto)
async def update_fuzzy_variable(id: str, command: UpdateFuzzyVariableCommand) -> FuzzyVariableDto:
    mediator: Medyator = di[Medyator]
    try:
        command.id = id
        await mediator.send(command)
        return command._result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e
    except DuplicateEntityError as e:
        raise HTTPException(status_code=409, detail=str(e)) from e


@router.delete("/{id}", response_model=bool)
async def delete_fuzzy_variable(id: str) -> bool:
    mediator: Medyator = di[Medyator]
    try:
        command = DeleteFuzzyVariableCommand(id=id)
        await mediator.send(command)
        return command._result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e


@router.get("/system/{system_id}", response_model=List[FuzzyVariableDto])
async def get_fuzzy_variables_by_system(
    system_id: str,
    role: Optional[str] = "all",
    skip: int = 0,
    limit: int = 50,
) -> List[FuzzyVariableDto]:
    mediator: Medyator = di[Medyator]
    query = GetAllFuzzyVariablesQuery  # placeholder to avoid NameError in snippet
    from FuzzyService.Application.Features.FuzzyVariables.Queries.GetFuzzyVariablesBySystemQuery import GetFuzzyVariablesBySystemQuery

    q = GetFuzzyVariablesBySystemQuery(system_id=system_id, role=role or "all", skip=skip, limit=limit)
    result = await mediator.send(q)
    return result


@router.post("/{variable_id}/terms", response_model=FuzzyVariableDto)
async def add_term_to_variable(variable_id: str, command: AddTermToVariableCommand) -> FuzzyVariableDto:
    """Agregar un término a una variable difusa."""
    mediator: Medyator = di[Medyator]
    try:
        command.variable_id = variable_id
        await mediator.send(command)
        return command._result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e
    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e)) from e


@router.delete("/{variable_id}/terms/{term_id}", response_model=FuzzyVariableDto)
async def remove_term_from_variable(variable_id: str, term_id: str) -> FuzzyVariableDto:
    """Remover un término de una variable difusa."""
    mediator: Medyator = di[Medyator]
    try:
        command = RemoveTermFromVariableCommand(variable_id=variable_id, term_id=term_id)
        await mediator.send(command)
        return command._result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e
    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e)) from e
