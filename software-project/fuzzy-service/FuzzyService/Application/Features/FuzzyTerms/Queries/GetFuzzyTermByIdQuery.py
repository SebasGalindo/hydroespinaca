from __future__ import annotations

from typing import Optional

from pydantic import BaseModel, Field, field_validator
from medyator import Query

from FuzzyService.Application.Features.FuzzyTerms.DTOs.FuzzyTermDto import FuzzyTermDto


class GetFuzzyTermByIdQuery(BaseModel, Query):
    """Query para obtener un término difuso por id."""

    id: str = Field(..., min_length=1)

    @field_validator("id")
    @classmethod
    def _validate_id(cls, v: str) -> str:
        v = (v or "").strip()
        if not v:
            raise ValueError("id es requerido")
        return v
