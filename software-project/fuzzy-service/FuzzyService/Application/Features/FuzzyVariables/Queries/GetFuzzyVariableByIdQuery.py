from __future__ import annotations

from typing import Optional

from pydantic import BaseModel, Field
from medyator import Query

from FuzzyService.Application.Features.FuzzyVariables.DTOs.FuzzyVariableDto import FuzzyVariableDto


class GetFuzzyVariableByIdQuery(BaseModel, Query):
    """Query para obtener una variable difusa por su Id."""

    id: str = Field(..., min_length=1)
    include_terms: bool = Field(default=True, description="Si true, retorna DTO con términos mapeados si están disponibles")

