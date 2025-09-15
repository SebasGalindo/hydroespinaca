"""Seed data configuration for the Fuzzy System.
This module contains all the initial data needed to bootstrap the fuzzy system.
"""

from __future__ import annotations

import logging
import os
from typing import Dict, List, Any, Optional

from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId, FuzzyVariableId, FuzzyRuleId, FuzzyTermId, FuzzyRoutineId

# Import interfaces for DI access
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository
from FuzzyService.Domain.Interfaces.IFuzzyRoutineRepository import IFuzzyRoutineRepository

_logger = logging.getLogger(__name__)


class SeedDataConfig:
    """Configuration class for seed data."""
    
    @staticmethod
    def get_system_config() -> Dict[str, Any]:
        """Get fuzzy system configuration."""
        return {
            "name": os.getenv("FUZZY_SEED_SYSTEM_NAME", "Hydroponic Control System"),
            "description": "Seed system for hydroponic environment control"
        }
    
    @staticmethod
    def get_output_variables_config() -> List[Dict[str, Any]]:
        """Get output variables configuration."""
        return [
            {
                "name": "Irrigation Duration",
                "description": "Duration of irrigation in minutes",
                "device_id": "68c821cd60cf00c98ede8229",  # Irrigation pump actuator
                "terms": [
                    {"name": "short", "description": "Short irrigation duration"},
                    {"name": "medium", "description": "Medium irrigation duration"},
                    {"name": "long", "description": "Long irrigation duration"}
                ]
            },
            {
                "name": "Irrigation Frequency",
                "description": "Frequency of irrigation per day",
                "device_id": "68c821cd60cf00c98ede822a",  # Irrigation scheduler actuator
                "terms": [
                    {"name": "low", "description": "Low irrigation frequency"},
                    {"name": "medium", "description": "Medium irrigation frequency"},
                    {"name": "high", "description": "High irrigation frequency"}
                ]
            },
            {
                "name": "Fan Power",
                "description": "Power level of cooling fan",
                "device_id": "68c821cd60cf00c98ede822b",  # Cooling fan actuator
                "terms": [
                    {"name": "off", "description": "Fan turned off"},
                    {"name": "low", "description": "Low fan power"},
                    {"name": "medium", "description": "Medium fan power"},
                    {"name": "high", "description": "High fan power"}
                ]
            },
            {
                "name": "Heater Power",
                "description": "Power level of heater",
                "device_id": "68c821cd60cf00c98ede822c",  # Heater actuator
                "terms": [
                    {"name": "off", "description": "Heater turned off"},
                    {"name": "low", "description": "Low heater power"},
                    {"name": "medium", "description": "Medium heater power"},
                    {"name": "high", "description": "High heater power"}
                ]
            }
        ]
    
    @staticmethod
    def get_input_variables_config() -> List[Dict[str, Any]]:
        """Get input variables configuration."""
        return [
            {
                "name": "Soil Moisture",
                "description": "Moisture level in the soil",
                "device_id": "68c821cd60cf00c98ede8111",
                "terms": [
                    {"name": "low", "description": "Low soil moisture"},
                    {"name": "medium", "description": "Medium soil moisture"},
                    {"name": "high", "description": "High soil moisture"}
                ]
            },
            {
                "name": "Ambient Temperature",
                "description": "Temperature of the environment",
                "device_id": "68c821e8690f20bddcde3222",
                "terms": [
                    {"name": "low", "description": "Low temperature"},
                    {"name": "medium", "description": "Medium temperature"},
                    {"name": "high", "description": "High temperature"}
                ]
            },
            {
                "name": "Light Intensity",
                "description": "Intensity of light in the environment",
                "device_id": "68c821ea690f20bddcde3333",
                "terms": [
                    {"name": "low", "description": "Low light intensity"},
                    {"name": "medium", "description": "Medium light intensity"},
                    {"name": "high", "description": "High light intensity"}
                ]
            }
        ]
    
    @staticmethod
    def get_routines_config() -> List[Dict[str, Any]]:
        """Get fuzzy routines configuration."""
        return [
            {
                "name": os.getenv("FUZZY_SEED_ROUTINE_DEFAULT_NAME", "Default Irrigation Routine"),
                "description": "Default irrigation routine for normal conditions",
                "steps": [
                    {"actuator_type": "irrigation", "power": "medium", "duration": "medium"},
                ]
            },
            {
                "name": os.getenv("FUZZY_SEED_ROUTINE_MODERATE_NAME", "Moderate Irrigation Routine"),
                "description": "Moderate irrigation routine for balanced conditions",
                "steps": [
                    {"actuator_type": "irrigation", "power": "low", "duration": "short"},
                ]
            },
            {
                "name": os.getenv("FUZZY_SEED_ROUTINE_PULSE_NAME", "Pulse Irrigation Routine"),
                "description": "Pulse irrigation routine for high light conditions",
                "steps": [
                    {"actuator_type": "irrigation", "power": "high", "duration": "short"},
                ]
            },
            {
                "name": os.getenv("FUZZY_SEED_ROUTINE_FAN_COOLING_NAME", "Fan Cooling Routine"),
                "description": "Fan cooling routine for high temperature",
                "steps": [
                    {"actuator_type": "fan", "power": "high", "duration": "long"},
                ]
            },
            {
                "name": os.getenv("FUZZY_SEED_ROUTINE_HEATER_WARMING_NAME", "Heater Warming Routine"),
                "description": "Heater warming routine for low temperature",
                "steps": [
                    {"actuator_type": "heater", "power": "medium", "duration": "medium"},
                ]
            },
            {
                "name": os.getenv("FUZZY_SEED_ROUTINE_DUAL_CONTROL_NAME", "Dual Control Routine"),
                "description": "Dual control routine with fan and heater coordination",
                "steps": [
                    {"actuator_type": "fan", "power": "medium", "duration": "short", "duration_variable": "Irrigation Duration"},
                    {"actuator_type": "heater", "power": "low", "duration": "medium", "duration_variable": "Irrigation Frequency"}
                ]
            }
        ]
    
    @staticmethod
    def get_rules_config() -> List[Dict[str, Any]]:
        """Get fuzzy rules configuration."""
        return [
            {
                "name": os.getenv("FUZZY_SEED_RULE_NAME", "Low moisture => irrigate"),
                "description": "Seed rule: if soil moisture is low, run default irrigation routine",
                "conditions": [
                    {"variable": "Soil Moisture", "operator": "IS", "value": "low"}
                ],
                "connectors": [],
                "consequent_routine": os.getenv("FUZZY_SEED_ROUTINE_DEFAULT_NAME", "Default Irrigation Routine")
            },
            {
                "name": os.getenv("FUZZY_SEED_RULE2_NAME", "Moisture medium AND Temp high => Moderate"),
                "description": "Seed rule: medium moisture AND high temperature",
                "conditions": [
                    {"variable": "Soil Moisture", "operator": "IS", "value": "medium"},
                    {"variable": "Ambient Temperature", "operator": "IS", "value": "high"}
                ],
                "connectors": ["AND"],
                "consequent_routine": os.getenv("FUZZY_SEED_ROUTINE_MODERATE_NAME", "Moderate Irrigation Routine")
            },
            {
                "name": os.getenv("FUZZY_SEED_RULE3_NAME", "Low moisture AND Light high => Pulse"),
                "description": "Seed rule: low moisture AND high light intensity",
                "conditions": [
                    {"variable": "Soil Moisture", "operator": "IS", "value": "low"},
                    {"variable": "Light Intensity", "operator": "IS", "value": "high"}
                ],
                "connectors": ["AND"],
                "consequent_routine": os.getenv("FUZZY_SEED_ROUTINE_PULSE_NAME", "Pulse Irrigation Routine")
            },
            {
                "name": os.getenv("FUZZY_SEED_RULE4_NAME", "Temp low OR Moisture high => Moderate"),
                "description": "Seed rule: low temperature OR high moisture",
                "conditions": [
                    {"variable": "Ambient Temperature", "operator": "IS", "value": "low"},
                    {"variable": "Soil Moisture", "operator": "IS", "value": "high"}
                ],
                "connectors": ["OR"],
                "consequent_routine": os.getenv("FUZZY_SEED_ROUTINE_MODERATE_NAME", "Moderate Irrigation Routine")
            },
            {
                "name": os.getenv("FUZZY_SEED_RULE5_NAME", "High temp => Fan cooling"),
                "description": "Seed rule: high temperature => activate fan cooling",
                "conditions": [
                    {"variable": "Ambient Temperature", "operator": "IS", "value": "high"}
                ],
                "connectors": [],
                "consequent_routine": os.getenv("FUZZY_SEED_ROUTINE_FAN_COOLING_NAME", "Fan Cooling Routine")
            },
            {
                "name": os.getenv("FUZZY_SEED_RULE6_NAME", "Low temp AND Low light => Heater warming"),
                "description": "Seed rule: low temperature AND low light => activate heater",
                "conditions": [
                    {"variable": "Ambient Temperature", "operator": "IS", "value": "low"},
                    {"variable": "Light Intensity", "operator": "IS", "value": "low"}
                ],
                "connectors": ["AND"],
                "consequent_routine": os.getenv("FUZZY_SEED_ROUTINE_HEATER_WARMING_NAME", "Heater Warming Routine")
            },
            {
                "name": os.getenv("FUZZY_SEED_RULE7_NAME", "Medium moisture AND High light => Dual control"),
                "description": "Seed rule: medium soil moisture AND high light => activate dual control",
                "conditions": [
                    {"variable": "Soil Moisture", "operator": "IS", "value": "medium"},
                    {"variable": "Light Intensity", "operator": "IS", "value": "high"}
                ],
                "connectors": ["AND"],
                "consequent_routine": os.getenv("FUZZY_SEED_ROUTINE_DUAL_CONTROL_NAME", "Dual Control Routine")
            }
        ]


