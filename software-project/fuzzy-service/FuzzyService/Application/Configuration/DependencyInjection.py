from __future__ import annotations

"""
Configuración de inyección de dependencias para la capa de Application.
- Registra Medyator dentro del contenedor kink
- Punto único para registrar handlers de Commands/Queries cuando existan
"""

import logging
from typing import Callable, Awaitable

from kink import di

# Importa la extensión para que el contenedor tenga di.add_medyator()
# (el simple import aplica la extensión al contenedor)
import medyator.kink  # noqa: F401
from medyator import Medyator

_logger = logging.getLogger(__name__)


def configure_application_di() -> None:
    """Registra los servicios de Application en el contenedor DI.

    - Inicializa Medyator en kink
    - Registra (en el futuro) behaviors/middlewares y handlers de Commands/Queries
    """
    # Registra una instancia de Medyator dentro del contenedor kink
    # Nota: di.add_medyator() proviene de la extensión importada arriba
    di.add_medyator()

    # Registrar handlers explícitamente en el contenedor, usando el tipo de request como clave
    from FuzzyService.Application.Features.FuzzySystems.Commands.CreateFuzzySystem import (
        CreateFuzzySystemCommand,
        CreateFuzzySystemHandler,
    )
    from FuzzyService.Application.Features.FuzzySystems.Queries.GetAllFuzzySystems import (
        GetAllFuzzySystemsQuery,
        GetAllFuzzySystemsHandler,
    )
    from FuzzyService.Application.Features.FuzzySystems.Commands.UpdateFuzzySystem import (
        UpdateFuzzySystemCommand,
        UpdateFuzzySystemHandler,
    )
    from FuzzyService.Application.Features.FuzzySystems.Commands.DeleteFuzzySystem import (
        DeleteFuzzySystemCommand,
        DeleteFuzzySystemHandler,
    )
    from FuzzyService.Application.Features.FuzzySystems.Commands.UpdateFuzzySystemStatus import (
        UpdateFuzzySystemStatusCommand,
        UpdateFuzzySystemStatusHandler,
    )
    from FuzzyService.Application.Features.FuzzySystems.Queries.GetFuzzySystemById import (
        GetFuzzySystemByIdQuery,
        GetFuzzySystemByIdHandler,
    )

    # FuzzyVariables - registrar comandos/queries y handlers
    from FuzzyService.Application.Features.FuzzyVariables.Commands import (
        CreateFuzzyVariableCommand,
        UpdateFuzzyVariableCommand,
        DeleteFuzzyVariableCommand,
    )
    from FuzzyService.Application.Features.FuzzyVariables.Queries import (
        GetAllFuzzyVariablesQuery,
        GetFuzzyVariableByIdQuery,
        GetFuzzyVariablesBySystemQuery,
    )
    from FuzzyService.Application.Features.FuzzyVariables.Handlers import (
        CreateFuzzyVariableHandler,
        UpdateFuzzyVariableHandler,
        DeleteFuzzyVariableHandler,
        GetAllFuzzyVariablesHandler,
        GetFuzzyVariableByIdHandler,
        GetFuzzyVariablesBySystemHandler,
    )

    # Handlers (se importarán una vez creados)
    from FuzzyService.Application.Features.FuzzyVariables.Handlers import (
        CreateFuzzyVariableHandler,
    )
    from FuzzyService.Application.Features.FuzzyVariables.Handlers import (
        UpdateFuzzyVariableHandler,
    )
    from FuzzyService.Application.Features.FuzzyVariables.Handlers import (
        DeleteFuzzyVariableHandler,
    )
    from FuzzyService.Application.Features.FuzzyVariables.Handlers import (
        GetAllFuzzyVariablesHandler,
    )
    from FuzzyService.Application.Features.FuzzyVariables.Handlers import (
        GetFuzzyVariableByIdHandler,
    )

    di[CreateFuzzySystemCommand] = CreateFuzzySystemHandler()
    di[UpdateFuzzySystemCommand] = UpdateFuzzySystemHandler()
    di[DeleteFuzzySystemCommand] = DeleteFuzzySystemHandler()
    di[GetAllFuzzySystemsQuery] = GetAllFuzzySystemsHandler()
    di[GetFuzzySystemByIdQuery] = GetFuzzySystemByIdHandler()
    di[UpdateFuzzySystemStatusCommand] = UpdateFuzzySystemStatusHandler()

    # Bind de FuzzyVariables
    di[CreateFuzzyVariableCommand] = CreateFuzzyVariableHandler()
    di[UpdateFuzzyVariableCommand] = UpdateFuzzyVariableHandler()
    di[DeleteFuzzyVariableCommand] = DeleteFuzzyVariableHandler()
    di[GetAllFuzzyVariablesQuery] = GetAllFuzzyVariablesHandler()
    di[GetFuzzyVariableByIdQuery] = GetFuzzyVariableByIdHandler()
    di[GetFuzzyVariablesBySystemQuery] = GetFuzzyVariablesBySystemHandler()
    
    # FuzzyVariables - Term management handlers
    from FuzzyService.Application.Features.FuzzyVariables.Commands.AddTermToVariableCommand import AddTermToVariableCommand
    from FuzzyService.Application.Features.FuzzyVariables.Commands.RemoveTermFromVariableCommand import RemoveTermFromVariableCommand
    from FuzzyService.Application.Features.FuzzyVariables.Handlers.AddTermToVariableHandler import AddTermToVariableHandler
    from FuzzyService.Application.Features.FuzzyVariables.Handlers.RemoveTermFromVariableHandler import RemoveTermFromVariableHandler
    
    di[AddTermToVariableCommand] = AddTermToVariableHandler()
    di[RemoveTermFromVariableCommand] = RemoveTermFromVariableHandler()

    # FuzzyTerms - registrar comandos/queries y handlers
    from FuzzyService.Application.Features.FuzzyTerms.Commands import (
        CreateFuzzyTermCommand,
        UpdateFuzzyTermCommand,
        DeleteFuzzyTermCommand,
    )
    from FuzzyService.Application.Features.FuzzyTerms.Queries import (
        GetAllFuzzyTermsQuery,
        GetFuzzyTermByIdQuery,
    )
    from FuzzyService.Application.Features.FuzzyTerms.Handlers import (
        CreateFuzzyTermHandler,
        UpdateFuzzyTermHandler,
        DeleteFuzzyTermHandler,
        GetAllFuzzyTermsHandler,
        GetFuzzyTermByIdHandler,
    )

    di[CreateFuzzyTermCommand] = CreateFuzzyTermHandler()
    di[UpdateFuzzyTermCommand] = UpdateFuzzyTermHandler()
    di[DeleteFuzzyTermCommand] = DeleteFuzzyTermHandler()
    di[GetAllFuzzyTermsQuery] = GetAllFuzzyTermsHandler()
    di[GetFuzzyTermByIdQuery] = GetFuzzyTermByIdHandler()

    # FuzzyRules handlers
    from FuzzyService.Application.Features.FuzzyRules.Commands.CreateFuzzyRuleCommand import CreateFuzzyRuleCommand
    from FuzzyService.Application.Features.FuzzyRules.Commands.UpdateFuzzyRuleCommand import UpdateFuzzyRuleCommand
    from FuzzyService.Application.Features.FuzzyRules.Commands.DeleteFuzzyRuleCommand import DeleteFuzzyRuleCommand
    from FuzzyService.Application.Features.FuzzyRules.Commands.AddConditionToRuleCommand import AddConditionToRuleCommand
    from FuzzyService.Application.Features.FuzzyRules.Commands.RemoveConditionFromRuleCommand import RemoveConditionFromRuleCommand
    from FuzzyService.Application.Features.FuzzyRules.Commands.UpdateRuleConnectorsCommand import UpdateRuleConnectorsCommand
    from FuzzyService.Application.Features.FuzzyRules.Commands.UpdateRuleConsequentCommand import UpdateRuleConsequentCommand
    from FuzzyService.Application.Features.FuzzyRules.Queries.GetAllFuzzyRulesQuery import GetAllFuzzyRulesQuery
    from FuzzyService.Application.Features.FuzzyRules.Queries.GetFuzzyRuleByIdQuery import GetFuzzyRuleByIdQuery
    from FuzzyService.Application.Features.FuzzyRules.Queries.GetFuzzyRulesBySystemQuery import GetFuzzyRulesBySystemQuery
    from FuzzyService.Application.Features.FuzzyRules.Handlers.CreateFuzzyRuleHandler import CreateFuzzyRuleHandler
    from FuzzyService.Application.Features.FuzzyRules.Handlers.UpdateFuzzyRuleHandler import UpdateFuzzyRuleHandler
    from FuzzyService.Application.Features.FuzzyRules.Handlers.DeleteFuzzyRuleHandler import DeleteFuzzyRuleHandler
    from FuzzyService.Application.Features.FuzzyRules.Handlers.AddConditionToRuleHandler import AddConditionToRuleHandler
    from FuzzyService.Application.Features.FuzzyRules.Handlers.RemoveConditionFromRuleHandler import RemoveConditionFromRuleHandler
    from FuzzyService.Application.Features.FuzzyRules.Handlers.UpdateRuleConnectorsHandler import UpdateRuleConnectorsHandler
    from FuzzyService.Application.Features.FuzzyRules.Handlers.UpdateRuleConsequentHandler import UpdateRuleConsequentHandler
    from FuzzyService.Application.Features.FuzzyRules.Handlers.GetAllFuzzyRulesHandler import GetAllFuzzyRulesHandler
    from FuzzyService.Application.Features.FuzzyRules.Handlers.GetFuzzyRuleByIdHandler import GetFuzzyRuleByIdHandler
    from FuzzyService.Application.Features.FuzzyRules.Handlers.GetFuzzyRulesBySystemHandler import GetFuzzyRulesBySystemHandler

    di[CreateFuzzyRuleCommand] = CreateFuzzyRuleHandler()
    di[UpdateFuzzyRuleCommand] = UpdateFuzzyRuleHandler()
    di[DeleteFuzzyRuleCommand] = DeleteFuzzyRuleHandler()
    di[AddConditionToRuleCommand] = AddConditionToRuleHandler()
    di[RemoveConditionFromRuleCommand] = RemoveConditionFromRuleHandler()
    di[UpdateRuleConnectorsCommand] = UpdateRuleConnectorsHandler()
    di[UpdateRuleConsequentCommand] = UpdateRuleConsequentHandler()
    di[GetAllFuzzyRulesQuery] = GetAllFuzzyRulesHandler()
    di[GetFuzzyRuleByIdQuery] = GetFuzzyRuleByIdHandler()
    di[GetFuzzyRulesBySystemQuery] = GetFuzzyRulesBySystemHandler()

    # FuzzyRoutines handlers
    from FuzzyService.Application.Features.FuzzyRoutines.Commands.CreateFuzzyRoutineCommand import CreateFuzzyRoutineCommand
    from FuzzyService.Application.Features.FuzzyRoutines.Commands.UpdateFuzzyRoutineCommand import UpdateFuzzyRoutineCommand
    from FuzzyService.Application.Features.FuzzyRoutines.Commands.DeleteFuzzyRoutineCommand import DeleteFuzzyRoutineCommand
    from FuzzyService.Application.Features.FuzzyRoutines.Commands.AddStepToRoutineCommand import AddStepToRoutineCommand
    from FuzzyService.Application.Features.FuzzyRoutines.Commands.UpdateStepInRoutineCommand import UpdateStepInRoutineCommand
    from FuzzyService.Application.Features.FuzzyRoutines.Commands.DeleteStepFromRoutineCommand import DeleteStepFromRoutineCommand
    from FuzzyService.Application.Features.FuzzyRoutines.Queries.GetAllFuzzyRoutinesQuery import GetAllFuzzyRoutinesQuery
    from FuzzyService.Application.Features.FuzzyRoutines.Queries.GetFuzzyRoutineByIdQuery import GetFuzzyRoutineByIdQuery
    from FuzzyService.Application.Features.FuzzyRoutines.Handlers.CreateFuzzyRoutineHandler import CreateFuzzyRoutineHandler
    from FuzzyService.Application.Features.FuzzyRoutines.Handlers.UpdateFuzzyRoutineHandler import UpdateFuzzyRoutineHandler
    from FuzzyService.Application.Features.FuzzyRoutines.Handlers.DeleteFuzzyRoutineHandler import DeleteFuzzyRoutineHandler
    from FuzzyService.Application.Features.FuzzyRoutines.Handlers.AddStepToRoutineHandler import AddStepToRoutineHandler
    from FuzzyService.Application.Features.FuzzyRoutines.Handlers.UpdateStepInRoutineHandler import UpdateStepInRoutineHandler
    from FuzzyService.Application.Features.FuzzyRoutines.Handlers.DeleteStepFromRoutineHandler import DeleteStepFromRoutineHandler
    from FuzzyService.Application.Features.FuzzyRoutines.Handlers.GetAllFuzzyRoutinesHandler import GetAllFuzzyRoutinesHandler
    from FuzzyService.Application.Features.FuzzyRoutines.Handlers.GetFuzzyRoutineByIdHandler import GetFuzzyRoutineByIdHandler

    di[CreateFuzzyRoutineCommand] = CreateFuzzyRoutineHandler()
    di[UpdateFuzzyRoutineCommand] = UpdateFuzzyRoutineHandler()
    di[DeleteFuzzyRoutineCommand] = DeleteFuzzyRoutineHandler()
    di[AddStepToRoutineCommand] = AddStepToRoutineHandler()
    di[UpdateStepInRoutineCommand] = UpdateStepInRoutineHandler()
    di[DeleteStepFromRoutineCommand] = DeleteStepFromRoutineHandler()
    di[GetAllFuzzyRoutinesQuery] = GetAllFuzzyRoutinesHandler()
    di[GetFuzzyRoutineByIdQuery] = GetFuzzyRoutineByIdHandler()

    # FuzzyEvaluations handlers
    from FuzzyService.Application.Features.FuzzyEvaluations.Queries.GetAllFuzzyEvaluationsQuery import GetAllFuzzyEvaluationsQuery
    from FuzzyService.Application.Features.FuzzyEvaluations.Queries.GetFuzzyEvaluationByIdQuery import GetFuzzyEvaluationByIdQuery
    from FuzzyService.Application.Features.FuzzyEvaluations.Queries.GetFuzzyEvaluationsBySystemQuery import GetFuzzyEvaluationsBySystemQuery
    from FuzzyService.Application.Features.FuzzyEvaluations.Handlers.GetAllFuzzyEvaluationsHandler import GetAllFuzzyEvaluationsHandler
    from FuzzyService.Application.Features.FuzzyEvaluations.Handlers.GetFuzzyEvaluationByIdHandler import GetFuzzyEvaluationByIdHandler
    from FuzzyService.Application.Features.FuzzyEvaluations.Handlers.GetFuzzyEvaluationsBySystemHandler import GetFuzzyEvaluationsBySystemHandler

    di[GetAllFuzzyEvaluationsQuery] = GetAllFuzzyEvaluationsHandler()
    di[GetFuzzyEvaluationByIdQuery] = GetFuzzyEvaluationByIdHandler()
    di[GetFuzzyEvaluationsBySystemQuery] = GetFuzzyEvaluationsBySystemHandler()

    # MQTT Sensor Processing handlers
    from FuzzyService.Application.Features.SensorProcessing.Commands.ProcessSensorReadingsCommand import ProcessSensorReadingsCommand
    from FuzzyService.Application.Features.SensorProcessing.Handlers.ProcessSensorReadingsHandler import ProcessSensorReadingsHandler
    
    di[ProcessSensorReadingsCommand] = ProcessSensorReadingsHandler()

    # Actuator Integration handlers
    from FuzzyService.Application.Features.ActuatorIntegration.Commands.SendRoutinesToActuatorCommand import SendRoutinesToActuatorCommand
    from FuzzyService.Application.Features.ActuatorIntegration.Handlers.SendRoutinesToActuatorHandler import SendRoutinesToActuatorHandler
    
    di[SendRoutinesToActuatorCommand] = SendRoutinesToActuatorHandler()

    # FuzzyEngine Service - Domain service implementation
    from FuzzyService.Domain.Interfaces.IFuzzyEngine import IFuzzyEngine
    from FuzzyService.Application.Services.NullFuzzyEngine import NullFuzzyEngine

    # Binding perezoso mediante factory para permitir reemplazo en infraestructura/configuración
    # Solo enlazar NullFuzzyEngine si no existe ya un binding previo (hecho por Infrastructure)
    try:
        existing_engine = di[IFuzzyEngine]
    except Exception:
        existing_engine = None
    if existing_engine is None:
        di[IFuzzyEngine] = lambda di: NullFuzzyEngine()
        _logger.info("Application DI: IFuzzyEngine no estaba configurado, enlazado a NullFuzzyEngine (fallback).")
    else:
        _logger.info("Application DI: IFuzzyEngine ya estaba configurado por otra capa; se respeta el binding existente.")

    # Exponer también acceso directo al mediador
    di["mediator"] = di[Medyator]

    _logger.info("Application DI configured: Medyator registered in kink and handlers wired.")


async def on_app_startup() -> None:
    """Hook opcional para inicialización de Application (si se requiere async)."""
    init: Callable[[], Awaitable] | None = None
    if init:
        await init()


async def on_app_shutdown() -> None:
    """Hook opcional para limpieza de Application (si se requiere async)."""
    close: Callable[[], Awaitable] | None = None
    if close:
        await close()
