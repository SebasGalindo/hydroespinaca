"""
Dependency Injection configuration for Infrastructure using kink.
- Registers Mongo settings, client factory, and database accessors
- Provides helper functions to wire up on FastAPI startup/shutdown
"""
from __future__ import annotations

import logging
import os
from typing import Callable, Awaitable, List

from kink import di

from . import DatabaseConfiguration as db

from FuzzyService.Domain.Interfaces.IFuzzyEngine import IFuzzyEngine

# Repos y servicios base
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository
from FuzzyService.Domain.Interfaces.IFuzzyRoutineRepository import IFuzzyRoutineRepository
from FuzzyService.Domain.Interfaces.IFuzzyEvaluationRepository import IFuzzyEvaluationRepository



_logger = logging.getLogger(__name__)


def configure_infrastructure_di() -> None:
    """Registers infrastructure services in the DI container."""
    # Settings as a singleton value
    di["mongo_settings"] = db.get_settings()

    # Async factories for client/database access
    di["mongo_init"] = db.init_mongo
    di["mongo_close"] = db.close_mongo
    di["mongo_db"] = db.get_database
    di["mongo_coll"] = db.get_collection

    # MQTT Configuration and Services
    from FuzzyService.Infrastructure.Configuration.ExternalServicesConfiguration import get_mqtt_settings
    from FuzzyService.Infrastructure.ExternalServices.MqttService.MqttClient import MqttClient
    from FuzzyService.Infrastructure.ExternalServices.MqttService.MqttMessageHandler import MqttMessageHandler
    from FuzzyService.Infrastructure.ExternalServices.MqttService.MqttSubscriber import MqttSubscriber
    from FuzzyService.Domain.Interfaces.IMqttService import IMqttService
    from medyator import Medyator
    
    mqtt_settings = get_mqtt_settings()
    di["mqtt_settings"] = mqtt_settings
    
    # MQTT services - will be initialized in startup
    di["mqtt_client"] = None  # Will be set in startup
    di["mqtt_subscriber"] = None  # Will be set in startup
    di["mqtt_handler"] = None  # Will be set in startup

    # Bind IFuzzyEngine directly with ScikitFuzzyEngine
    try:
        from FuzzyService.Domain.Interfaces.IFuzzyEngine import IFuzzyEngine
        
        from FuzzyService.Infrastructure.ExternalServices.FuzzyEngine.ScikitFuzzyEngine import ScikitFuzzyEngine
        from medyator import Medyator

        # Create instance of ScikitFuzzyEngine with temporary Medyator
        # Note: This will be replaced by the proper Medyator when Application DI configures it
        temp_mediator = Medyator(di)
        scikit_engine_instance = ScikitFuzzyEngine(temp_mediator)
        
        # Register ScikitFuzzyEngine instance as IFuzzyEngine
        di["scikit_engine"] = scikit_engine_instance
        di[IFuzzyEngine] = scikit_engine_instance
        
        _logger.info("IFuzzyEngine bound to ScikitFuzzyEngine instance with mediator.")
    except Exception:
        _logger.exception("Failed to bind IFuzzyEngine to ScikitFuzzyEngine.")
        raise  # Re-raise the exception since NullFuzzyEngine is no longer available

    _logger.info("Infrastructure DI configured: mongo settings, MQTT settings, IFuzzyEngine and factories registered.")