async def seed_fuzzy_system_data(di) -> None:
    """
    Seed initial data for the fuzzy system using external configuration.
    
    Args:
        di: Dependency injection container
    """
    _logger.info("Starting fuzzy system data seeding...")
    
    # Get repositories
    repo_system = di[IFuzzySystemRepository]
    repo_variable = di[IFuzzyVariableRepository]
    repo_term = di[IFuzzyTermRepository]
    repo_routine = di[IFuzzyRoutineRepository]
    repo_rule = di[IFuzzyRuleRepository]
    
    if not all([repo_system, repo_variable, repo_term, repo_routine, repo_rule]):
        _logger.warning("Some repositories not available; skipping seed data creation.")
        return
    
    # Create fuzzy system
    system = await _create_fuzzy_system(repo_system)
    if not system:
        return
    
    # Create variables and terms
    variables_map = await _create_variables_and_terms(repo_variable, repo_term, system)
    
    # Create routines
    routines_map = await _create_routines(repo_routine, repo_term, variables_map)
    
    # Create rules
    await _create_rules(repo_rule, system, variables_map, routines_map)
    
    # Update system relationships
    await _update_system_relationships(repo_system, system, variables_map, repo_rule)
    
    _logger.info("Fuzzy system data seeding completed successfully.")


async def _create_fuzzy_system(repo_system):
    """Create the main fuzzy system."""
    from FuzzyService.Domain.Entities.fuzzy_system import FuzzySystem
    from FuzzyService.Domain.Enums.EntityStatus import FuzzySystemStatus
    from FuzzyService.Domain.Enums.DefuzzificationMethod import DefuzzificationMethod
    from FuzzyService.Domain.ValueObjects.OperatorsConfig import OperatorsConfig
    
    system_name = "Hydroponic Control System"
    existing_system = await repo_system.get_by_name(system_name)  # type: ignore[attr-defined]
    
    if not existing_system:
        system = await repo_system.create(  # type: ignore[attr-defined]
            FuzzySystem(
                name=system_name,
                status=FuzzySystemStatus.ACTIVE,
                defuzzification_method=DefuzzificationMethod.CENTROID,
                operators=OperatorsConfig(),
                input_variable_ids=[],
                output_variable_ids=[],
                rule_ids=[]
            )
        )
        _logger.info("Seed: created fuzzy system '%s' (id=%s)", system_name, str(system.id))
        return system
    else:
        _logger.info("Seed: fuzzy system '%s' already exists (id=%s)", system_name, str(existing_system.id))
        return existing_system


