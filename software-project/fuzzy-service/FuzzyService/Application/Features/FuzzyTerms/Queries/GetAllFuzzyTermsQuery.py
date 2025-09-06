from __future__ import annotations

from typing import Optional

from pydantic import BaseModel, Field
from medyator import Query

from FuzzyService.Application.Features.FuzzyTerms.DTOs.FuzzyTermDto import FuzzyTermDto


class GetAllFuzzyTermsQuery(BaseModel, Query):
    """Query para obtener términos, con filtro opcional por variable_id."""

    variable_id: Optional[str] = Field(default=None, description="Filtrar por variable_id")
    # Nota: se podría añadir paginación si es necesario más adelante

