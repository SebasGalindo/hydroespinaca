"""Fuzzy Service - Servicio de control difuso para hidroponía.

Este servicio implementa lógica de control difuso para sistemas hidropónicos,
recibiendo lecturas de sensores vía MQTT y enviando comandos a actuadores
vía REST API.

Arquitectura Clean Architecture:
- domain/: Modelos de negocio y lógica pura
- application/: Casos de uso y DTOs
- infrastructure/: Implementaciones concretas (repos, clientes)
- api/: Endpoints REST y configuración FastAPI
- workers/: Procesamiento en background
"""

from __future__ import annotations

import asyncio
import logging
from contextlib import asynccontextmanager
from typing import List

from fastapi import Depends, FastAPI, Request, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
from fastapi.exceptions import RequestValidationError
from starlette.exceptions import HTTPException as StarletteHTTPException

# Importaciones de la nueva arquitectura
from api.endpoints import router
from application.use_cases import (
    FuzzyEvaluationUseCase,
    HysteresisFilterUseCase,
    RoutineManagementUseCase,
    SimulationUseCase,
    VariableManagementUseCase,
)
from application.interfaces.fuzzy_service import IFuzzyService
from domain.models import Routine, Variable
from domain.interfaces.actuator_client import IActuatorClient
from domain.interfaces.mqtt_client import IMQTTClient
from infrastructure.actuator_client import ActuatorServiceClient
from infrastructure.config import settings
from infrastructure.mqtt_client import SensorMQTTClient
from infrastructure.database import mongo_db
from infrastructure.repositories import (
    InMemoryActuatorStateRepository,
    InMemoryRoutineRepository,
    InMemoryVariableRepository,
)
from infrastructure.mongo_repositories import (
    MongoActuatorStateRepository,
    MongoRoutineRepository,
    MongoVariableRepository,
)
from infrastructure.fuzzy_service_impl import FuzzyServiceImpl
from workers.fuzzy_worker import FuzzyWorker


# ===== Configuración de logging =====
def setup_logging():
    """Configura el sistema de logging."""
    log_level = getattr(logging, settings.log_level.upper(), logging.INFO)
    
    if settings.log_format == "json":
        # TODO: Implementar formato JSON estructurado
        log_format = "%(asctime)s - %(name)s - %(levelname)s - %(message)s"
    else:
        log_format = "%(asctime)s - %(name)s - %(levelname)s - %(message)s"
    
    logging.basicConfig(
        level=log_level,
        format=log_format
    )
    
    # Reducir verbosidad de librerías externas
    logging.getLogger("httpx").setLevel(logging.WARNING)
    logging.getLogger("asyncio").setLevel(logging.WARNING)


setup_logging()


# ===== Inicialización de dependencias =====
class DependencyContainer:
    """Contenedor de dependencias para inyección.
    
    Centraliza la creación e inicialización de todas las dependencias
    del sistema siguiendo el patrón de inversión de dependencias.
    """
    
    def __init__(self):
        # Repositorios MongoDB
        self.variable_repository = MongoVariableRepository()
        self.routine_repository = MongoRoutineRepository()
        self.actuator_state_repository = MongoActuatorStateRepository()
        
        # Clientes externos (usando interfaces)
        self.actuator_client: IActuatorClient = ActuatorServiceClient(
            base_url=settings.actuator_service_url,
            api_key=settings.actuator_service_api_key,
            timeout=settings.actuator_service_timeout
        )
        
        self.mqtt_client: IMQTTClient = SensorMQTTClient(
            broker_host=settings.mqtt_broker_host,
            broker_port=settings.mqtt_broker_port,
            username=settings.mqtt_username,
            password=settings.mqtt_password,
            topic_pattern=settings.mqtt_topic_pattern
        )
        
        # Servicios de aplicación
        self.fuzzy_service: IFuzzyService = FuzzyServiceImpl(
            variable_repository=self.variable_repository,
            routine_repository=self.routine_repository,
            actuator_state_repository=self.actuator_state_repository,
            actuator_client=self.actuator_client
        )
        
        # Casos de uso (capa de aplicación)
        self.variable_use_case = VariableManagementUseCase(self.variable_repository)
        self.routine_use_case = RoutineManagementUseCase(self.routine_repository)
        self.fuzzy_evaluation = FuzzyEvaluationUseCase()
        self.simulation_use_case = SimulationUseCase()
        
        # Worker principal
        self.fuzzy_worker = FuzzyWorker(
            routine_repository=self.routine_repository,
            actuator_state_repository=self.actuator_state_repository,
            actuator_client=self.actuator_client
        )
        
        # Configurar handler MQTT
        self.mqtt_client.set_message_handler(self.fuzzy_worker.process_reading_batch)
        
        logging.info("DependencyContainer inicializado")
    
    async def initialize_sample_data(self):
        """Inicializa datos de ejemplo para desarrollo."""
        # Variable de ejemplo
        temp_var = Variable(
            id="temp_water",
            name="Temperatura del agua",
            unit="°C",
            description="Temperatura del agua en el sistema hidropónico"
        )
        await self.variable_repository.save(temp_var)
        
        # Rutina de ejemplo
        from domain.models import ThresholdRule, ActuatorMapping
        
        sample_routine = Routine(
            id="temp_control",
            name="Control de temperatura",
            active=True,
            threshold_rules=[
                ThresholdRule(
                    variableId="temp_water",
                    greater_than=25.0,
                    target=70,
                    hold_seconds=30,
                    output_name="cooling_pump"
                )
            ],
            outputs=[
                ActuatorMapping(
                    output_name="cooling_pump",
                    actuatorId="pump_01",
                    esp32Id="esp32_main",
                    actuator_type="on_off",
                    on_threshold=65.0
                )
            ]
        )
        await self.routine_repository.save(sample_routine)
        
        logging.info("Datos de ejemplo inicializados")