async def on_startup() -> None:
    """Hook to initialize infrastructure resources (e.g., Mongo)."""
    init_fn: Callable[[], Awaitable] = di["mongo_init"]
    await init_fn()

    # Instantiate repositories and optionally ensure indexes after Mongo is ready
    try:
        from FuzzyService.Infrastructure.Persistence.Repositories.FuzzySystemRepository import FuzzySystemRepository
        from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
        from FuzzyService.Infrastructure.Persistence.Repositories.FuzzyVariableRepository import FuzzyVariableRepository
        from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
        from FuzzyService.Infrastructure.Persistence.Repositories.FuzzyTermRepository import FuzzyTermRepository
        from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository

        systems_coll = db.get_collection("systems")
        variables_coll = db.get_collection("variables")
        terms_coll = db.get_collection("terms")

        repo_fs = FuzzySystemRepository(systems_coll)
        repo_var = FuzzyVariableRepository(variables_coll)
        repo_term = FuzzyTermRepository(terms_coll)

        # Bind by interface (preferred) and keep string alias for convenience
        di[IFuzzySystemRepository] = repo_fs
        di["repo_fuzzy_system"] = repo_fs

        di[IFuzzyVariableRepository] = repo_var
        di["repo_fuzzy_variable"] = repo_var

        di[IFuzzyTermRepository] = repo_term
        di["repo_fuzzy_term"] = repo_term

        # Optional repositories: Rule, Routine, Evaluation
        optional_repos: List[object] = []

        # FuzzyRuleRepository (optional)
        try:
            from FuzzyService.Infrastructure.Persistence.Repositories.FuzzyRuleRepository import FuzzyRuleRepository  # type: ignore
            from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository  # type: ignore

            rules_coll = db.get_collection("rules")
            repo_rule = FuzzyRuleRepository(rules_coll)
            di[IFuzzyRuleRepository] = repo_rule
            di["repo_fuzzy_rule"] = repo_rule
            optional_repos.append(repo_rule)
            _logger.info("FuzzyRuleRepository initialized and registered in DI.")
        except Exception as ex:
            _logger.warning("Optional FuzzyRuleRepository not initialized: %s", ex)

        # FuzzyRoutineRepository (optional)
        try:
            from FuzzyService.Infrastructure.Persistence.Repositories.FuzzyRoutineRepository import FuzzyRoutineRepository  # type: ignore
            from FuzzyService.Domain.Interfaces.IFuzzyRoutineRepository import IFuzzyRoutineRepository  # type: ignore

            routines_coll = db.get_collection("routines")
            repo_routine = FuzzyRoutineRepository(routines_coll)
            di[IFuzzyRoutineRepository] = repo_routine
            di["repo_fuzzy_routine"] = repo_routine
            optional_repos.append(repo_routine)
            _logger.info("FuzzyRoutineRepository initialized and registered in DI.")
        except Exception as ex:
            _logger.warning("Optional FuzzyRoutineRepository not initialized: %s", ex)

        # FuzzyEvaluationRepository (optional)
        try:
            from FuzzyService.Infrastructure.Persistence.Repositories.FuzzyEvaluationRepository import FuzzyEvaluationRepository  # type: ignore
            from FuzzyService.Domain.Interfaces.IFuzzyEvaluationRepository import IFuzzyEvaluationRepository  # type: ignore

            evaluations_coll = db.get_collection("evaluations")
            repo_eval = FuzzyEvaluationRepository(evaluations_coll)
            di[IFuzzyEvaluationRepository] = repo_eval
            di["repo_fuzzy_evaluation"] = repo_eval
            optional_repos.append(repo_eval)
            _logger.info("FuzzyEvaluationRepository initialized and registered in DI.")
        except Exception as ex:
            _logger.warning("Optional FuzzyEvaluationRepository not initialized: %s", ex)

        # IFuzzyEngine is now configured in configure_infrastructure_di()

        # Bind IActuatorService to concrete ActuatorService (HTTP client)
        try:
            from FuzzyService.Domain.Interfaces.IActuatorService import IActuatorService
            from FuzzyService.Infrastructure.ExternalServices.ActuatorService.ActuatorService import ActuatorService

            base_url = os.getenv("FUZZY_ACTUATOR_SERVICE_URL") or os.getenv("ACTUATOR_SERVICE_URL") or "http://localhost:5002"
            timeout_env = os.getenv("FUZZY_ACTUATOR_SERVICE_TIMEOUT") or os.getenv("ACTUATOR_SERVICE_TIMEOUT")
            try:
                timeout = float(timeout_env) if timeout_env else 30.0
            except Exception:
                timeout = 30.0

            actuator_service = ActuatorService(base_url=base_url, timeout=timeout)

            # Optional: override send routines endpoint from env if provided
            send_ep = os.getenv("FUZZY_ACTUATOR_SERVICE_SEND_ROUTINES_ENDPOINT") or os.getenv("ACTUATOR_SERVICE_SEND_ROUTINES_ENDPOINT")
            if send_ep:
                actuator_service.endpoint = send_ep if send_ep.startswith("/") else f"/{send_ep}"

            di[IActuatorService] = actuator_service
            di["actuator_service"] = actuator_service
            _logger.info("IActuatorService bound to ActuatorService (base_url=%s, timeout=%s).", base_url, timeout)
        except Exception:
            _logger.exception("Failed to bind IActuatorService to ActuatorService.")

        ensure_indexes_env = os.getenv("FUZZY_ENSURE_INDEXES_ON_STARTUP", "true").strip().lower()
        ensure_indexes = ensure_indexes_env in ("1", "true", "yes", "y", "on")
        if ensure_indexes:
            await repo_fs.ensure_indexes()
            await repo_var.ensure_indexes()
            await repo_term.ensure_indexes()
            # Ensure indexes for optional repos if they are available
            for r in optional_repos:
                try:
                    await r.ensure_indexes()  # type: ignore[attr-defined]
                except Exception:
                    _logger.exception("Failed ensuring indexes for optional repository: %s", type(r).__name__)
            _logger.info("Repositories initialized and indexes ensured (startup gating enabled).")
        else:
            _logger.info(
                "Repositories initialized. Skipping ensure_indexes on startup (FUZZY_ENSURE_INDEXES_ON_STARTUP=%s)",
                ensure_indexes_env,
            )

        # Run idempotent seed after indexes (gated by FUZZY_SEED_ON_STARTUP)
        await _seed_initial_data(di)
        
        # Initialize MQTT services
        await _initialize_mqtt_services(di)
    except Exception:
        _logger.exception("Error initializing repositories on startup")
        raise
    _logger.info("Infrastructure startup completed.")


