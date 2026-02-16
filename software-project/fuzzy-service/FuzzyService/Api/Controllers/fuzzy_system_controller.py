from __future__ import annotations

from datetime import datetime
from typing import List, Optional

from fastapi import APIRouter, HTTPException, Depends
from kink import di
from medyator import Medyator

from FuzzyService.Infrastructure.Authentication.jwt_auth import get_current_user, require_scopes, Scopes, UserClaims

from FuzzyService.Application.Features.FuzzySystems.DTOs.FuzzySystemDto import FuzzySystemDto
from FuzzyService.Application.Features.FuzzySystems.Commands.CreateFuzzySystem.CreateFuzzySystemCommand import CreateFuzzySystemCommand
from FuzzyService.Application.Features.FuzzySystems.Commands.UpdateFuzzySystem.UpdateFuzzySystemCommand import UpdateFuzzySystemCommand
from FuzzyService.Application.Features.FuzzySystems.Commands.DeleteFuzzySystem.DeleteFuzzySystemCommand import DeleteFuzzySystemCommand
from FuzzyService.Application.Features.FuzzySystems.Commands.ActivateFuzzySystem.ActivateFuzzySystemCommand import ActivateFuzzySystemCommand
from FuzzyService.Application.Features.FuzzySystems.Commands.CloneFuzzySystem.CloneFuzzySystemCommand import CloneFuzzySystemCommand
from FuzzyService.Application.Features.FuzzySystems.Commands.ImportFuzzySystem.ImportFuzzySystemCommand import ImportFuzzySystemCommand
from FuzzyService.Application.Features.FuzzySystems.Queries.GetFuzzySystemById.GetFuzzySystemByIdQuery import GetFuzzySystemByIdQuery
from FuzzyService.Application.Features.FuzzySystems.Queries.GetAllFuzzySystems.GetAllFuzzySystemsQuery import GetAllFuzzySystemsQuery
from FuzzyService.Application.Features.FuzzySystems.Queries.ExportFuzzySystem.ExportFuzzySystemQuery import ExportFuzzySystemQuery
from FuzzyService.Application.Features.FuzzySystems.Queries.SimulateFuzzySystem.SimulateFuzzySystemQuery import SimulateFuzzySystemQuery
from FuzzyService.Application.Features.FuzzySystems.Commands.UpdateFuzzySystemStatus.UpdateFuzzySystemStatusCommand import UpdateFuzzySystemStatusCommand
from FuzzyService.Domain.Enums import FuzzySystemStatus
from FuzzyService.Domain.Errors.DomainErrors import (
    EntityNotFoundError,
    DuplicateEntityError,
    BusinessRuleViolationError,
)

router = APIRouter(prefix="/api/fuzzy-systems", tags=["fuzzy-systems"])


@router.get("", response_model=List[FuzzySystemDto])
async def list_fuzzy_systems(
    skip: int = 0,
    limit: int = 50,
    isActive: Optional[bool] = None,
    name_contains: Optional[str] = None,
    created_after: Optional[datetime] = None,
    created_before: Optional[datetime] = None,
    order_by_created_at_desc: bool = True,
    user: UserClaims = Depends(require_scopes(Scopes.FUZZY_SYSTEM_READ)),
) -> List[FuzzySystemDto]:
    mediator: Medyator = di[Medyator]
    query = GetAllFuzzySystemsQuery(
        skip=skip,
        limit=limit,
        isActive=isActive,
        name_contains=name_contains,
        created_after=created_after,
        created_before=created_before,
        order_by_created_at_desc=order_by_created_at_desc,
    )
    result = await mediator.send(query)
    return result


@router.get("/{id}", response_model=FuzzySystemDto)
async def get_fuzzy_system_by_id(
    id: str,
    user: UserClaims = Depends(require_scopes(Scopes.FUZZY_SYSTEM_READ)),
) -> FuzzySystemDto:
    mediator: Medyator = di[Medyator]
    try:
        dto = await mediator.send(GetFuzzySystemByIdQuery(id=id))
        return dto
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e


@router.post("", response_model=FuzzySystemDto, status_code=201)
async def create_fuzzy_system(
    command: CreateFuzzySystemCommand,
    user: UserClaims = Depends(require_scopes(Scopes.FUZZY_SYSTEM_CREATE)),
) -> FuzzySystemDto:
    mediator: Medyator = di[Medyator]
    try:
        await mediator.send(command)
        return command._result
    except DuplicateEntityError as e:
        raise HTTPException(status_code=409, detail=str(e)) from e
    

