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

from FuzzyService.Application.Services.NullFuzzyEngine import NullFuzzyEngine

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
        from FuzzyService.Application.Services.NullFuzzyEngine import NullFuzzyEngine
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
        _logger.exception("Failed to bind IFuzzyEngine; fallback to NullFuzzyEngine.")
        try:
            from FuzzyService.Domain.Interfaces.IFuzzyEngine import IFuzzyEngine
            from FuzzyService.Application.Services.NullFuzzyEngine import NullFuzzyEngine
            di[IFuzzyEngine] = NullFuzzyEngine()
        except Exception:
            _logger.exception("Also failed to bind NullFuzzyEngine.")

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
    """Idempotent seeding of initial fuzzy system data, gated by FUZZY_SEED_ON_STARTUP env var."""
    seed_env = os.getenv("FUZZY_SEED_ON_STARTUP", "true").strip().lower()
    if seed_env not in ("1", "true", "yes", "y", "on"):
        _logger.info("Skipping seeding on startup (FUZZY_SEED_ON_STARTUP=%s)", seed_env)
        return

    try:
        # Resolve repos
        from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
        from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
        from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository
        from FuzzyService.Domain.Interfaces.IFuzzyRoutineRepository import IFuzzyRoutineRepository
        from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository

        repo_fs = di[IFuzzySystemRepository]
        repo_var = di[IFuzzyVariableRepository]
        repo_term = di[IFuzzyTermRepository]
        repo_routine = di[IFuzzyRoutineRepository]
        repo_rule = di[IFuzzyRuleRepository]

        # System
        from FuzzyService.Domain.Entities.fuzzy_system import FuzzySystem
        from FuzzyService.Domain.Enums import DefuzzificationMethod
        from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId, FuzzyVariableId, FuzzyTermId, FuzzyRoutineId
        from FuzzyService.Domain.ValueObjects.MembershipFunction import MembershipFunction
        from FuzzyService.Domain.Enums import MembershipFunctionType
        from FuzzyService.Domain.Entities.fuzzy_term import FuzzyTerm
        from FuzzyService.Domain.Entities.fuzzy_variable import FuzzyVariable

        system_name = os.getenv("FUZZY_SEED_SYSTEM_NAME", "Irrigation Control System")
        system = await repo_fs.get_by_name(system_name)
        if not system:
            system = FuzzySystem(name=system_name, defuzzification_method=DefuzzificationMethod.CENTROID)
            system = await repo_fs.create(system)
            _logger.info("Seed: created system '%s' (id=%s)", system_name, str(system.id))
        else:
            _logger.info("Seed: system '%s' already exists (id=%s)", system_name, str(system.id))

        # Helpers
        def tri(a: float, b: float, c: float, umin: float = 0.0, umax: float = 100.0) -> MembershipFunction:
            return MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[float(a), float(b), float(c)],
                universe_min=float(umin),
                universe_max=float(umax),
            )

        async def ensure_variable_with_terms(name: str, var_type: str, device_id: str | None, terms_def: list[tuple[str, MembershipFunction]]):
            var = await repo_var.get_by_name(name)
            if not var:
                var = await repo_var.create(FuzzyVariable(name=name, variable_type=var_type, device_id=device_id or None))
                _logger.info("Seed: created variable '%s' (type=%s, id=%s)", name, var_type, str(var.id))
            term_ids: list[FuzzyTermId] = []
            for label, mf in terms_def:
                existing_term = await repo_term.get_by_label(FuzzyVariableId(str(var.id)), label)
                if existing_term:
                    term = existing_term
                    _logger.info("Seed: term '%s' already exists for variable '%s'", label, name)
                else:
                    term = await repo_term.create(
                        FuzzyTerm(
                            variable_id=FuzzyVariableId(str(var.id)),
                            label=label,
                            membership_function=mf,
                        )
                    )
                    _logger.info("Seed: created term '%s' for variable '%s' (id=%s)", label, name, str(term.id))
                term_ids.append(FuzzyTermId(str(term.id)))
            # Update variable terms if changed
            if set(map(str, var.terms)) != set(map(str, term_ids)):
                var.terms = term_ids
                var = await repo_var.update(var)
                _logger.info("Seed: updated terms for variable '%s'", name)
            return var, term_ids

        # Output variables and terms
        power_var, power_terms = await ensure_variable_with_terms(
            os.getenv("FUZZY_SEED_POWER_VAR_NAME", "Irrigation Power"),
            "output",
            os.getenv("FUZZY_SEED_POWER_DEVICE", "actuator_pump_1"),
            [("low", tri(0, 0, 40)), ("medium", tri(30, 50, 70)), ("high", tri(60, 100, 100))],
        )
        duration_var, duration_terms = await ensure_variable_with_terms(
            os.getenv("FUZZY_SEED_DURATION_VAR_NAME", "Irrigation Duration"),
            "output",
            os.getenv("FUZZY_SEED_DURATION_DEVICE", "actuator_pump_1"),
            [("short", tri(0, 0, 30)), ("medium", tri(20, 50, 80)), ("long", tri(70, 100, 100))],
        )
        # Additional output variable for broader coverage of consequents
        frequency_var, frequency_terms = await ensure_variable_with_terms(
            os.getenv("FUZZY_SEED_FREQUENCY_VAR_NAME", "Irrigation Frequency"),
            "output",
            os.getenv("FUZZY_SEED_FREQUENCY_DEVICE", "actuator_pump_1"),
            [("rare", tri(0, 0, 30)), ("normal", tri(30, 50, 70)), ("frequent", tri(70, 100, 100))],
        )

        # Input variable for rule condition
        input_var, input_terms = await ensure_variable_with_terms(
            os.getenv("FUZZY_SEED_INPUT_VAR_NAME", "Soil Moisture"),
            "input",
            os.getenv("FUZZY_SEED_INPUT_DEVICE", "sensor_soil_moisture_1"),
            [("low", tri(0, 0, 40)), ("medium", tri(30, 50, 70)), ("high", tri(60, 100, 100))],
        )
        # Additional input variables (ensure at least 3 inputs with 3 labels each)
        input_var2, input_terms2 = await ensure_variable_with_terms(
            os.getenv("FUZZY_SEED_INPUT2_VAR_NAME", "Ambient Temperature"),
            "input",
            os.getenv("FUZZY_SEED_INPUT2_DEVICE", "sensor_temperature_1"),
            [("low", tri(0, 0, 20)), ("medium", tri(18, 25, 32)), ("high", tri(30, 50, 50))],
        )
        input_var3, input_terms3 = await ensure_variable_with_terms(
            os.getenv("FUZZY_SEED_INPUT3_VAR_NAME", "Light Intensity"),
            "input",
            os.getenv("FUZZY_SEED_INPUT3_DEVICE", "sensor_light_1"),
            [("low", tri(0, 0, 30)), ("medium", tri(25, 45, 70)), ("high", tri(60, 100, 100))],
        )

        # Routine
        routine = None
        if repo_routine:
            routine_name = os.getenv("FUZZY_SEED_ROUTINE_NAME", "Default Irrigation Routine")
            routine = await repo_routine.get_by_name(routine_name)  # type: ignore[attr-defined]
            if not routine:
                # pick power 'high' and duration 'long'
                label_index = {"low": 0, "medium": 1, "high": 2}
                # Fallback to last element if not found
                power_high_id = power_terms[label_index.get("high", len(power_terms) - 1)] if power_terms else None
                duration_long_id = duration_terms[label_index.get("long", len(duration_terms) - 1)] if duration_terms else None
                if power_high_id and duration_long_id:
                    from FuzzyService.Domain.Entities.fuzzy_routine import FuzzyRoutine, RoutineStep
                    step = RoutineStep(step_id=1, condition="default", power_term_id=power_high_id, duration_term_id=duration_long_id)
                    routine = await repo_routine.create(FuzzyRoutine(routine_name=routine_name, steps=[step]))  # type: ignore[attr-defined]
                    _logger.info("Seed: created routine '%s' (id=%s)", routine_name, str(routine.id))
            else:
                _logger.info("Seed: routine '%s' already exists (id=%s)", routine_name, str(routine.id))

            # Additional routines with multiple steps
            try:
                from FuzzyService.Domain.Entities.fuzzy_routine import FuzzyRoutine, RoutineStep
                label_index = {"low": 0, "medium": 1, "high": 2}
                dur_index = {"short": 0, "medium": 1, "long": 2}

                moderate_name = os.getenv("FUZZY_SEED_ROUTINE_MODERATE_NAME", "Moderate Irrigation Routine")
                routine_moderate = await repo_routine.get_by_name(moderate_name)  # type: ignore[attr-defined]
                if not routine_moderate and power_terms and duration_terms:
                    steps_mod = [
                        RoutineStep(step_id=1, condition="if moisture=medium AND temp=high", power_term_id=power_terms[label_index["medium"]], duration_term_id=duration_terms[dur_index["medium"]]),
                        RoutineStep(step_id=2, condition="fallback", power_term_id=power_terms[label_index["low"]], duration_term_id=duration_terms[dur_index["short"]]),
                    ]
                    routine_moderate = await repo_routine.create(FuzzyRoutine(routine_name=moderate_name, steps=steps_mod))  # type: ignore[attr-defined]
                    _logger.info("Seed: created routine '%s' (id=%s)", moderate_name, str(routine_moderate.id))

                pulse_name = os.getenv("FUZZY_SEED_ROUTINE_PULSE_NAME", "Pulse Irrigation Routine")
                routine_pulse = await repo_routine.get_by_name(pulse_name)  # type: ignore[attr-defined]
                if not routine_pulse and power_terms and duration_terms:
                    steps_pulse = [
                        RoutineStep(step_id=1, condition="pulse-1", power_term_id=power_terms[label_index["medium"]], duration_term_id=duration_terms[dur_index["short"]]),
                        RoutineStep(step_id=2, condition="pulse-2", power_term_id=power_terms[label_index["high"]], duration_term_id=duration_terms[dur_index["short"]]),
                        RoutineStep(step_id=3, condition="pulse-3", power_term_id=power_terms[label_index["low"]], duration_term_id=duration_terms[dur_index["short"]]),
                    ]
                    routine_pulse = await repo_routine.create(FuzzyRoutine(routine_name=pulse_name, steps=steps_pulse))  # type: ignore[attr-defined]
                    _logger.info("Seed: created routine '%s' (id=%s)", pulse_name, str(routine_pulse.id))
            except Exception:
                _logger.exception("Seed: failed creating additional routines")
        else:
            _logger.info("Seed: routine repository not available; skipping routine creation.")

        # Rule
        rule = None
        created_rules = []
        if repo_rule and routine:
            rule_name = os.getenv("FUZZY_SEED_RULE_NAME", "Low moisture => irrigate")
            existing_rule = await repo_rule.get_by_name(FuzzySystemId(str(system.id)), rule_name)  # type: ignore[attr-defined]
            if not existing_rule:
                from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule
                condition = {"variableId": FuzzyVariableId(str(input_var.id)), "operator": "IS", "value": "low"}
                rule = await repo_rule.create(  # type: ignore[attr-defined]
                    FuzzyRule(
                        name=rule_name,
                        system_id=FuzzySystemId(str(system.id)),
                        description="Seed rule: if soil moisture is low, run default irrigation routine",
                        conditions=[condition],
                        connectors=[],
                        consequent=FuzzyRoutineId(str(routine.id)),
                    )
                )
                _logger.info("Seed: created rule '%s' (id=%s)", rule_name, str(rule.id))
            else:
                rule = existing_rule
                _logger.info("Seed: rule '%s' already exists (id=%s)", rule_name, str(rule.id))
            if rule:
                created_rules.append(rule)

            # Additional rules using multiple conditions and connectors
            # Rule 2: moisture=medium AND temp=high => Moderate routine
            moderate_name = os.getenv("FUZZY_SEED_ROUTINE_MODERATE_NAME", "Moderate Irrigation Routine")
            routine_moderate = await repo_routine.get_by_name(moderate_name)  # type: ignore[attr-defined]
            rule_name2 = os.getenv("FUZZY_SEED_RULE2_NAME", "Moisture medium AND Temp high => Moderate")
            existing_rule2 = await repo_rule.get_by_name(FuzzySystemId(str(system.id)), rule_name2)  # type: ignore[attr-defined]
            if not existing_rule2 and routine_moderate:
                from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule
                conditions2 = [
                    {"variableId": FuzzyVariableId(str(input_var.id)), "operator": "IS", "value": "medium"},
                    {"variableId": FuzzyVariableId(str(input_var2.id)), "operator": "IS", "value": "high"},
                ]
                rule2 = await repo_rule.create(  # type: ignore[attr-defined]
                    FuzzyRule(
                        name=rule_name2,
                        system_id=FuzzySystemId(str(system.id)),
                        description="Seed rule: medium moisture AND high temperature",
                        conditions=conditions2,
                        connectors=["AND"],
                        consequent=FuzzyRoutineId(str(routine_moderate.id)),
                    )
                )
                created_rules.append(rule2)
                _logger.info("Seed: created rule '%s' (id=%s)", rule_name2, str(rule2.id))

            # Rule 3: moisture=low AND light=high => Pulse routine
            pulse_name = os.getenv("FUZZY_SEED_ROUTINE_PULSE_NAME", "Pulse Irrigation Routine")
            routine_pulse = await repo_routine.get_by_name(pulse_name)  # type: ignore[attr-defined]
            rule_name3 = os.getenv("FUZZY_SEED_RULE3_NAME", "Low moisture AND Light high => Pulse")
            existing_rule3 = await repo_rule.get_by_name(FuzzySystemId(str(system.id)), rule_name3)  # type: ignore[attr-defined]
            if not existing_rule3 and routine_pulse:
                from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule
                conditions3 = [
                    {"variableId": FuzzyVariableId(str(input_var.id)), "operator": "IS", "value": "low"},
                    {"variableId": FuzzyVariableId(str(input_var3.id)), "operator": "IS", "value": "high"},
                ]
                rule3 = await repo_rule.create(  # type: ignore[attr-defined]
                    FuzzyRule(
                        name=rule_name3,
                        system_id=FuzzySystemId(str(system.id)),
                        description="Seed rule: low moisture AND high light => pulse irrigation",
                        conditions=conditions3,
                        connectors=["AND"],
                        consequent=FuzzyRoutineId(str(routine_pulse.id)),
                    )
                )
                created_rules.append(rule3)
                _logger.info("Seed: created rule '%s' (id=%s)", rule_name3, str(rule3.id))

            # Rule 4: temp=low OR moisture=high => Moderate routine
            rule_name4 = os.getenv("FUZZY_SEED_RULE4_NAME", "Temp low OR Moisture high => Moderate")
            existing_rule4 = await repo_rule.get_by_name(FuzzySystemId(str(system.id)), rule_name4)  # type: ignore[attr-defined]
            if not existing_rule4 and routine_moderate:
                from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule
                conditions4 = [
                    {"variableId": FuzzyVariableId(str(input_var2.id)), "operator": "IS", "value": "low"},
                    {"variableId": FuzzyVariableId(str(input_var.id)), "operator": "IS", "value": "high"},
                ]
                rule4 = await repo_rule.create(  # type: ignore[attr-defined]
                    FuzzyRule(
                        name=rule_name4,
                        system_id=FuzzySystemId(str(system.id)),
                        description="Seed rule: low temperature OR high moisture",
                        conditions=conditions4,
                        connectors=["OR"],
                        consequent=FuzzyRoutineId(str(routine_moderate.id)),
                    )
                )
                created_rules.append(rule4)
                _logger.info("Seed: created rule '%s' (id=%s)", rule_name4, str(rule4.id))
        elif not repo_rule:
            _logger.info("Seed: rule repository not available; skipping rule creation.")

        # Update system relationships and status
        changed = False
        current_inputs = [str(v) for v in system.input_variable_ids]
        current_outputs = [str(v) for v in system.output_variable_ids]
        current_rules = [str(r) for r in system.rule_ids]

        for iv in [input_var, input_var2, input_var3]:
            if str(iv.id) not in current_inputs:
                system.input_variable_ids.append(FuzzyVariableId(str(iv.id)))
                changed = True
        for v in (power_var, duration_var, frequency_var):
            if str(v.id) not in current_outputs:
                system.output_variable_ids.append(FuzzyVariableId(str(v.id)))
                changed = True
        for r in created_rules:
            if str(r.id) not in current_rules:
                system.rule_ids.append(r.id)
                changed = True

        # Try to set target status
        target_status = (os.getenv("FUZZY_SEED_SYSTEM_STATUS", "ACTIVE") or "").upper()
        if target_status == "ACTIVE":
            try:
                system.activate()
                changed = True
            except Exception as ex:
                _logger.warning("Seed: cannot activate system yet: %s", ex)
        elif target_status == "TESTING":
            try:
                system.set_testing_mode()
                changed = True
            except Exception as ex:
                _logger.warning("Seed: cannot set system TESTING: %s", ex)

        if changed:
            await repo_fs.update(system)
            _logger.info("Seed: system updated (relationships/status)")
        else:
            _logger.info("Seed: system already up to date")

        _logger.info("Seeding completed for system '%s'", system_name)
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
