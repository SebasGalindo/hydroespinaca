from __future__ import annotations

from typing import List, Optional

from fastapi import APIRouter, HTTPException
from kink import di
from medyator import Medyator

from FuzzyService.Application.Features.FuzzyRoutines.DTOs.FuzzyRoutineDto import FuzzyRoutineDto, RoutineStepDto, UpdateRoutineStepDto
from FuzzyService.Application.Features.FuzzyRoutines.Commands.CreateFuzzyRoutineCommand import CreateFuzzyRoutineCommand
from FuzzyService.Application.Features.FuzzyRoutines.Commands.UpdateFuzzyRoutineCommand import UpdateFuzzyRoutineCommand
from FuzzyService.Application.Features.FuzzyRoutines.Commands.DeleteFuzzyRoutineCommand import DeleteFuzzyRoutineCommand
from FuzzyService.Application.Features.FuzzyRoutines.Commands.AddStepToRoutineCommand import AddStepToRoutineCommand
from FuzzyService.Application.Features.FuzzyRoutines.Commands.UpdateStepInRoutineCommand import UpdateStepInRoutineCommand
from FuzzyService.Application.Features.FuzzyRoutines.Commands.DeleteStepFromRoutineCommand import DeleteStepFromRoutineCommand
from FuzzyService.Application.Features.FuzzyRoutines.Queries.GetAllFuzzyRoutinesQuery import GetAllFuzzyRoutinesQuery
from FuzzyService.Application.Features.FuzzyRoutines.Queries.GetFuzzyRoutineByIdQuery import GetFuzzyRoutineByIdQuery
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError, BusinessRuleViolationError

router = APIRouter(prefix="/api/fuzzy-routines", tags=["fuzzy-routines"])


@router.get("", response_model=List[FuzzyRoutineDto])
async def list_fuzzy_routines(
    skip: int = 0,
    limit: int = 50,
) -> List[FuzzyRoutineDto]:
    """Obtiene todas las rutinas difusas con paginaciÃ³n."""
    mediator: Medyator = di[Medyator]
    
    query = GetAllFuzzyRoutinesQuery(
        skip=skip,
        limit=limit
    )
    
    await mediator.send(query)
    return query._result


@router.get("/{routine_id}", response_model=FuzzyRoutineDto)
async def get_fuzzy_routine_by_id(routine_id: str) -> FuzzyRoutineDto:
    """Obtiene una rutina difusa por su ID."""
    mediator: Medyator = di[Medyator]
    
    try:
        query = GetFuzzyRoutineByIdQuery(routine_id=routine_id)
        await mediator.send(query)
        
        if query._result is None:
            raise HTTPException(status_code=404, detail=f"Rutina con ID {routine_id} no encontrada")
        
        return query._result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e


@router.post("", response_model=FuzzyRoutineDto, status_code=201)
async def create_fuzzy_routine(command: CreateFuzzyRoutineCommand) -> FuzzyRoutineDto:
    """Crea una nueva rutina difusa."""
    mediator: Medyator = di[Medyator]
    
    try:
        await mediator.send(command)
        return command._result
    except BusinessRuleViolationError as e:
        raise HTTPException(status_code=400, detail=str(e)) from e


@router.put("/{routine_id}", response_model=FuzzyRoutineDto)
async def update_fuzzy_routine(routine_id: str, command: UpdateFuzzyRoutineCommand) -> FuzzyRoutineDto:
    """Actualiza una rutina difusa existente."""
    mediator: Medyator = di[Medyator]
    
    try:
        command.routine_id = routine_id
        await mediator.send(command)
        return command._result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e
    except BusinessRuleViolationError as e:
        raise HTTPException(status_code=400, detail=str(e)) from e


@router.delete("/{routine_id}", response_model=bool)
async def delete_fuzzy_routine(routine_id: str) -> bool:
    """Elimina una rutina difusa."""
    mediator: Medyator = di[Medyator]
    
    try:
        command = DeleteFuzzyRoutineCommand(routine_id=routine_id)
        await mediator.send(command)
        return command._result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e


@router.post("/{routine_id}/steps", response_model=FuzzyRoutineDto)
async def add_step_to_routine(routine_id: str, step: RoutineStepDto) -> FuzzyRoutineDto:
    """Agrega un paso a una rutina difusa existente."""
    mediator: Medyator = di[Medyator]
    
    try:
        command = AddStepToRoutineCommand(routine_id=routine_id, step=step)
        await mediator.send(command)
        return command._result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e
    except BusinessRuleViolationError as e:
        raise HTTPException(status_code=400, detail=str(e)) from e


@router.put("/{routine_id}/steps/{step_id}", response_model=FuzzyRoutineDto)
async def update_step_in_routine(
    routine_id: str,
    step_id: int,
    step_data: UpdateRoutineStepDto
) -> FuzzyRoutineDto:
    """Actualiza un paso especÃ­fico en una rutina difusa."""
    mediator: Medyator = di[Medyator]
    
    try:
        command = UpdateStepInRoutineCommand(
            routine_id=routine_id,
            step_id=step_id,
            condition=step_data.condition,
            power_term_id=step_data.power_term_id,
            duration_term_id=step_data.duration_term_id
        )
        await mediator.send(command)
        return command._result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e
    except BusinessRuleViolationError as e:
        raise HTTPException(status_code=400, detail=str(e)) from e


@router.delete("/{routine_id}/steps/{step_id}", response_model=FuzzyRoutineDto)
async def delete_step_from_routine(routine_id: str, step_id: int) -> FuzzyRoutineDto:
    """Elimina un paso especÃ­fico de una rutina difusa."""
    mediator: Medyator = di[Medyator]
    
    try:
        command = DeleteStepFromRoutineCommand(routine_id=routine_id, step_id=step_id)
        await mediator.send(command)
        return command._result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e

