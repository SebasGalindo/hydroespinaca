from __future__ import annotations

from typing import Optional, List
from datetime import datetime, timezone

from fastapi import APIRouter, HTTPException, Query, Path, Depends
from fastapi.responses import JSONResponse

from FuzzyService.Infrastructure.Authentication.jwt_auth import get_current_user, require_scopes, Scopes, UserClaims

from FuzzyService.Application.Features.FuzzyEvaluations.Queries.GetAllFuzzyEvaluationsQuery import (
    GetAllFuzzyEvaluationsQuery,
    GetAllFuzzyEvaluationsResponse
)
from FuzzyService.Application.Features.FuzzyEvaluations.Queries.GetFuzzyEvaluationByIdQuery import (
    GetFuzzyEvaluationByIdQuery,
    GetFuzzyEvaluationByIdResponse
)
from FuzzyService.Application.Features.FuzzyEvaluations.Queries.GetFuzzyEvaluationsBySystemQuery import (
    GetFuzzyEvaluationsBySystemQuery,
    GetFuzzyEvaluationsBySystemResponse
)
from FuzzyService.Application.Features.FuzzyEvaluations.Commands.CreateFuzzyEvaluationCommand import CreateFuzzyEvaluationCommand
from FuzzyService.Application.Features.FuzzyEvaluations.Handlers.CreateFuzzyEvaluationHandler import CreateFuzzyEvaluationHandler
from FuzzyService.Application.Features.FuzzyEvaluations.Handlers.GetAllFuzzyEvaluationsHandler import GetAllFuzzyEvaluationsHandler
from FuzzyService.Application.Features.FuzzyEvaluations.Handlers.GetFuzzyEvaluationByIdHandler import GetFuzzyEvaluationByIdHandler
from FuzzyService.Application.Features.FuzzyEvaluations.Handlers.GetFuzzyEvaluationsBySystemHandler import GetFuzzyEvaluationsBySystemHandler
from FuzzyService.Application.Features.FuzzyEvaluations.DTOs.FuzzyEvaluationDto import FuzzyEvaluationDto
from kink import di


# Query parameter descriptions (Sonar: avoid duplicated string literals)
class QueryDescriptions:
    """Centralized query parameter descriptions for API documentation."""
    FILTER_BY_SYSTEM_ID = "Filtrar por ID del sistema fuzzy"
    PAGE_NUMBER = "Número de página"
    PAGE_SIZE = "Tamaño de página"


router = APIRouter(
    prefix="/api/fuzzy-evaluations",
    tags=["Fuzzy Evaluations"],
    responses={
        404: {"description": "Evaluación no encontrada"},
        422: {"description": "Error de validación"},
        500: {"description": "Error interno del servidor"}
    }
)


@router.post(
    "/",
    response_model=FuzzyEvaluationDto,
    status_code=201,
    summary="Crear evaluación fuzzy",
    description="Crea una nueva evaluación fuzzy con valores de entrada y reglas activadas"
)
async def create_evaluation(
    command: CreateFuzzyEvaluationCommand,
    user: UserClaims = Depends(require_scopes(Scopes.FUZZY_EVALUATION_CREATE)),
) -> FuzzyEvaluationDto:
    """
    Crea una nueva evaluación fuzzy.
    
    Args:
        command: Datos de la evaluación a crear
        
    Returns:
        FuzzyEvaluationDto: La evaluación creada
        
    Raises:
        HTTPException: Si hay errores de validación o el sistema no existe
    """
    try:
        # Obtener el handler desde el contenedor de dependencias
        handler: CreateFuzzyEvaluationHandler = di[CreateFuzzyEvaluationHandler]
        
        # Ejecutar el comando
        await handler(command)
        
        # Retornar el resultado
        if command._result is None:
            raise HTTPException(status_code=500, detail="Error al crear la evaluación")
            
        return command._result
        
    except ValueError as e:
        raise HTTPException(status_code=422, detail=f"Error de validación: {str(e)}")
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error interno: {str(e)}")


