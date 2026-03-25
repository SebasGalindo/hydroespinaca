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
    Usa operaciones bulk para minimizar round-trips a la base de datos.
    """

    async def __call__(self, request: ImportFuzzySystemCommand) -> None:
        system_repo: IFuzzySystemRepository = di[IFuzzySystemRepository]
        variable_repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]
        term_repo: IFuzzyTermRepository = di[IFuzzyTermRepository]
        rule_repo: IFuzzyRuleRepository = di[IFuzzyRuleRepository]

        system_data = request.system
        _logger.info("Importando sistema '%s' (version=%s)", system_data.get("name"), request.version)

        # ── 1. Crear sistema vacío primero (para tener system_id real) ──
        operators_data = system_data.get("operators", {})
        operators = OperatorsConfig.from_dict(operators_data) if operators_data else OperatorsConfig()

        empty_system = FuzzySystem(
            name=system_data["name"],
            status=FuzzySystemStatus.DRAFT,
            defuzzification_method=system_data.get("defuzzification_method", "centroid"),
            operators=operators,
            input_variable_ids=[],
            output_variable_ids=[],
            rule_ids=[],
        )
        created_system = await system_repo.create(empty_system)
        new_system_id = created_system.id
        _logger.info("Sistema importado (vacío): id=%s", str(new_system_id))

        # ── 2. Bulk-crear variables ─────────────────────────────────
        vars_to_insert: List[FuzzyVariable] = []
        for var_data in request.variables:
            vars_to_insert.append(FuzzyVariable(
                name=var_data["name"],
                description=var_data.get("description", ""),
                variable_type=var_data.get("variable_type", "input"),
                actuator_type=var_data.get("actuator_type"),
                defuzzification_threshold=var_data.get("defuzzification_threshold", 50.0),
                universe_min=var_data.get("universe_min"),
                universe_max=var_data.get("universe_max"),
                reference_code=var_data.get("reference_code"),
                terms=[],
            ))

        created_variables = await variable_repo.create_many(vars_to_insert)
        _logger.debug("Variables importadas: %d", len(created_variables))

        # ── 3. Preparar y bulk-crear términos ───────────────────────
        terms_to_insert: List[FuzzyTerm] = []
        for idx, term_data in enumerate(request.terms):
            var_ref = term_data.get("variable_ref")
            if var_ref is None or var_ref < 0 or var_ref >= len(created_variables):
                raise ValidationError(
                    f"Término [{idx}]: variable_ref={var_ref} fuera de rango (0-{len(created_variables)-1})"
                )
            target_var = created_variables[var_ref]
            mf_data = term_data.get("membership_function", {})

            terms_to_insert.append(FuzzyTerm(
                variable_id=target_var.id,
                label=term_data["label"],
                membership_function=MembershipFunction(
                    function_type=MembershipFunctionType(mf_data["function_type"]),
                    parameters=list(mf_data["parameters"]),
                    universe_min=mf_data["universe_min"],
                    universe_max=mf_data["universe_max"],
                ),
            ))

        created_terms = await term_repo.create_many(terms_to_insert)
        _logger.debug("Términos importados: %d", len(created_terms))

        # ── 4. Bulk-update variables con sus term_ids ───────────────
        # Agrupar terms por variable
        var_term_map: Dict[str, List[FuzzyTermId]] = {}
        for ct in created_terms:
            key = str(ct.variable_id)
            var_term_map.setdefault(key, []).append(ct.id)

        term_updates = [
            (cv.id, var_term_map.get(str(cv.id), []))
            for cv in created_variables
            if str(cv.id) in var_term_map
        ]
        await variable_repo.update_many_terms(term_updates)

        # ── 5. Clasificar variables en input/output ─────────────────
        input_var_ids = [v.id for v in created_variables if v.variable_type == "input"]
        output_var_ids = [v.id for v in created_variables if v.variable_type == "output"]

        # ── 6. Preparar y bulk-crear reglas (con system_id real) ────
        rules_to_insert: List[FuzzyRule] = []
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

            rules_to_insert.append(FuzzyRule(
                name=rule_data["name"],
                system_id=new_system_id,
                description=rule_data.get("description"),
                conditions=conditions,
                connectors=connectors,
                consequents=consequents,
            ))

        created_rules = await rule_repo.create_many(rules_to_insert)
        new_rule_ids = [r.id for r in created_rules]

        # ── 7. Actualizar sistema con variable_ids y rule_ids ───────
        created_system.input_variable_ids = input_var_ids
        created_system.output_variable_ids = output_var_ids
        created_system.rule_ids = new_rule_ids
        final_system = await system_repo.update(created_system)

        _logger.info(
            "Sistema importado exitosamente: '%s' (id=%s, %d vars, %d terms, %d rules)",
            final_system.name, str(final_system.id),
            len(created_variables), len(created_terms), len(new_rule_ids),
        )

        request._result = FuzzySystemDto.from_entity(final_system)
