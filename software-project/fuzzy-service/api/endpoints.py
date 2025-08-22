"""Endpoints de la API REST.

Define todos los endpoints HTTP para gestión de variables, rutinas,
simulación y health checks.
"""

from __future__ import annotations

from typing import Dict, List

from fastapi import APIRouter, Depends, HTTPException, status
from fastapi.security import HTTPBearer

from application.dtos import (
    ActuatorStatusDto,
    FuzzyRuleCreateDto,
    FuzzyRuleResponseDto,
    FuzzyVariableInfoDto,
    ReadingBatch,
    RoutineCreateDto,
    RoutineStateUpdateDto,
    SimulateResponseDto,
    SystemConfigDto,
    SystemConfigUpdateDto,
    SystemMetricsDto,
    VariableCreateDto,
    FuzzySystemCreateDto,
    FuzzySystemResponseDto,
    FuzzySystemUpdateDto,
    FuzzySystemExportDto,
    VariableResponseDto,
    VariableUpdateDto,
    TermCreateDto,
    TermResponseDto,
    TermUpdateDto,
    RoutineResponseDto,
    RoutineUpdateDto,
    ActuatorMappingCreateDto,
    ActuatorMappingResponseDto,
    ActuatorMappingUpdateDto,
)
from application.use_cases import (
    FuzzyEvaluationUseCase,
    RoutineManagementUseCase,
    SimulationUseCase,
    VariableManagementUseCase,
)
from application.interfaces.fuzzy_service import IFuzzyService
from domain.models import Routine, Variable
from api.security import ScopeChecker
from infrastructure.monitoring import PerformanceMonitor  # //optimizado de "sin monitoreo" a "métricas de endpoints" porque permite análisis de uso
from infrastructure.database import mongo_db


# Router principal para todos los endpoints
router = APIRouter()

# Monitor de rendimiento global para endpoints
# //optimizado de "sin métricas" a "monitoreo de endpoints" porque permite análisis de rendimiento de la API
performance_monitor = PerformanceMonitor()

# Security scheme para JWT (opcional)
security = HTTPBearer(auto_error=False)


# ===== Health Check Endpoints =====
@router.get("/healthz", tags=["health"])
async def health_check():
    """Health check básico.
    
    Endpoint simple para verificar que el servicio está funcionando.
    No requiere autenticación.
    """
    return {"status": "healthy", "service": "fuzzy-service"}


@router.get("/readyz", tags=["health"])
async def readiness_check():
    """Readiness check.
    
    Verifica que el servicio está listo para recibir tráfico.
    Incluye verificaciones de dependencias externas como MongoDB.
    """
    health_status = {"status": "ready", "service": "fuzzy-service", "checks": {}}
    
    # Verificar conexión a MongoDB
    try:
        # Intentar hacer ping a MongoDB
        await mongo_db.client.admin.command('ping')
        health_status["checks"]["mongodb"] = {"status": "healthy", "message": "Connected"}
    except Exception as e:
        health_status["checks"]["mongodb"] = {"status": "unhealthy", "message": str(e)}
        health_status["status"] = "not_ready"
    
    # TODO: Agregar verificaciones de MQTT y actuator-service
    
    # Si algún check falla, devolver status 503
    if health_status["status"] == "not_ready":
        raise HTTPException(status_code=503, detail=health_status)
    
    return health_status


# ===== Variable Management Endpoints =====
@router.post(
    "/api/variables",
    response_model=dict,
    status_code=status.HTTP_201_CREATED,
    tags=["variables"]
)
async def create_variable(
    variable_dto: VariableCreateDto,
    variable_use_case: VariableManagementUseCase = Depends(),
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    """Crea una nueva variable.
    
    Requiere scope: fuzzy.write
    """
    try:
        variable_id = await variable_use_case.create_variable(variable_dto)
        return {"id": variable_id, "message": "Variable created successfully"}
    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e))