# Instancia global del contenedor
container = DependencyContainer()


# ===== Dependency injection para FastAPI =====
def get_variable_use_case() -> VariableManagementUseCase:
    return container.variable_use_case


def get_routine_use_case() -> RoutineManagementUseCase:
    return container.routine_use_case


def get_simulation_use_case() -> SimulationUseCase:
    return container.simulation_use_case


def get_fuzzy_service() -> IFuzzyService:
    return container.fuzzy_service


def get_fuzzy_evaluation_use_case() -> FuzzyEvaluationUseCase:
    return container.fuzzy_evaluation


# ===== Configuración del ciclo de vida =====
@asynccontextmanager
async def lifespan(app: FastAPI):
    """Gestión del ciclo de vida de la aplicación."""
    # Startup
    logging.info("Iniciando fuzzy-service...")
    
    try:
        # Conectar a MongoDB
        await mongo_db.connect()
        
        # Inicializar datos de ejemplo
        await container.initialize_sample_data()
        
        # Iniciar worker
        await container.fuzzy_worker.start()
        
        # Iniciar cliente MQTT en background
        await container.mqtt_client.connect()
        mqtt_task = asyncio.create_task(container.mqtt_client.start_listening())
        
        logging.info("Fuzzy-service iniciado correctamente")
        
        yield
        
    finally:
        # Shutdown
        logging.info("Deteniendo fuzzy-service...")
        
        # Detener MQTT
        await container.mqtt_client.stop_listening()
        await container.mqtt_client.disconnect()
        
        # Detener worker
        await container.fuzzy_worker.stop()
        
        # Cerrar conexión a MongoDB
        await mongo_db.disconnect()
        
        # Cancelar task MQTT
        if 'mqtt_task' in locals():
            mqtt_task.cancel()
            try:
                await mqtt_task
            except asyncio.CancelledError:
                pass
        
        logging.info("Fuzzy-service detenido")


# ===== Configuración de FastAPI =====
app = FastAPI(
    title="Fuzzy Service",
    description="Servicio de control difuso para sistemas hidropónicos",
    version="1.0.0",
    lifespan=lifespan
)

# ===== Middleware Configuration =====
# CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # En producción, especificar dominios específicos
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


# ===== Exception Handlers =====
@app.exception_handler(StarletteHTTPException)
async def http_exception_handler(request: Request, exc: StarletteHTTPException):
    """Manejador global para excepciones HTTP."""
    return JSONResponse(
        status_code=exc.status_code,
        content={
            "error": {
                "type": "http_error",
                "message": exc.detail,
                "status_code": exc.status_code,
                "path": str(request.url.path)
            }
        }
    )


@app.exception_handler(RequestValidationError)
async def validation_exception_handler(request: Request, exc: RequestValidationError):
    """Manejador global para errores de validación de Pydantic."""
    return JSONResponse(
        status_code=422,
        content={
            "error": {
                "type": "validation_error",
                "message": "Error de validación en los datos de entrada",
                "details": exc.errors(),
                "path": str(request.url.path)
            }
        }
    )


@app.exception_handler(Exception)
async def general_exception_handler(request: Request, exc: Exception):
    """Manejador global para excepciones no controladas."""
    logging.error(f"Error no controlado en {request.url.path}: {str(exc)}", exc_info=True)
    return JSONResponse(
        status_code=500,
        content={
            "error": {
                "type": "internal_server_error",
                "message": "Error interno del servidor",
                "path": str(request.url.path)
            }
        }
    )


# Registrar router con dependency overrides
app.include_router(router)

# Configurar dependency injection
app.dependency_overrides[VariableManagementUseCase] = get_variable_use_case
app.dependency_overrides[RoutineManagementUseCase] = get_routine_use_case
app.dependency_overrides[SimulationUseCase] = get_simulation_use_case
app.dependency_overrides[FuzzyEvaluationUseCase] = get_fuzzy_evaluation_use_case
app.dependency_overrides[IFuzzyService] = get_fuzzy_service


if __name__ == "__main__":
    import uvicorn
    
    uvicorn.run(
        "main:app",
        host=settings.host,
        port=settings.port,
        reload=settings.debug
    )