@router.put("/{id}", response_model=FuzzySystemDto)
async def update_fuzzy_system(
    id: str,
    command: UpdateFuzzySystemCommand,
    user: UserClaims = Depends(require_scopes(Scopes.FUZZY_SYSTEM_UPDATE)),
) -> FuzzySystemDto:
    mediator: Medyator = di[Medyator]
    try:
        # Asegurar que el ID del path coincida con el del command
        command.id = id
        await mediator.send(command)
        return command._result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e
    except DuplicateEntityError as e:
        raise HTTPException(status_code=409, detail=str(e)) from e


@router.patch("/{id}/status", response_model=FuzzySystemDto)
async def update_fuzzy_system_status(
    id: str,
    command: UpdateFuzzySystemStatusCommand,
    user: UserClaims = Depends(require_scopes(Scopes.FUZZY_SYSTEM_UPDATE)),
) -> FuzzySystemDto:
    mediator: Medyator = di[Medyator]
    try:
        # Asegurar que el ID del path coincida con el del command
        command.id = id
        await mediator.send(command)
        return command._result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e


@router.post("/{id}/activate", response_model=FuzzySystemDto)
async def activate_fuzzy_system(
    id: str,
    user: UserClaims = Depends(require_scopes(Scopes.FUZZY_SYSTEM_UPDATE)),
) -> FuzzySystemDto:
    """Activa un sistema difuso de forma exclusiva.
    Desactiva todos los sistemas activos y activa el indicado.
    """
    mediator: Medyator = di[Medyator]
    try:
        command = ActivateFuzzySystemCommand(id=id)
        await mediator.send(command)
        return command._result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e
    except BusinessRuleViolationError as e:
        raise HTTPException(status_code=422, detail=str(e)) from e


@router.post("/{id}/clone", response_model=FuzzySystemDto, status_code=201)
async def clone_fuzzy_system(
    id: str,
    command: CloneFuzzySystemCommand = None,
    user: UserClaims = Depends(require_scopes(Scopes.FUZZY_SYSTEM_CREATE)),
) -> FuzzySystemDto:
    """Clona (deep copy) un sistema difuso completo con todas sus variables, términos y reglas."""
    mediator: Medyator = di[Medyator]
    if command is None:
        command = CloneFuzzySystemCommand()
    try:
        command.id = id
        await mediator.send(command)
        return command._result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e
    except DuplicateEntityError as e:
        raise HTTPException(status_code=409, detail=str(e)) from e


@router.get("/{id}/export")
async def export_fuzzy_system(
    id: str,
    user: UserClaims = Depends(require_scopes(Scopes.FUZZY_SYSTEM_READ)),
):
    """Exporta un sistema difuso completo a JSON portátil (sistema + variables + términos + reglas)."""
    mediator: Medyator = di[Medyator]
    try:
        query = ExportFuzzySystemQuery(id=id)
        result = await mediator.send(query)
        return result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e


@router.post("/import", response_model=FuzzySystemDto, status_code=201)
async def import_fuzzy_system(
    command: ImportFuzzySystemCommand,
    user: UserClaims = Depends(require_scopes(Scopes.FUZZY_SYSTEM_CREATE)),
) -> FuzzySystemDto:
    """Importa un sistema difuso desde un JSON exportado previamente."""
    mediator: Medyator = di[Medyator]
    try:
        await mediator.send(command)
        return command._result
    except DuplicateEntityError as e:
        raise HTTPException(status_code=409, detail=str(e)) from e


@router.post("/{id}/simulate")
async def simulate_fuzzy_system(
    id: str,
    query: SimulateFuzzySystemQuery,
    user: UserClaims = Depends(require_scopes(Scopes.FUZZY_SYSTEM_READ)),
):
    """Simula una evaluación fuzzy con inputs arbitrarios sin persistir el resultado."""
    mediator: Medyator = di[Medyator]
    try:
        query.id = id
        result = await mediator.send(query)
        return result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e
    except BusinessRuleViolationError as e:
        raise HTTPException(status_code=422, detail=str(e)) from e


@router.delete("/{id}", response_model=bool)
async def delete_fuzzy_system(
    id: str,
    user: UserClaims = Depends(require_scopes(Scopes.FUZZY_SYSTEM_DELETE)),
) -> bool:
    mediator: Medyator = di[Medyator]
    try:
        command = DeleteFuzzySystemCommand(id=id)
        await mediator.send(command)
        return command._result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e