@router.get(
    "/api/variables",
    response_model=List[Variable],
    tags=["variables"]
)
async def list_variables(
    variable_use_case: VariableManagementUseCase = Depends(),
    _: dict = Depends(ScopeChecker(["fuzzy.read"]))
):
    """Lista todas las variables.
    
    Requiere scope: fuzzy.read
    """
    return await variable_use_case.list_variables()


# ===== Routine Management Endpoints =====
@router.post(
    "/api/routines",
    response_model=dict,
    status_code=status.HTTP_201_CREATED,
    tags=["routines"]
)
async def create_routine(
    routine_dto: RoutineCreateDto,
    routine_use_case: RoutineManagementUseCase = Depends(),
    _: dict = Depends(ScopeChecker(["variable:write"]))
):
    """Crea una nueva rutina.
    
    Requiere scope: fuzzy.write
    """
    try:
        routine_id = await routine_use_case.create_routine(routine_dto)
        return {"id": routine_id, "message": "Routine created successfully"}
    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e))


@router.get(
    "/api/routines",
    response_model=List[Routine],
    tags=["routines"]
)
async def list_routines(
    routine_use_case: RoutineManagementUseCase = Depends(),
    _: dict = Depends(ScopeChecker(["variable:read"]))
):
    """Lista todas las rutinas.
    
    Requiere scope: variable:read
    """
    return await routine_use_case.list_routines()


@router.patch(
    "/api/routines/{routine_id}/state",
    response_model=dict,
    tags=["routines"]
)
async def update_routine_state(
    routine_id: str,
    state_dto: RoutineStateUpdateDto,
    routine_use_case: RoutineManagementUseCase = Depends(),
    _: dict = Depends(ScopeChecker(["variable:write"]))
):
    """Actualiza el estado activo/inactivo de una rutina.
    
    Requiere scope: variable:write
    """
    try:
        await routine_use_case.update_routine_state(routine_id, state_dto)
        return {"message": "Routine state updated successfully"}
    except ValueError as e:
        raise HTTPException(status_code=404, detail=str(e))


# ===== Simulation Endpoint =====
@router.post(
    "/api/simulate",
    response_model=SimulateResponseDto,
    tags=["simulation"]
)
async def simulate_evaluation(
    batch: ReadingBatch,
    simulation_use_case: SimulationUseCase = Depends(),
    routine_use_case: RoutineManagementUseCase = Depends(),
    evaluation_use_case: FuzzyEvaluationUseCase = Depends(),
    _: dict = Depends(ScopeChecker(["variable:read"]))
):
    """Simula la evaluación de un lote de lecturas.
    
    Permite probar la lógica de evaluación sin ejecutar comandos reales.
    Requiere scope: variable:read
    """
    # //optimizado de "sin monitoreo" a "monitoreo de endpoint" porque permite análisis de rendimiento de simulaciones
    with performance_monitor.timing_context("simulate_evaluation_endpoint"):
        # Obtener rutinas activas
        routines = await routine_use_case.list_routines()
        active_routines = [r for r in routines if r.active]
        
        # Simular evaluación
        return await simulation_use_case.simulate(batch, active_routines, evaluation_use_case)


# ===== Actuator Status Endpoint =====
@router.get(
    "/api/actuators/status",
    response_model=List[ActuatorStatusDto],
    tags=["actuators"]
)
async def get_actuator_status(
    fuzzy_service: IFuzzyService = Depends(),
    _: dict = Depends(ScopeChecker(["fuzzy.read"]))
):
    """Obtiene el estado actual de todos los actuadores.
    
    Muestra información de histeresis y cooldown.
    Requiere scope: fuzzy.read
    """
    # //optimizado de "sin monitoreo" a "monitoreo de endpoint" porque permite análisis de consultas de estado
    with performance_monitor.timing_context("get_actuator_status_endpoint"):
        try:
            actuators_data = await fuzzy_service.get_actuators_status()
            return [
                ActuatorStatusDto(
                    actuator_id=data["actuator_id"],
                    last_target=data["last_target"],
                    last_emitted_at=data["last_emitted_at"],
                    is_in_cooldown=data["is_in_cooldown"],
                    cooldown_remaining=data["cooldown_remaining"]
                )
                for data in actuators_data
            ]
        except Exception as e:
            raise HTTPException(
                status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
                detail=f"Error obteniendo estado de actuadores: {str(e)}"
            )


