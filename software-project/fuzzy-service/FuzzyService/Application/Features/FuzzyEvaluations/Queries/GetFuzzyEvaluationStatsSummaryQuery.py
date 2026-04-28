from __future__ import annotations

from typing import Dict, Any, Optional
from datetime import datetime

from pydantic import BaseModel, Field, field_validator


class GetFuzzyEvaluationStatsSummaryQuery(BaseModel):
    """Query para obtener las estadísticas de las evaluaciones fuzzy."""
    
    system_id: Optional[str] = Field(default=None, description="Filtrar por ID del sistema fuzzy")
    days: int = Field(default=7, ge=1, le=365, description="Días hacia atrás para las estadísticas")
    
    @field_validator('system_id')
    @classmethod
    def validate_system_id(cls, v: Optional[str]) -> Optional[str]:
        """Valida que el ID del sistema no esté vacío si se proporciona."""
        if v is not None and (not v or not v.strip()):
            raise ValueError('El ID del sistema no puede estar vacío')
        return v.strip() if v else None


class GetFuzzyEvaluationStatsSummaryResponse(BaseModel):
    """Respuesta para la query GetFuzzyEvaluationStatsSummary."""
    
    total_evaluations: int = Field(..., description="Número total de evaluaciones en el período")
    date_range: Dict[str, Any] = Field(..., description="Rango de fechas para las estadísticas")
    systems_stats: Dict[str, int] = Field(..., description="Estadísticas por sistema")
    daily_stats: Dict[str, int] = Field(..., description="Estadísticas por día")
    avg_evaluations_per_day: float = Field(..., description="Promedio de evaluaciones por día")