async def _seed_initial_data(di):
    """Idempotent seeding of initial fuzzy system data, gated by environment and FUZZY_SEED_ON_STARTUP env var."""
    # Similar to .NET services: no seed data in production by default
    environment = os.getenv("ASPNETCORE_ENVIRONMENT", "Development")
    default_seed = "true" if environment.lower() == "development" else "false"
    
    seed_env = os.getenv("FUZZY_SEED_ON_STARTUP", default_seed).strip().lower()
    if seed_env not in ("1", "true", "yes", "y", "on"):
        _logger.info("Skipping seeding on startup (Environment=%s, FUZZY_SEED_ON_STARTUP=%s)", environment, seed_env)
        return

    try:
        # Import and use the external seed data module
        from .SeedData import seed_fuzzy_system_data
        await seed_fuzzy_system_data(di)
    except Exception:
        _logger.exception("Error during seeding initial data")


async def _initialize_mqtt_services(di) -> None:
    """Initialize MQTT services and start background subscription."""
    try:
        from FuzzyService.Infrastructure.ExternalServices.MqttService.MqttClient import MqttClient
        from FuzzyService.Infrastructure.ExternalServices.MqttService.MqttMessageHandler import MqttMessageHandler
        from FuzzyService.Infrastructure.ExternalServices.MqttService.MqttSubscriber import MqttSubscriber
        from medyator import Medyator
        
        mqtt_settings = di["mqtt_settings"]
        mediator = di[Medyator]
        
        # Create MQTT services
        mqtt_client = MqttClient(mqtt_settings)
        mqtt_handler = MqttMessageHandler(mediator)
        mqtt_subscriber = MqttSubscriber(mqtt_client, mqtt_handler, mqtt_settings)
        
        # Store in DI
        di["mqtt_client"] = mqtt_client
        di["mqtt_handler"] = mqtt_handler
        di["mqtt_subscriber"] = mqtt_subscriber
        
        # Start MQTT subscription in background
        import asyncio
        asyncio.create_task(mqtt_subscriber.start())
        
        _logger.info("MQTT services initialized and subscription started")
    except Exception:
        _logger.exception("Error initializing MQTT services")


async def on_shutdown() -> None:
    """Hook to cleanly dispose infrastructure resources (e.g., Mongo)."""
    # Clean up MQTT services
    try:
        mqtt_subscriber = di["mqtt_subscriber"]
        if mqtt_subscriber:
            await mqtt_subscriber.stop_subscription()
        mqtt_client = di["mqtt_client"]
        if mqtt_client:
            await mqtt_client.disconnect()
        _logger.info("MQTT services cleaned up")
    except Exception:
        _logger.exception("Error cleaning up MQTT services")
    
    # Clean up MongoDB
    close_fn: Callable[[], Awaitable] = di["mongo_close"]
    await close_fn()
    _logger.info("Infrastructure shutdown completed.")