# ===== Fuzzy Rules Management Endpoints =====
@router.get(
    "/api/rules",
    response_model=List[FuzzyRuleResponseDto],
    tags=["fuzzy-rules"]
)
async def list_fuzzy_rules(
    fuzzy_service: IFuzzyService = Depends(),
    _: dict = Depends(ScopeChecker(["variable:read"]))
):
    """Lista todas las reglas difusas activas.
    
    Requiere scope: variable:read
    """
    rules = await fuzzy_service.get_active_rules()
    return [
        FuzzyRuleResponseDto(
            id=rule["rule_id"],
            name=rule.get("name", rule["rule_id"]),
            description=rule.get("description"),
            conditions_text=str(rule["antecedents"]),
            output_variable_id=str(rule["consequent"][0]) if rule["consequent"] else "",
            output_fuzzy_set_name=str(rule["consequent"][1]) if len(rule["consequent"]) > 1 else "",
            priority=1,
            active=True
        )
        for rule in rules
    ]


@router.post(
    "/api/rules",
    response_model=dict,
    status_code=status.HTTP_201_CREATED,
    tags=["fuzzy-rules"]
)
async def create_fuzzy_rule(
    rule_dto: FuzzyRuleCreateDto,
    fuzzy_service: IFuzzyService = Depends(),
    _: dict = Depends(ScopeChecker(["variable:write"]))
):
    """Crea una nueva regla difusa.
    
    Requiere scope: variable:write
    """
    try:
        # Convertir DTO a formato esperado por el servicio
        antecedents = [(cond.variable_id, cond.fuzzy_set_name) for cond in rule_dto.conditions]
        consequent = (rule_dto.output_variable_id, rule_dto.output_fuzzy_set_name)
        
        success = await fuzzy_service.add_fuzzy_rule(
            rule_id=rule_dto.id,
            antecedents=antecedents,
            consequent=consequent,
            weight=1.0
        )
        
        if success:
            return {"id": rule_dto.id, "message": "Fuzzy rule created successfully"}
        else:
            raise HTTPException(status_code=400, detail="Failed to create fuzzy rule")
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))


@router.delete(
    "/api/rules/{rule_id}",
    response_model=dict,
    tags=["fuzzy-rules"]
)
async def delete_fuzzy_rule(
    rule_id: str,
    fuzzy_service: IFuzzyService = Depends(),
    _: dict = Depends(ScopeChecker(["variable:write"]))
):
    """Elimina una regla difusa.
    
    Requiere scope: variable:write
    """
    try:
        success = await fuzzy_service.remove_fuzzy_rule(rule_id)
        if success:
            return {"message": "Fuzzy rule deleted successfully"}
        else:
            raise HTTPException(status_code=404, detail="Fuzzy rule not found")
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))


# ===== Fuzzy Variables Info Endpoint =====
@router.get(
    "/api/fuzzy/variables",
    response_model=Dict[str, FuzzyVariableInfoDto],
    tags=["fuzzy-variables"]
)
async def get_fuzzy_variables_info(
    fuzzy_service: IFuzzyService = Depends(),
    _: dict = Depends(ScopeChecker(["variable:read"]))
):
    """Obtiene información sobre las variables difusas configuradas.
    
    Requiere scope: variable:read
    """
    variables_info = await fuzzy_service.get_fuzzy_variables_info()
    return {
        name: FuzzyVariableInfoDto(
            name=info["name"],
            universe_range=info["universe_range"],
            sets=info["sets"]
        )
        for name, info in variables_info.items()
    }


