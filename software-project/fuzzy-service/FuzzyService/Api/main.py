from __future__ import annotations

import logging
from datetime import datetime
from fastapi import FastAPI
from fastapi.responses import JSONResponse
from contextlib import asynccontextmanager

from FuzzyService.Api.Configuration.api_configuration import configure_api
from FuzzyService.Api.Middleware.error_handling_middleware import ErrorHandlingMiddleware

from FuzzyService.Infrastructure.Configuration import DependencyInjection as infra_di
from FuzzyService.Application.Configuration import DependencyInjection as app_di

# Routers
from FuzzyService.Api.Controllers import fuzzy_system_controller
from FuzzyService.Api.Controllers import fuzzy_variable_controller
from FuzzyService.Api.Controllers import fuzzy_term_controller
from FuzzyService.Api.Controllers import fuzzy_rule_controller
from FuzzyService.Api.Controllers import fuzzy_evaluation_controller

# Environment-based configuration (similar to .NET services)
import os
environment = os.getenv("ASPNETCORE_ENVIRONMENT", "Development")
is_development = environment.lower() == "development"

# Configuración granular de logging basada en entorno
log_level = logging.DEBUG if is_development else logging.INFO
logging.basicConfig(level=log_level)

# Configurar loggers específicos
if is_development:
    logging.getLogger("fuzzy-service").setLevel(logging.DEBUG)
    logging.getLogger("FuzzyService").setLevel(logging.DEBUG)
    logging.getLogger("ActuatorService").setLevel(logging.DEBUG)
else:
    logging.getLogger("fuzzy-service").setLevel(logging.INFO)
    logging.getLogger("FuzzyService").setLevel(logging.INFO)
    logging.getLogger("ActuatorService").setLevel(logging.WARNING)

# Silenciar loggers ruidosos
logging.getLogger("pymongo").setLevel(logging.WARNING)
logging.getLogger("motor").setLevel(logging.WARNING)
logging.getLogger("httpx").setLevel(logging.WARNING)
logging.getLogger("httpcore").setLevel(logging.WARNING)

_logger = logging.getLogger("fuzzy-service")


@asynccontextmanager
async def lifespan(app: FastAPI):
    # Startup: initialize infrastructure resources
    await infra_di.on_startup()
    
    # Initialize authentication service
    from FuzzyService.Infrastructure.Authentication.jwt_auth import initialize_auth_service
    await initialize_auth_service()
    
    _logger.info("Application startup completed")
    try:
        yield
    finally:
        # Shutdown: dispose infrastructure resources
        await infra_di.on_shutdown()
        _logger.info("Application shutdown completed")


app = FastAPI(lifespan=lifespan)
configure_api(app)

# Configure infrastructure DI first (provides concrete implementations)
infra_di.configure_infrastructure_di()

# Configure application DI second (uses infrastructure implementations)
app_di.configure_application_di()

# Middleware
app.add_middleware(ErrorHandlingMiddleware)

# Routers
app.include_router(fuzzy_system_controller.router)
app.include_router(fuzzy_variable_controller.router)
app.include_router(fuzzy_term_controller.router)
app.include_router(fuzzy_rule_controller.router)
app.include_router(fuzzy_evaluation_controller.router)

# Health endpoint (no authentication required for health checks)
@app.get("/health", tags=["health"])  # simple Dockerfile healthcheck compatibility
async def health() -> JSONResponse:
    """Health check endpoint similar to .NET services."""
    try:
        # Basic health check - service is running
        health_status = {
            "status": "Healthy",
            "service": "fuzzy-service",
            "environment": environment,
            "timestamp": datetime.utcnow().isoformat() + "Z"
        }
        
        # In development, add more details
        if is_development:
            from FuzzyService.Infrastructure.Configuration.DatabaseConfiguration import get_settings
            mongo_settings = get_settings()
            health_status["database"] = {
                "connection": "configured",
                "database": mongo_settings.database
            }
        
        return JSONResponse(content=health_status)
    except Exception as e:
        _logger.error(f"Health check failed: {e}")
        return JSONResponse(
            status_code=503,
            content={
                "status": "Unhealthy",
                "service": "fuzzy-service",
                "error": str(e) if is_development else "Service unavailable"
            }
        )
