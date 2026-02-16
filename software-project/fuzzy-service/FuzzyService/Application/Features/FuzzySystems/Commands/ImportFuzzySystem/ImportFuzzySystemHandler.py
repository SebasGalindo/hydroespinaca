from __future__ import annotations

import logging
from typing import Any, Dict, List

from medyator import CommandHandler
from kink import di

from FuzzyService.Application.Features.FuzzySystems.DTOs.FuzzySystemDto import FuzzySystemDto
from FuzzyService.Application.Features.FuzzySystems.Commands.ImportFuzzySystem.ImportFuzzySystemCommand import ImportFuzzySystemCommand
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyVariableId, FuzzyTermId
from FuzzyService.Domain.Enums import FuzzySystemStatus, DefuzzificationMethod, MembershipFunctionType, RuleConnector
from FuzzyService.Domain.Entities.fuzzy_system import FuzzySystem
from FuzzyService.Domain.Entities.fuzzy_variable import FuzzyVariable
from FuzzyService.Domain.Entities.fuzzy_term import FuzzyTerm
from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule
from FuzzyService.Domain.Entities.rule_consequent import RuleConsequent
from FuzzyService.Domain.ValueObjects.MembershipFunction import MembershipFunction
from FuzzyService.Domain.ValueObjects.OperatorsConfig import OperatorsConfig
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository
from FuzzyService.Domain.Errors.DomainErrors import ValidationError

_logger = logging.getLogger(__name__)