# ===== System Metrics Endpoint =====
@router.get(
    "/api/metrics",
    response_model=SystemMetricsDto,
    tags=["system"]
)
async def get_system_metrics(
    fuzzy_service: IFuzzyService = Depends(),
    _: dict = Depends(ScopeChecker(["fuzzy.read"]))
):
    """Obtiene métricas del sistema.
    
    Requiere scope: fuzzy.read
    """
    try:
        metrics = await fuzzy_service.get_system_metrics()
        return SystemMetricsDto(
            active_rules_count=metrics["fuzzy_system"]["total_rules"],
            total_evaluations=metrics["evaluations"]["total_evaluations"],
            commands_sent_today=metrics["commands"]["commands_sent_today"],
            average_response_time_ms=metrics["performance"]["average_response_time_ms"],
            system_uptime_seconds=metrics["system"]["uptime_seconds"]
        )
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error obteniendo métricas del sistema: {str(e)}"
        )


# ===== System Configuration Endpoints =====
@router.get(
    "/api/config",
    response_model=SystemConfigDto,
    tags=["system"]
)
async def get_system_config(
    _: dict = Depends(ScopeChecker(["system:read"]))
):
    """Obtiene la configuración actual del sistema.
    
    Requiere scope: system:read
    """
    # TODO: Implementar almacenamiento persistente de configuración
    # Por ahora retornamos valores por defecto
    return SystemConfigDto()


