from __future__ import annotations

from typing import Literal
from pydantic import BaseModel, Field
from medyator import Query

from FuzzyService.Application.Features.FuzzyVariables.DTOs.FuzzyVariableDto import FuzzyVariableDto


class GetFuzzyVariablesBySystemQuery(BaseModel, Query):
    """Query para obtener variables de un sistema (todas/input/output)."""

    system_id: str = Field(..., min_length=1)
    role: Literal["all", "input", "output"] = Field(
        default="all",
        description="Filtrar por rol dentro del sistema: todas, solo input o solo output",
    )
    skip: int = Field(0, ge=0)
    limit: int = Field(50, ge=1, le=200)