class ImportFuzzySystemHandler(CommandHandler[ImportFuzzySystemCommand]):
    """Handler del comando ImportFuzzySystemCommand.
    
    Reconstruye un sistema difuso completo desde el formato de exportación portable.
    Los índices de posición (variable_ref, term_refs) se resuelven a nuevos IDs.
    """

    async def __call__(self, request: ImportFuzzySystemCommand) -> None:
        system_repo: IFuzzySystemRepository = di[IFuzzySystemRepository]
        variable_repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]
        term_repo: IFuzzyTermRepository = di[IFuzzyTermRepository]
        rule_repo: IFuzzyRuleRepository = di[IFuzzyRuleRepository]

        system_data = request.system
        _logger.info("Importando sistema '%s' (version=%s)", system_data.get("name"), request.version)

        # 1. Crear las variables
        created_variables: List[FuzzyVariable] = []  # índice → variable creada
        for idx, var_data in enumerate(request.variables):
            new_var = FuzzyVariable(
                name=var_data["name"],
                description=var_data.get("description", ""),
                variable_type=var_data.get("variable_type", "input"),
                actuator_type=var_data.get("actuator_type"),
                defuzzification_threshold=var_data.get("defuzzification_threshold", 50.0),
                universe_min=var_data.get("universe_min"),
                universe_max=var_data.get("universe_max"),
                reference_code=var_data.get("reference_code"),
                terms=[],
            )
            created_var = await variable_repo.create(new_var)
            created_variables.append(created_var)
            _logger.debug("Variable importada [%d]: '%s' → id=%s", idx, created_var.name, str(created_var.id))

        # 2. Crear los términos (resolviendo variable_ref → variable ID)
        created_terms: List[FuzzyTerm] = []  # índice → término creado
        for idx, term_data in enumerate(request.terms):
            var_ref = term_data.get("variable_ref")
            if var_ref is None or var_ref < 0 or var_ref >= len(created_variables):
                raise ValidationError(
                    f"Término [{idx}]: variable_ref={var_ref} fuera de rango (0-{len(created_variables)-1})"
                )

            target_var = created_variables[var_ref]
            mf_data = term_data.get("membership_function", {})

            new_term = FuzzyTerm(
                variable_id=target_var.id,
                label=term_data["label"],
                membership_function=MembershipFunction(
                    function_type=MembershipFunctionType(mf_data["function_type"]),
                    parameters=list(mf_data["parameters"]),
                    universe_min=mf_data["universe_min"],
                    universe_max=mf_data["universe_max"],
                ),
            )
            created_term = await term_repo.create(new_term)
            created_terms.append(created_term)
            _logger.debug("Término importado [%d]: '%s' → id=%s", idx, created_term.label, str(created_term.id))

            # Agregar el término a la variable
            target_var.terms.append(created_term.id)

        # 3. Actualizar variables con sus términos
        for var in created_variables:
            if var.terms:
                await variable_repo.update(var)

        # 4. Clasificar variables en input/output
        input_var_ids = [v.id for v in created_variables if v.variable_type == "input"]
        output_var_ids = [v.id for v in created_variables if v.variable_type == "output"]

        # 5. Crear las reglas (resolviendo variable_ref y term_refs)
        created_rule_ids = []
        for idx, rule_data in enumerate(request.rules):
            # Resolver condiciones
            conditions = []
            for cond in rule_data.get("conditions", []):
                var_ref = cond.get("variable_ref")
                if var_ref is None or var_ref < 0 or var_ref >= len(created_variables):
                    raise ValidationError(
                        f"Regla [{idx}]: condition variable_ref={var_ref} fuera de rango"
                    )
                conditions.append({
                    "variableId": created_variables[var_ref].id,
                    "operator": cond["operator"],
                    "value": cond["value"],
                })

            # Resolver conectores
            connectors = [RuleConnector(c) for c in rule_data.get("connectors", [])]

            # Resolver consecuentes
            consequents = []
            for cons in rule_data.get("consequents", []):
                var_ref = cons.get("variable_ref")
                if var_ref is None or var_ref < 0 or var_ref >= len(created_variables):
                    raise ValidationError(
                        f"Regla [{idx}]: consequent variable_ref={var_ref} fuera de rango"
                    )
                term_refs = cons.get("term_refs", [])
                resolved_terms = []
                for t_ref in term_refs:
                    if t_ref is None or t_ref < 0 or t_ref >= len(created_terms):
                        raise ValidationError(
                            f"Regla [{idx}]: term_ref={t_ref} fuera de rango"
                        )
                    resolved_terms.append(created_terms[t_ref].id)

                consequents.append(RuleConsequent(
                    variable_id=created_variables[var_ref].id,
                    terms=resolved_terms,
                    aggregation_method=cons.get("aggregation_method", "max"),
                ))

            new_rule = FuzzyRule(
                name=rule_data["name"],
                system_id=None,  # Se actualizará después
                description=rule_data.get("description"),
                conditions=conditions,
                connectors=connectors,
                consequents=consequents,
            )
            created_rule = await rule_repo.create(new_rule)
            created_rule_ids.append(created_rule.id)
            _logger.debug("Regla importada [%d]: '%s' → id=%s", idx, created_rule.name, str(created_rule.id))

        # 6. Crear el sistema
        operators_data = system_data.get("operators", {})
        operators = OperatorsConfig.from_dict(operators_data) if operators_data else OperatorsConfig()

        new_system = FuzzySystem(
            name=system_data["name"],
            status=FuzzySystemStatus.DRAFT,
            defuzzification_method=system_data.get("defuzzification_method", "centroid"),
            operators=operators,
            input_variable_ids=input_var_ids,
            output_variable_ids=output_var_ids,
            rule_ids=created_rule_ids,
        )
        created_system = await system_repo.create(new_system)

        # 7. Actualizar system_id en las reglas
        for rule_id in created_rule_ids:
            rule = await rule_repo.get_by_id(rule_id)
            if rule is not None:
                rule.system_id = created_system.id
                await rule_repo.update(rule)

        _logger.info(
            "Sistema importado exitosamente: '%s' (id=%s, %d vars, %d terms, %d rules)",
            created_system.name, str(created_system.id),
            len(created_variables), len(created_terms), len(created_rule_ids),
        )

        request._result = FuzzySystemDto.from_entity(created_system)