def _get_realistic_membership_function(variable_name: str, term_name: str):
    """Get realistic membership function parameters based on variable and term."""
    from FuzzyService.Domain.ValueObjects import MembershipFunction
    from FuzzyService.Domain.Enums import MembershipFunctionType
    
    # Soil Moisture (0-100%)
    if variable_name == "Soil Moisture":
        if term_name == "low":
            return MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[0.0, 0.0, 30.0],
                universe_min=0.0,
                universe_max=100.0
            )
        elif term_name == "medium":
            return MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[20.0, 50.0, 80.0],
                universe_min=0.0,
                universe_max=100.0
            )
        elif term_name == "high":
            return MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[70.0, 100.0, 100.0],
                universe_min=0.0,
                universe_max=100.0
            )
    
    # Ambient Temperature (10-40°C)
    elif variable_name == "Ambient Temperature":
        if term_name == "low":
            return MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[10.0, 10.0, 20.0],
                universe_min=10.0,
                universe_max=40.0
            )
        elif term_name == "medium":
            return MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[18.0, 25.0, 32.0],
                universe_min=10.0,
                universe_max=40.0
            )
        elif term_name == "high":
            return MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[30.0, 40.0, 40.0],
                universe_min=10.0,
                universe_max=40.0
            )
    
    # Light Intensity (0-1000 lux)
    elif variable_name == "Light Intensity":
        if term_name == "low":
            return MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[0.0, 0.0, 300.0],
                universe_min=0.0,
                universe_max=1000.0
            )
        elif term_name == "medium":
            return MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[200.0, 500.0, 800.0],
                universe_min=0.0,
                universe_max=1000.0
            )
        elif term_name == "high":
            return MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[700.0, 1000.0, 1000.0],
                universe_min=0.0,
                universe_max=1000.0
            )
    
    # Output variables
    elif variable_name == "Irrigation Duration":
        if term_name == "short":
            return MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[0.0, 0.0, 10.0],
                universe_min=0.0,
                universe_max=30.0
            )
        elif term_name == "medium":
            return MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[5.0, 15.0, 25.0],
                universe_min=0.0,
                universe_max=30.0
            )
        elif term_name == "long":
            return MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[20.0, 30.0, 30.0],
                universe_min=0.0,
                universe_max=30.0
            )
    
    elif variable_name == "Irrigation Frequency":
        if term_name == "low":
            return MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[0.0, 0.0, 2.0],
                universe_min=0.0,
                universe_max=6.0
            )
        elif term_name == "medium":
            return MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[1.0, 3.0, 5.0],
                universe_min=0.0,
                universe_max=6.0
            )
        elif term_name == "high":
            return MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[4.0, 6.0, 6.0],
                universe_min=0.0,
                universe_max=6.0
            )
    
    elif variable_name == "Fan Power":
        if term_name == "low":
            return MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[0.0, 0.0, 30.0],
                universe_min=0.0,
                universe_max=100.0
            )
        elif term_name == "medium":
            return MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[20.0, 50.0, 80.0],
                universe_min=0.0,
                universe_max=100.0
            )
        elif term_name == "high":
            return MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[70.0, 100.0, 100.0],
                universe_min=0.0,
                universe_max=100.0
            )
    
    elif variable_name == "Heater Power":
        if term_name == "low":
            return MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[0.0, 0.0, 30.0],
                universe_min=0.0,
                universe_max=100.0
            )
        elif term_name == "medium":
            return MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[20.0, 50.0, 80.0],
                universe_min=0.0,
                universe_max=100.0
            )
        elif term_name == "high":
            return MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[70.0, 100.0, 100.0],
                universe_min=0.0,
                universe_max=100.0
            )
    
    # Default fallback
    return MembershipFunction(
        function_type=MembershipFunctionType.TRIANGULAR,
        parameters=[0.0, 0.5, 1.0],
        universe_min=0.0,
        universe_max=1.0
    )