@router.get(
    "/",
    response_model=GetAllFuzzyEvaluationsResponse,
    summary="Listar evaluaciones fuzzy",
    description="Obtiene una lista paginada de evaluaciones fuzzy con filtros opcionales"
)
async def get_all_evaluations(
    system_id: Optional[str] = Query(None, description=QueryDescriptions.FILTER_BY_SYSTEM_ID),
    start_date: Optional[datetime] = Query(None, description="Fecha de inicio (ISO 8601)"),
    end_date: Optional[datetime] = Query(None, description="Fecha de fin (ISO 8601)"),
    page: int = Query(1, ge=1, description=QueryDescriptions.PAGE_NUMBER),
    page_size: int = Query(20, ge=1, le=100, description=QueryDescriptions.PAGE_SIZE),
    sort_by: str = Query("timestamp", description="Campo de ordenamiento"),
    sort_order: str = Query("desc", description="Orden (asc/desc)"),
    user: UserClaims = Depends(require_scopes(Scopes.FUZZY_EVALUATION_READ)),
) -> GetAllFuzzyEvaluationsResponse:
    """Obtiene todas las evaluaciones fuzzy con filtros y paginación."""
    try:
        query = GetAllFuzzyEvaluationsQuery(
            system_id=system_id,
            start_date=start_date,
            end_date=end_date,
            page=page,
            page_size=page_size,
            sort_by=sort_by,
            sort_order=sort_order
        )
        
        handler = GetAllFuzzyEvaluationsHandler()
        response = await handler(query)
        
        return response
        
    except ValueError as e:
        raise HTTPException(status_code=422, detail=str(e))
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error interno: {str(e)}")


@router.get(
    "/recent",
    response_model=GetAllFuzzyEvaluationsResponse,
    summary="Obtener evaluaciones recientes",
    description="Obtiene las evaluaciones fuzzy más recientes (últimas 24 horas por defecto)"
)
async def get_recent_evaluations(
    hours: int = Query(24, ge=1, le=168, description="Horas hacia atrás (máximo 7 días)"),
    system_id: Optional[str] = Query(None, description=QueryDescriptions.FILTER_BY_SYSTEM_ID),
    page: int = Query(1, ge=1, description=QueryDescriptions.PAGE_NUMBER),
    page_size: int = Query(20, ge=1, le=100, description=QueryDescriptions.PAGE_SIZE),
    user: UserClaims = Depends(require_scopes(Scopes.FUZZY_EVALUATION_READ)),
) -> GetAllFuzzyEvaluationsResponse:
    """Obtiene evaluaciones fuzzy recientes."""
    try:
        from datetime import timedelta
        
        # Calcular fecha de inicio (hace X horas)
        end_date = datetime.now(timezone.utc)
        start_date = end_date - timedelta(hours=hours)
        
        query = GetAllFuzzyEvaluationsQuery(
            system_id=system_id,
            start_date=start_date,
            end_date=end_date,
            page=page,
            page_size=page_size,
            sort_by="timestamp",
            sort_order="desc"
        )
        
        handler = GetAllFuzzyEvaluationsHandler()
        response = await handler(query)
        
        return response
        
    except ValueError as e:
        raise HTTPException(status_code=422, detail=str(e))
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error interno: {str(e)}")


@router.get(
    "/system/{system_id}",
    response_model=GetFuzzyEvaluationsBySystemResponse,
    summary="Obtener evaluaciones por sistema",
    description="Obtiene evaluaciones fuzzy de un sistema específico"
)
async def get_evaluations_by_system(
    system_id: str = Path(..., description="ID del sistema fuzzy"),
    start_date: Optional[datetime] = Query(None, description="Fecha de inicio (ISO 8601)"),
    end_date: Optional[datetime] = Query(None, description="Fecha de fin (ISO 8601)"),
    page: int = Query(1, ge=1, description=QueryDescriptions.PAGE_NUMBER),
    page_size: int = Query(20, ge=1, le=100, description=QueryDescriptions.PAGE_SIZE),
    sort_order: str = Query("desc", description="Orden por timestamp (asc/desc)"),
    user: UserClaims = Depends(require_scopes(Scopes.FUZZY_EVALUATION_READ)),
) -> GetFuzzyEvaluationsBySystemResponse:
    """Obtiene evaluaciones fuzzy de un sistema específico."""
    try:
        query = GetFuzzyEvaluationsBySystemQuery(
            system_id=system_id,
            start_date=start_date,
            end_date=end_date,
            page=page,
            page_size=page_size,
            sort_order=sort_order
        )
        
        handler = GetFuzzyEvaluationsBySystemHandler()
        response = await handler(query)
        
        return response
        
    except ValueError as e:
        raise HTTPException(status_code=422, detail=str(e))
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error interno: {str(e)}")


