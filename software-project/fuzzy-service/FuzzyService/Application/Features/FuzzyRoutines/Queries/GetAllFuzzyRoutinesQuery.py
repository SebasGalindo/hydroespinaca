from __future__ import annotations

from typing import List, Optional
from pydantic import BaseModel, Field, PrivateAttr, ConfigDict
from medyator import Query

from FuzzyService.Application.Features.FuzzyRoutines.DTOs.FuzzyRoutineDto import FuzzyRoutineDto


class GetAllFuzzyRoutinesQuery(BaseModel, Query):
    """Query para obtener todas las rutinas difusas con paginación."""
    
    model_config = ConfigDict(validate_assignment=True, extra="forbid")
    
    skip: int = Field(
        default=0, 
        ge=0,
        description="Número de registros a omitir para paginación"
    )
    limit: int = Field(
        default=100, 
        ge=1, 
        le=1000,
        description="Número máximo de registros a retornar"
    )
    
    _result: Optional[List[FuzzyRoutineDto]] = PrivateAttr(default=None)