@router.put(
    "/api/config",
    response_model=SystemConfigDto,
    tags=["system"]
)
async def update_system_config(
    config_update: SystemConfigUpdateDto,
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    """Actualiza la configuración del sistema.
    
    Requiere scope: fuzzy.write
    """
    # TODO: Implementar almacenamiento persistente de configuración
    # TODO: Aplicar cambios dinámicamente al sistema
    
    # Por ahora simulamos la actualización
    current_config = SystemConfigDto()
    
    # Aplicar cambios solo a campos no nulos
    update_data = config_update.model_dump(exclude_unset=True)
    updated_config = current_config.model_copy(update=update_data)
    
    return updated_config


# ===== Fuzzy Systems CRUD Endpoints =====
@router.post(
    "/api/fuzzy/systems",
    response_model=FuzzySystemResponseDto,
    status_code=201,
    tags=["systems"]
)
async def create_fuzzy_system(
    system_data: FuzzySystemCreateDto,
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    """Crea un nuevo sistema difuso.
    
    Requiere scope: fuzzy.write
    """
    # TODO: Implementar creación de sistema difuso
    from datetime import datetime
    import uuid
    
    system_id = str(uuid.uuid4())
    return FuzzySystemResponseDto(
        id=system_id,
        name=system_data.name,
        description=system_data.description,
        version=system_data.version,
        status=system_data.status,
        created_at=datetime.now(),
        updated_at=datetime.now(),
        variables_count=0,
        rules_count=0,
        routines_count=0
    )


@router.get(
    "/api/fuzzy/systems",
    response_model=List[FuzzySystemResponseDto],
    tags=["systems"]
)
async def list_fuzzy_systems(
    name: Optional[str] = None,
    status: Optional[str] = None,
    version: Optional[str] = None,
    _: dict = Depends(ScopeChecker(["fuzzy.read"]))
):
    """Lista sistemas difusos con filtros opcionales.
    
    Requiere scope: fuzzy.read
    """
    # TODO: Implementar listado con filtros
    return []


@router.get(
    "/api/fuzzy/systems/{system_id}",
    response_model=FuzzySystemResponseDto,
    tags=["systems"]
)
async def get_fuzzy_system(
    system_id: str,
    _: dict = Depends(ScopeChecker(["fuzzy.read"]))
):
    """Obtiene un sistema difuso por ID.
    
    Requiere scope: fuzzy.read
    """
    # TODO: Implementar obtención por ID
    raise HTTPException(status_code=404, detail="Sistema no encontrado")


@router.put(
    "/api/fuzzy/systems/{system_id}",
    response_model=FuzzySystemResponseDto,
    tags=["systems"]
)
async def update_fuzzy_system(
    system_id: str,
    system_update: FuzzySystemUpdateDto,
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    """Actualiza un sistema difuso (solo en estado draft).
    
    Requiere scope: fuzzy.write
    """
    # TODO: Implementar actualización con validación de estado
    raise HTTPException(status_code=404, detail="Sistema no encontrado")


@router.post(
    "/api/fuzzy/systems/{system_id}/publish",
    response_model=FuzzySystemResponseDto,
    tags=["systems"]
)
async def publish_fuzzy_system(
    system_id: str,
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    """Publica una versión del sistema difuso.
    
    Requiere scope: fuzzy.write
    """
    # TODO: Implementar publicación con validaciones
    raise HTTPException(status_code=404, detail="Sistema no encontrado")


@router.post(
    "/api/fuzzy/systems/{system_id}/export",
    response_model=FuzzySystemExportDto,
    tags=["systems"]
)
async def export_fuzzy_system(
    system_id: str,
    _: dict = Depends(ScopeChecker(["fuzzy.read"]))
):
    """Exporta un sistema difuso como paquete JSON.
    
    Requiere scope: fuzzy.read
    """
    # TODO: Implementar exportación completa
    from datetime import datetime
    return FuzzySystemExportDto(
        system={},
        variables=[],
        rules=[],
        routines=[],
        export_timestamp=datetime.now()
    )


@router.post(
    "/api/fuzzy/systems/import",
    response_model=FuzzySystemResponseDto,
    status_code=201,
    tags=["systems"]
)
async def import_fuzzy_system(
    import_data: FuzzySystemExportDto,
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    """Importa un sistema difuso desde paquete JSON.
    
    Requiere scope: fuzzy.write
    """
    # TODO: Implementar importación con validaciones
    raise HTTPException(status_code=400, detail="Error en importación")


# ===== Variables CRUD Endpoints =====
@router.post(
    "/api/fuzzy/variables",
    response_model=VariableResponseDto,
    status_code=201,
    tags=["variables"]
)
async def create_variable(
    variable_data: VariableCreateDto,
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    """Crea una nueva variable difusa.
    
    Requiere scope: fuzzy.write
    """
    # TODO: Implementar creación de variable
    return VariableResponseDto(
        id=variable_data.id,
        name=variable_data.name,
        unit=variable_data.unit,
        description=variable_data.description,
        fuzzy_sets=[]
    )


@router.get(
    "/api/fuzzy/variables",
    response_model=List[VariableResponseDto],
    tags=["variables"]
)
async def list_variables(
    _: dict = Depends(ScopeChecker(["fuzzy.read"]))
):
    """Lista todas las variables difusas.
    
    Requiere scope: fuzzy.read
    """
    # TODO: Implementar listado de variables
    return []


@router.get(
    "/api/fuzzy/variables/{variable_id}",
    response_model=VariableResponseDto,
    tags=["variables"]
)
async def get_variable(
    variable_id: str,
    _: dict = Depends(ScopeChecker(["fuzzy.read"]))
):
    """Obtiene una variable difusa por ID.
    
    Requiere scope: fuzzy.read
    """
    # TODO: Implementar obtención por ID
    raise HTTPException(status_code=404, detail="Variable no encontrada")


@router.put(
    "/api/fuzzy/variables/{variable_id}",
    response_model=VariableResponseDto,
    tags=["variables"]
)
async def update_variable(
    variable_id: str,
    variable_update: VariableUpdateDto,
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    """Actualiza una variable difusa.
    
    Requiere scope: fuzzy.write
    """
    # TODO: Implementar actualización de variable
    raise HTTPException(status_code=404, detail="Variable no encontrada")


@router.delete(
    "/api/fuzzy/variables/{variable_id}",
    status_code=204,
    tags=["variables"]
)
async def delete_variable(
    variable_id: str,
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    """Elimina una variable difusa.
    
    Requiere scope: fuzzy.write
    """
    # TODO: Implementar eliminación con validaciones
    raise HTTPException(status_code=404, detail="Variable no encontrada")


# ===== Terms (Fuzzy Sets) CRUD Endpoints =====
@router.post(
    "/api/fuzzy/terms",
    response_model=TermResponseDto,
    status_code=201,
    tags=["terms"]
)
async def create_term(
    term_data: TermCreateDto,
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    """Crea un nuevo término (conjunto difuso).
    
    Requiere scope: fuzzy.write
    """
    # TODO: Implementar creación de término
    import uuid
    return TermResponseDto(
        id=str(uuid.uuid4()),
        name=term_data.name,
        membership_type=term_data.membership_type,
        parameters=term_data.parameters,
        description=term_data.description,
        variable_id=term_data.variable_id,
        variable_name="Variable Name"  # TODO: obtener nombre real
    )


@router.get(
    "/api/fuzzy/terms",
    response_model=List[TermResponseDto],
    tags=["terms"]
)
async def list_terms(
    variable_id: Optional[str] = None,
    _: dict = Depends(ScopeChecker(["fuzzy.read"]))
):
    """Lista términos con filtro opcional por variable.
    
    Requiere scope: fuzzy.read
    """
    # TODO: Implementar listado con filtros
    return []


@router.get(
    "/api/fuzzy/terms/{term_id}",
    response_model=TermResponseDto,
    tags=["terms"]
)
async def get_term(
    term_id: str,
    _: dict = Depends(ScopeChecker(["fuzzy.read"]))
):
    """Obtiene un término por ID.
    
    Requiere scope: fuzzy.read
    """
    # TODO: Implementar obtención por ID
    raise HTTPException(status_code=404, detail="Término no encontrado")


@router.put(
    "/api/fuzzy/terms/{term_id}",
    response_model=TermResponseDto,
    tags=["terms"]
)
async def update_term(
    term_id: str,
    term_update: TermUpdateDto,
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    """Actualiza un término.
    
    Requiere scope: fuzzy.write
    """
    # TODO: Implementar actualización de término
    raise HTTPException(status_code=404, detail="Término no encontrado")


@router.delete(
    "/api/fuzzy/terms/{term_id}",
    status_code=204,
    tags=["terms"]
)
async def delete_term(
    term_id: str,
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    """Elimina un término.
    
    Requiere scope: fuzzy.write
    """
    # TODO: Implementar eliminación con validaciones
    raise HTTPException(status_code=404, detail="Término no encontrado")


# ===== Rules CRUD Endpoints =====
@router.post(
    "/api/fuzzy/rules",
    response_model=FuzzyRuleResponseDto,
    status_code=201,
    tags=["rules"]
)
async def create_rule(
    rule_data: FuzzyRuleCreateDto,
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    """Crea una nueva regla difusa.
    
    Requiere scope: fuzzy.write
    """
    # TODO: Implementar creación de regla
    return FuzzyRuleResponseDto(
        id=rule_data.id,
        name=rule_data.name,
        description=rule_data.description,
        conditions_text="Condiciones generadas",  # TODO: generar texto real
        output_variable_id=rule_data.output_variable_id,
        output_fuzzy_set_name=rule_data.output_fuzzy_set_name,
        priority=rule_data.priority,
        active=rule_data.active
    )


@router.get(
    "/api/fuzzy/rules",
    response_model=List[FuzzyRuleResponseDto],
    tags=["rules"]
)
async def list_rules(
    _: dict = Depends(ScopeChecker(["fuzzy.read"]))
):
    """Lista todas las reglas difusas.
    
    Requiere scope: fuzzy.read
    """
    # TODO: Implementar listado de reglas
    return []


@router.get(
    "/api/fuzzy/rules/{rule_id}",
    response_model=FuzzyRuleResponseDto,
    tags=["rules"]
)
async def get_rule(
    rule_id: str,
    _: dict = Depends(ScopeChecker(["fuzzy.read"]))
):
    """Obtiene una regla difusa por ID.
    
    Requiere scope: fuzzy.read
    """
    # TODO: Implementar obtención por ID
    raise HTTPException(status_code=404, detail="Regla no encontrada")


@router.put(
    "/api/fuzzy/rules/{rule_id}",
    response_model=FuzzyRuleResponseDto,
    tags=["rules"]
)
async def update_rule(
    rule_id: str,
    rule_update: FuzzyRuleCreateDto,  # Reutilizamos el DTO de creación
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    """Actualiza una regla difusa.
    
    Requiere scope: fuzzy.write
    """
    # TODO: Implementar actualización de regla
    raise HTTPException(status_code=404, detail="Regla no encontrada")


@router.delete(
    "/api/fuzzy/rules/{rule_id}",
    status_code=204,
    tags=["rules"]
)
async def delete_rule(
    rule_id: str,
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    """Elimina una regla difusa.
    
    Requiere scope: fuzzy.write
    """
    # TODO: Implementar eliminación con validaciones
    raise HTTPException(status_code=404, detail="Regla no encontrada")


# ===== Routines CRUD Endpoints =====
@router.post(
    "/api/fuzzy/routines",
    response_model=RoutineResponseDto,
    status_code=201,
    tags=["routines"]
)
async def create_routine(
    routine_data: RoutineCreateDto,
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    """Crea una nueva rutina.
    
    Requiere scope: fuzzy.write
    """
    # TODO: Implementar creación de rutina
    return RoutineResponseDto(
        id=routine_data.id,
        name=routine_data.name,
        description=None,
        active=routine_data.active,
        fuzzy_rules_count=0,
        threshold_rules_count=len(routine_data.threshold_rules),
        outputs_count=len(routine_data.outputs),
        input_variables=[],
        output_variables=[]
    )


@router.get(
    "/api/fuzzy/routines",
    response_model=List[RoutineResponseDto],
    tags=["routines"]
)
async def list_routines(
    _: dict = Depends(ScopeChecker(["fuzzy.read"]))
):
    """Lista todas las rutinas.
    
    Requiere scope: fuzzy.read
    """
    # TODO: Implementar listado de rutinas
    return []


@router.get(
    "/api/fuzzy/routines/{routine_id}",
    response_model=RoutineResponseDto,
    tags=["routines"]
)
async def get_routine(
    routine_id: str,
    _: dict = Depends(ScopeChecker(["fuzzy.read"]))
):
    """Obtiene una rutina por ID.
    
    Requiere scope: fuzzy.read
    """
    # TODO: Implementar obtención por ID
    raise HTTPException(status_code=404, detail="Rutina no encontrada")


@router.put(
    "/api/fuzzy/routines/{routine_id}",
    response_model=RoutineResponseDto,
    tags=["routines"]
)
async def update_routine(
    routine_id: str,
    routine_update: RoutineUpdateDto,
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    """Actualiza una rutina.
    
    Requiere scope: fuzzy.write
    """
    # TODO: Implementar actualización de rutina
    raise HTTPException(status_code=404, detail="Rutina no encontrada")


@router.delete(
    "/api/fuzzy/routines/{routine_id}",
    status_code=204,
    tags=["routines"]
)
async def delete_routine(
    routine_id: str,
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    """Elimina una rutina.
    
    Requiere scope: fuzzy.write
    """
    # TODO: Implementar eliminación con validaciones
    raise HTTPException(status_code=404, detail="Rutina no encontrada")


# ===== Actuator Mappings CRUD Endpoints =====
@router.post(
    "/api/fuzzy/actuator-mappings",
    response_model=ActuatorMappingResponseDto,
    status_code=201,
    tags=["actuator-mappings"]
)
async def create_actuator_mapping(
    mapping_data: ActuatorMappingCreateDto,
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    """Crea un nuevo mapeo de actuador.
    
    Requiere scope: fuzzy.write
    """
    # TODO: Implementar creación de mapeo
    from datetime import datetime
    import uuid
    
    return ActuatorMappingResponseDto(
        id=str(uuid.uuid4()),
        output_name=mapping_data.output_name,
        actuatorId=mapping_data.actuatorId,
        esp32Id=mapping_data.esp32Id,
        actuator_type=mapping_data.actuator_type,
        on_threshold=mapping_data.on_threshold,
        description=mapping_data.description,
        created_at=datetime.now(),
        updated_at=datetime.now()
    )


@router.get(
    "/api/fuzzy/actuator-mappings",
    response_model=List[ActuatorMappingResponseDto],
    tags=["actuator-mappings"]
)
async def list_actuator_mappings(
    _: dict = Depends(ScopeChecker(["fuzzy.read"]))
):
    """Lista todos los mapeos de actuadores.
    
    Requiere scope: fuzzy.read
    """
    # TODO: Implementar listado de mapeos
    return []


@router.get(
    "/api/fuzzy/actuator-mappings/{mapping_id}",
    response_model=ActuatorMappingResponseDto,
    tags=["actuator-mappings"]
)
async def get_actuator_mapping(
    mapping_id: str,
    _: dict = Depends(ScopeChecker(["fuzzy.read"]))
):
    """Obtiene un mapeo de actuador por ID.
    
    Requiere scope: fuzzy.read
    """
    # TODO: Implementar obtención por ID
    raise HTTPException(status_code=404, detail="Mapeo no encontrado")


@router.put(
    "/api/fuzzy/actuator-mappings/{mapping_id}",
    response_model=ActuatorMappingResponseDto,
    tags=["actuator-mappings"]
)
async def update_actuator_mapping(
    mapping_id: str,
    mapping_update: ActuatorMappingUpdateDto,
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    """Actualiza un mapeo de actuador.
    
    Requiere scope: fuzzy.write
    """
    # TODO: Implementar actualización de mapeo
    raise HTTPException(status_code=404, detail="Mapeo no encontrado")


@router.delete(
    "/api/fuzzy/actuator-mappings/{mapping_id}",
    status_code=204,
    tags=["actuator-mappings"]
)
async def delete_actuator_mapping(
    mapping_id: str,
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    """Elimina un mapeo de actuador.
    
    Requiere scope: fuzzy.write
    """
    # TODO: Implementar eliminación con validaciones
    raise HTTPException(status_code=404, detail="Mapeo no encontrado")


# ===== Performance Monitoring Endpoints =====
@router.get(
    "/api/performance/stats",
    tags=["monitoring"]
)
async def get_performance_stats(
    evaluation_use_case: FuzzyEvaluationUseCase = Depends(),
    _: dict = Depends(ScopeChecker(["fuzzy.read"]))
):
    """Obtiene estadísticas de rendimiento del sistema.
    
    Incluye métricas de endpoints, casos de uso y motor fuzzy.
    Requiere scope: fuzzy.read
    """
    # //optimizado de "sin estadísticas" a "estadísticas de rendimiento" porque permite monitoreo del sistema
    endpoint_stats = performance_monitor.get_stats()
    use_case_stats = evaluation_use_case.get_performance_stats()
    
    return {
        "endpoint_metrics": endpoint_stats,
        "use_case_metrics": use_case_stats,
        "timestamp": performance_monitor._start_time
    }


@router.post(
    "/api/performance/clear-cache",
    status_code=204,
    tags=["monitoring"]
)
async def clear_performance_cache(
    evaluation_use_case: FuzzyEvaluationUseCase = Depends(),
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    """Limpia los cachés de rendimiento del sistema.
    
    Útil para reiniciar métricas o liberar memoria.
    Requiere scope: fuzzy.write
    """
    # //optimizado de "sin limpieza" a "limpieza de cachés" porque permite gestión de memoria
    evaluation_use_case.clear_caches()
    performance_monitor.clear_metrics()
    
    return None