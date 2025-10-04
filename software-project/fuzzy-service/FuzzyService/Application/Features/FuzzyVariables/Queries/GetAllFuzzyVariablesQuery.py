from __future__ import annotations

from typing import Optional
from datetime import datetime

from pydantic import BaseModel, Field
from medyator import Query


class GetAllFuzzyVariablesQuery(BaseModel, Query):
    """Query para obtener variables difusas con filtros y paginación."""

    # Paginación
    skip: int = Field(0, ge=0)
    limit: int = Field(50, ge=1, le=200)

    # Filtros
    variable_type: Optional[str] = Field(default=None, description="'input' o 'output'")
    reference_id: Optional[str] = None
    name_contains: Optional[str] = Field(default=None, min_length=1)
    term_id: Optional[str] = None

    # Ordenamiento simple por created_at
    order_by_created_at_desc: bool = True