async def _create_variables_and_terms(repo_variable, repo_term, system):
    """Create variables and their terms."""
    from FuzzyService.Domain.Entities.fuzzy_variable import FuzzyVariable
    from FuzzyService.Domain.Entities.fuzzy_term import FuzzyTerm
    
    variables_map = {}
    
    # Helper function to ensure variable with terms
    async def ensure_variable_with_terms(var_config, is_input=True):
        existing_var = await repo_variable.get_by_name(var_config["name"])  # type: ignore[attr-defined]
        
        if not existing_var:
            variable = await repo_variable.create(  # type: ignore[attr-defined]
                FuzzyVariable(
                    name=var_config["name"],
                    description=var_config["description"],
                    variable_type="input" if is_input else "output",
                    device_id=var_config.get("device_id"),
                    terms=[]
                )
            )
            _logger.info("Seed: created variable '%s' (id=%s)", var_config["name"], str(variable.id))
        else:
            variable = existing_var
            _logger.info("Seed: variable '%s' already exists (id=%s)", var_config["name"], str(variable.id))
        
        # Create terms for this variable
        for term_config in var_config["terms"]:
            existing_term = await repo_term.get_by_label(FuzzyVariableId(str(variable.id)), term_config["name"])  # type: ignore[attr-defined]
            if not existing_term:
                # Get realistic parameters based on variable name and term
                membership_function = _get_realistic_membership_function(var_config["name"], term_config["name"])
                
                term = await repo_term.create(  # type: ignore[attr-defined]
                    FuzzyTerm(
                        label=term_config["name"],
                        variable_id=FuzzyVariableId(str(variable.id)),
                        membership_function=membership_function
                    )
                )
                _logger.info("Seed: created term '%s' for variable '%s' (id=%s)", term_config["name"], var_config["name"], str(term.id))
        
        return variable
    
    # Create output variables
    for var_config in SeedDataConfig.get_output_variables_config():
        variable = await ensure_variable_with_terms(var_config, is_input=False)
        variables_map[var_config["name"]] = variable
    
    # Create input variables
    for var_config in SeedDataConfig.get_input_variables_config():
        variable = await ensure_variable_with_terms(var_config, is_input=True)
        variables_map[var_config["name"]] = variable
    
    return variables_map


