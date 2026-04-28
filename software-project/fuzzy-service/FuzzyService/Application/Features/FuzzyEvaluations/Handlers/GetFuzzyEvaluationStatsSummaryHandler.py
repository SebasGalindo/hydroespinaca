from __future__ import annotations

from typing import Dict, Any
from datetime import datetime, timezone, timedelta

from FuzzyService.Application.Features.FuzzyEvaluations.Queries.GetFuzzyEvaluationStatsSummaryQuery import (
    GetFuzzyEvaluationStatsSummaryQuery,
    GetFuzzyEvaluationStatsSummaryResponse
)
from FuzzyService.Domain.Interfaces.IFuzzyEvaluationRepository import IFuzzyEvaluationRepository
from kink import di


class GetFuzzyEvaluationStatsSummaryHandler:
    """Handler para obtener las estadísticas generales de evaluaciones fuzzy."""
    
    async def __call__(self, query: GetFuzzyEvaluationStatsSummaryQuery) -> GetFuzzyEvaluationStatsSummaryResponse:
        """Ejecuta la query para obtener el resumen de estadísticas de evaluaciones fuzzy."""
        
        # Obtener repositorio
        evaluation_repo: IFuzzyEvaluationRepository = di[IFuzzyEvaluationRepository]
        
        # Calcular fechas
        end_date = datetime.now(timezone.utc)
        start_date = end_date - timedelta(days=query.days)
        
        # Configurar filtros
        filters: Dict[str, Any] = {"start_date": start_date, "end_date": end_date}
        if query.system_id:
            filters["system_id"] = query.system_id
            
        # Obtener estadísticas del repositorio
        summary = await evaluation_repo.compute_summary_stats(filters)
        total_evaluations = int(summary.get("total", 0))
        days = query.days
        
        # Formatear la respuesta
        return GetFuzzyEvaluationStatsSummaryResponse(
            total_evaluations=total_evaluations,
            date_range={
                "start_date": start_date.isoformat(),
                "end_date": end_date.isoformat(),
                "days": days,
            },
            systems_stats=summary.get("systems_stats", {}),
            daily_stats=summary.get("daily_stats", {}),
            avg_evaluations_per_day=total_evaluations / days if days > 0 else 0,
        )
