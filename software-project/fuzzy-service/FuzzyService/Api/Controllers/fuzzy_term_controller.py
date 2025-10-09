from __future__ import annotations

from typing import List, Optional

from fastapi import APIRouter, HTTPException
from kink import di
from medyator import Medyator

from FuzzyService.Application.Features.FuzzyTerms.DTOs.FuzzyTermDto import FuzzyTermDto
from FuzzyService.Application.Features.FuzzyTerms.Commands.CreateFuzzyTermCommand import CreateFuzzyTermCommand
from FuzzyService.Application.Features.FuzzyTerms.Commands.UpdateFuzzyTermCommand import UpdateFuzzyTermCommand
from FuzzyService.Application.Features.FuzzyTerms.Commands.DeleteFuzzyTermCommand import DeleteFuzzyTermCommand
from FuzzyService.Application.Features.FuzzyTerms.Queries.GetAllFuzzyTermsQuery import GetAllFuzzyTermsQuery
from FuzzyService.Application.Features.FuzzyTerms.Queries.GetFuzzyTermByIdQuery import GetFuzzyTermByIdQuery
from FuzzyService.Domain.Errors.DomainErrors import (
    EntityNotFoundError,
    DuplicateEntityError,
)

router = APIRouter(prefix="/api/fuzzy-terms", tags=["fuzzy-terms"])


@router.get("", response_model=List[FuzzyTermDto])
async def list_fuzzy_terms(variable_id: Optional[str] = None) -> List[FuzzyTermDto]:
    mediator: Medyator = di[Medyator]
    query = GetAllFuzzyTermsQuery(variable_id=variable_id)
    result = await mediator.send(query)
    return result


@router.get("/{id}", response_model=FuzzyTermDto)
async def get_fuzzy_term_by_id(id: str) -> FuzzyTermDto:
    mediator: Medyator = di[Medyator]
    try:
        dto = await mediator.send(GetFuzzyTermByIdQuery(id=id))
        return dto
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e


@router.post("", response_model=FuzzyTermDto, status_code=201)
async def create_fuzzy_term(command: CreateFuzzyTermCommand) -> FuzzyTermDto:
    mediator: Medyator = di[Medyator]
    try:
        await mediator.send(command)
        return command._result
    except DuplicateEntityError as e:
        raise HTTPException(status_code=409, detail=str(e)) from e


@router.put("/{id}", response_model=FuzzyTermDto)
async def update_fuzzy_term(id: str, command: UpdateFuzzyTermCommand) -> FuzzyTermDto:
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
async def delete_fuzzy_term(id: str) -> bool:
    mediator: Medyator = di[Medyator]
    try:
        command = DeleteFuzzyTermCommand(id=id)
        await mediator.send(command)
        return command._result
    except EntityNotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e)) from e
