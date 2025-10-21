from __future__ import annotations

from typing import List, Dict, Any
from pydantic import BaseModel, Field
from medyator import Query


class GetAllRulesNameDescriptionQuery(BaseModel, Query):
    """Query para obtener todas las reglas difusas con solo id, nombre y descripción."""
    
    skip: int = Field(
        0, 
        ge=0,
        description="Número de elementos a omitir para paginación"
    )
    limit: int = Field(
        100, 
        ge=1, 
        le=1000,
        description="Número máximo de elementos a retornar"
    )
    
    _result: List[Dict[str, Any]] = []
