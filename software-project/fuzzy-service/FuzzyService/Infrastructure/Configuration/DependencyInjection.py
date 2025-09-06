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

    _logger.info("Infrastructure DI configured: mongo settings and factories registered.")


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

        # ActuatorService (external service)
        try:
            from FuzzyService.Infrastructure.ExternalServices.ActuatorService.ActuatorService import ActuatorService
            from FuzzyService.Domain.Interfaces.IActuatorService import IActuatorService
            
            actuator_service_url = os.getenv("ACTUATOR_SERVICE_URL", "http://localhost:5002")
            actuator_service_timeout = float(os.getenv("ACTUATOR_SERVICE_TIMEOUT", "30.0"))
            
            actuator_service = ActuatorService(actuator_service_url, actuator_service_timeout)
            di[IActuatorService] = actuator_service
            di["actuator_service"] = actuator_service
            _logger.info("ActuatorService initialized and registered in DI.")
        except Exception as ex:
            _logger.warning("ActuatorService not initialized: %s", ex)

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
    except Exception:
        _logger.exception("Error initializing repositories on startup")
        raise

    _logger.info("Infrastructure startup completed.")


async def on_shutdown() -> None:
    """Hook to cleanly dispose infrastructure resources (e.g., Mongo)."""
    close_fn: Callable[[], Awaitable] = di["mongo_close"]
    await close_fn()
    _logger.info("Infrastructure shutdown completed.")
