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
    from FuzzyService.Application.Features.FuzzyRules.Queries.GetAllRulesNameDescriptionQuery import GetAllRulesNameDescriptionQuery
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
    from FuzzyService.Application.Features.FuzzyRules.Handlers.GetAllRulesNameDescriptionHandler import GetAllRulesNameDescriptionHandler

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
    di[GetAllRulesNameDescriptionQuery] = GetAllRulesNameDescriptionHandler()

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
    from FuzzyService.Application.Features.ActuatorIntegration.Commands.SendCommandsToActuatorCommand import SendCommandsToActuatorCommand
    from FuzzyService.Application.Features.ActuatorIntegration.Handlers.SendCommandsToActuatorHandler import SendCommandsToActuatorHandler

    di[SendCommandsToActuatorCommand] = SendCommandsToActuatorHandler()

    # FuzzyEngine Service - Domain service implementation
    from FuzzyService.Domain.Interfaces.IFuzzyEngine import IFuzzyEngine

    # Verificar que IFuzzyEngine esté configurado por la capa de Infrastructure
    try:
        di[IFuzzyEngine]  # Solo verificamos que existe
        _logger.info("Application DI: IFuzzyEngine ya estaba configurado por otra capa; se respeta el binding existente.")
    except Exception:
        _logger.error("Application DI: IFuzzyEngine no está configurado. Debe ser configurado por la capa de Infrastructure.")
        raise ValueError("IFuzzyEngine must be configured by Infrastructure layer")

    # Exponer también acceso directo al mediador
    di["mediator"] = di[Medyator]

    _logger.info("Application DI configured: Medyator registered in kink and handlers wired.")


# Global lifecycle hooks (configurable externally)
_init_hook: Callable[[], Awaitable[None]] | None = None
_close_hook: Callable[[], Awaitable[None]] | None = None


def register_lifecycle_hooks(
    init: Callable[[], Awaitable[None]] | None = None,
    close: Callable[[], Awaitable[None]] | None = None
) -> None:
    """Register optional lifecycle hooks for application startup and shutdown.
    
    Args:
        init: Optional async function to run during application startup
        close: Optional async function to run during application shutdown
        
    Example:
        async def custom_init():
            print("Custom initialization")
            
        async def custom_cleanup():
            print("Custom cleanup")
            
        register_lifecycle_hooks(init=custom_init, close=custom_cleanup)
    """
    global _init_hook, _close_hook
    _init_hook = init
    _close_hook = close
    _logger.info("Lifecycle hooks registered (init: %s, close: %s)", 
                 init is not None, close is not None)


async def on_app_startup() -> None:
    """Hook opcional para inicialización de Application (si se requiere async).
    
    Este hook ejecuta la función de inicialización registrada mediante
    `register_lifecycle_hooks()`, si existe.
    
    Nota para análisis estático (SonarQube, mypy):
        - El hook `_init_hook` es configurable globalmente, no una constante.
        - Se usa `is not None` para verificación explícita de tipo.
        - Esto evita falsos positivos de "always False" en analizadores estáticos.
    """
    if _init_hook is not None:
        await _init_hook()


async def on_app_shutdown() -> None:
    """Hook opcional para limpieza de Application (si se requiere async).
    
    Este hook ejecuta la función de limpieza registrada mediante
    `register_lifecycle_hooks()`, si existe.
    
    Nota para análisis estático (SonarQube, mypy):
        - El hook `_close_hook` es configurable globalmente, no una constante.
        - Se usa `is not None` para verificación explícita de tipo.
        - Esto evita falsos positivos de "always False" en analizadores estáticos.
    """
    if _close_hook is not None:
        await _close_hook()
