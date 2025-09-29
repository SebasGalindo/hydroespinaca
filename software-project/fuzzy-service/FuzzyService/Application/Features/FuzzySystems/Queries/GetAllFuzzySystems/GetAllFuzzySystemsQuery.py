from __future__ import annotations

from typing import Optional
from datetime import datetime

from pydantic import BaseModel, Field
from medyator import Query


class GetAllFuzzySystemsQuery(BaseModel, Query):
    """Query para obtener sistemas difusos con filtros y paginación."""

    # Paginación
    skip: int = Field(0, ge=0)
    limit: int = Field(50, ge=1, le=200)

    # Filtros opcionales
    isActive: Optional[bool] = None
    name_contains: Optional[str] = Field(default=None, min_length=1)
    created_after: Optional[datetime] = None
    created_before: Optional[datetime] = None

    # Ordenamiento (por ahora simple por created_at desc/asc)
    order_by_created_at_desc: bool = True