@router.get(
    "/{evaluation_id}",
    response_model=FuzzyEvaluationDto,
    summary="Obtener evaluación por ID",
    description="Obtiene una evaluación fuzzy específica por su ID"
)
async def get_evaluation_by_id(
    evaluation_id: str = Path(..., description="ID de la evaluación fuzzy"),
    user: UserClaims = Depends(require_scopes(Scopes.FUZZY_EVALUATION_READ)),
) -> FuzzyEvaluationDto:
    """Obtiene una evaluación fuzzy por su ID."""
    try:
        query = GetFuzzyEvaluationByIdQuery(evaluation_id=evaluation_id)
        
        handler = GetFuzzyEvaluationByIdHandler()
        response = await handler(query)
        
        if not response.found or response.evaluation is None:
            raise HTTPException(status_code=404, detail=f"Evaluación con ID {evaluation_id} no encontrada")
        
        return response.evaluation
        
    except ValueError as e:
        raise HTTPException(status_code=422, detail=str(e))
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error interno: {str(e)}")


@router.get(
    "/stats/summary",
    summary="Estadísticas de evaluaciones",
    description="Obtiene estadísticas generales de las evaluaciones fuzzy"
)
async def get_evaluation_stats(
    system_id: Optional[str] = Query(None, description=QueryDescriptions.FILTER_BY_SYSTEM_ID),
    days: int = Query(7, ge=1, le=365, description="Días hacia atrás para las estadísticas"),
    user: UserClaims = Depends(require_scopes(Scopes.FUZZY_EVALUATION_READ)),
) -> JSONResponse:
    """Obtiene estadísticas de evaluaciones fuzzy."""
    try:
        from datetime import timedelta
        from FuzzyService.Domain.Interfaces.IFuzzyEvaluationRepository import IFuzzyEvaluationRepository
        
        # Calcular rango de fechas
        end_date = datetime.now(timezone.utc)
        start_date = end_date - timedelta(days=days)
        
        # Obtener repositorio
        evaluation_repo: IFuzzyEvaluationRepository = di[IFuzzyEvaluationRepository]
        
        # Construir filtros
        filters = {
            'start_date': start_date,
            'end_date': end_date
        }
        if system_id:
            filters['system_id'] = system_id
        
        # Obtener evaluaciones para estadísticas
        evaluations = await evaluation_repo.filter_evaluations(filters=filters, skip=0, limit=10000)
        
        # Calcular estadísticas
        total_evaluations = len(evaluations)
        
        # Estadísticas por sistema
        systems_stats = {}
        for eval in evaluations:
            sys_id = str(eval.system_id) if eval.system_id else "unknown"
            if sys_id not in systems_stats:
                systems_stats[sys_id] = 0
            systems_stats[sys_id] += 1
        
        # Estadísticas por día
        daily_stats = {}
        for eval in evaluations:
            if eval.timestamp:
                day_key = eval.timestamp.strftime('%Y-%m-%d')
                if day_key not in daily_stats:
                    daily_stats[day_key] = 0
                daily_stats[day_key] += 1
        
        stats = {
            "total_evaluations": total_evaluations,
            "date_range": {
                "start_date": start_date.isoformat(),
                "end_date": end_date.isoformat(),
                "days": days
            },
            "systems_stats": systems_stats,
            "daily_stats": daily_stats,
            "avg_evaluations_per_day": total_evaluations / days if days > 0 else 0
        }
        
        return JSONResponse(content=stats)
        
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error interno: {str(e)}")
