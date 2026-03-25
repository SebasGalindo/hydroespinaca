from __future__ import annotations

from typing import Optional, List, Dict, Any
from datetime import datetime

from pydantic import BaseModel, Field, PrivateAttr
from medyator import Query


class ExportFuzzySystemQuery(BaseModel, Query):
    """Query para exportar un sistema difuso completo a JSON portátil.
    
    Retorna el sistema con todas sus variables, términos y reglas
    en un formato independiente de IDs (usa índices de posición).
    """

    # ID del sistema a exportar
    id: str = Field(min_length=1)

    # Campo result para almacenar el resultado
    _result: Optional[Dict[str, Any]] = PrivateAttr(default=None)