async def _create_routines(repo_routine, repo_term, variables_map):
    """Create fuzzy routines."""
    from FuzzyService.Domain.Entities.fuzzy_routine import FuzzyRoutine, RoutineStep
    
    routines_map = {}
    
    for routine_config in SeedDataConfig.get_routines_config():
        existing_routine = await repo_routine.get_by_name(routine_config["name"])  # type: ignore[attr-defined]
        
        if not existing_routine:
            # Create routine steps
            steps = []
            for step_config in routine_config["steps"]:
                # Get term IDs for power and duration
                power_term_id = await _get_term_id_by_name(repo_term, variables_map, step_config["actuator_type"].title() + " Power", step_config["power"])
                
                # Use duration_variable if specified, otherwise default to "Irrigation Duration"
                duration_variable = step_config.get("duration_variable", "Irrigation Duration")
                duration_term_id = await _get_term_id_by_name(repo_term, variables_map, duration_variable, step_config["duration"])
                
                if power_term_id and duration_term_id:
                    step = RoutineStep(
                        step_id=len(steps),  # Use index as step_id
                        condition=f"Actuator: {step_config['actuator_type']}",
                        power_term_id=power_term_id,
                        duration_term_id=duration_term_id
                    )
                    steps.append(step)
            
            if steps:
                routine = await repo_routine.create(  # type: ignore[attr-defined]
                    FuzzyRoutine(
                        routine_name=routine_config["name"],
                        steps=steps
                    )
                )
                _logger.info("Seed: created routine '%s' (id=%s)", routine_config["name"], str(routine.id))
                routines_map[routine_config["name"]] = routine
        else:
            _logger.info("Seed: routine '%s' already exists (id=%s)", routine_config["name"], str(existing_routine.id))
            routines_map[routine_config["name"]] = existing_routine
    
    return routines_map


