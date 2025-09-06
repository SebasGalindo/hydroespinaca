from __future__ import annotations

import logging
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
from FuzzyService.Api.Controllers import fuzzy_routine_controller
from FuzzyService.Api.Controllers import fuzzy_evaluation_controller

logging.basicConfig(level=logging.INFO)
_logger = logging.getLogger("fuzzy-service")


@asynccontextmanager
async def lifespan(app: FastAPI):
    # Startup: initialize infrastructure resources
    await infra_di.on_startup()
    _logger.info("Application startup completed")
    try:
        yield
    finally:
        # Shutdown: dispose infrastructure resources
        await infra_di.on_shutdown()
        _logger.info("Application shutdown completed")


app = FastAPI(lifespan=lifespan)
configure_api(app)

# Configure infrastructure DI
infra_di.configure_infrastructure_di()

# Configure application DI
app_di.configure_application_di()

# Middleware
app.add_middleware(ErrorHandlingMiddleware)

# Routers
app.include_router(fuzzy_system_controller.router)
app.include_router(fuzzy_variable_controller.router)
app.include_router(fuzzy_term_controller.router)
app.include_router(fuzzy_rule_controller.router)
app.include_router(fuzzy_routine_controller.router)
app.include_router(fuzzy_evaluation_controller.router)

# Health endpoint
@app.get("/health", tags=["health"])  # simple Dockerfile healthcheck compatibility
async def health() -> JSONResponse:
    return JSONResponse(content={"status": "ok"})
