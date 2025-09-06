from __future__ import annotations

from fastapi import APIRouter
from fastapi.responses import JSONResponse

from kink import di

from FuzzyService.Domain.Interfaces.IActuatorService import IActuatorService


router = APIRouter(
    prefix="/api/actuator-integration",
    tags=["Actuator Integration"],
    responses={
        503: {"description": "Servicio de actuadores no disponible"},
        500: {"description": "Error interno del servidor"},
    },
)


@router.get(
    "/status",
    summary="Estado de comunicación con actuator-service",
    description="Verifica si el servicio de actuadores está disponible y funcionando",
)
async def get_actuator_status() -> JSONResponse:
    """Verifica el estado de comunicación con el actuator-service."""
    try:
        # Obtener el servicio desde el contenedor de dependencias
        actuator_service: IActuatorService = di[IActuatorService]

        # Verificar disponibilidad
        is_available = await actuator_service.is_available()

        status_code = 200 if is_available else 503

        return JSONResponse(
            status_code=status_code,
            content={
                "service_name": "actuator-service",
                "available": is_available,
                "status": "healthy" if is_available else "unavailable",
                "message": (
                    "El servicio de actuadores está disponible y funcionando"
                    if is_available
                    else "El servicio de actuadores no está disponible"
                ),
            },
        )

    except Exception as e:
        return JSONResponse(
            status_code=500,
            content={
                "service_name": "actuator-service",
                "available": False,
                "status": "error",
                "message": f"Error al verificar el estado del servicio: {str(e)}",
            },
        )