async def _create_rules(repo_rule, system, variables_map, routines_map):
    """Create fuzzy rules."""
    from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule
    
    for rule_config in SeedDataConfig.get_rules_config():
        existing_rule = await repo_rule.get_by_name(FuzzySystemId(str(system.id)), rule_config["name"])  # type: ignore[attr-defined]
        
        if not existing_rule:
            # Build conditions
            conditions = []
            for condition_config in rule_config["conditions"]:
                variable = variables_map.get(condition_config["variable"])
                if variable:
                    condition = {
                        "variableId": FuzzyVariableId(str(variable.id)),
                        "operator": condition_config["operator"],
                        "value": condition_config["value"]
                    }
                    conditions.append(condition)
            
            # Get consequent routine
            routine = routines_map.get(rule_config["consequent_routine"])
            
            if conditions and routine:
                rule = await repo_rule.create(  # type: ignore[attr-defined]
                    FuzzyRule(
                        name=rule_config["name"],
                        system_id=FuzzySystemId(str(system.id)),
                        description=rule_config["description"],
                        conditions=conditions,
                        connectors=rule_config["connectors"],
                        consequent=FuzzyRoutineId(str(routine.id)),
                    )
                )
                _logger.info("Seed: created rule '%s' (id=%s)", rule_config["name"], str(rule.id))


async def _update_system_relationships(repo_system, system, variables_map, repo_rule):
    """Update system with created variables and rules."""
    # Get all variables for this system
    all_variables = list(variables_map.values())
    
    # Separate variables by type
    input_variables = [FuzzyVariableId(str(var.id)) for var in all_variables if var.variable_type == "input"]
    output_variables = [FuzzyVariableId(str(var.id)) for var in all_variables if var.variable_type == "output"]
    
    # Get all rules for this system
    all_rules = await repo_rule.get_by_system_id(FuzzySystemId(str(system.id)))  # type: ignore[attr-defined]
    
    # Update system
    system.input_variable_ids = input_variables
    system.output_variable_ids = output_variables
    system.rule_ids = [rule.id for rule in all_rules] if all_rules else []
    
    await repo_system.update(system)  # type: ignore[attr-defined]
    _logger.info("Seed: updated system relationships - %d input vars, %d output vars, %d rules", 
                len(input_variables), len(output_variables), len(all_rules))


async def _get_term_id_by_name(repo_term, variables_map, variable_name: str, term_name: str) -> Optional[FuzzyTermId]:
    """Helper to get term ID by variable and term name."""
    variable = variables_map.get(variable_name)
    if not variable:
        return None
    
    term = await repo_term.get_by_label(FuzzyVariableId(str(variable.id)), term_name)  # type: ignore[attr-defined]
    return FuzzyTermId(str(term.id)) if term else None