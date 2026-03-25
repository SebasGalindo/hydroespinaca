from __future__ import annotations

import logging
from typing import Dict, List

from medyator import CommandHandler
from kink import di

from FuzzyService.Application.Features.FuzzySystems.DTOs.FuzzySystemDto import FuzzySystemDto
from FuzzyService.Application.Features.FuzzySystems.Commands.CloneFuzzySystem.CloneFuzzySystemCommand import CloneFuzzySystemCommand
from FuzzyService.Domain.ValueObjects.DomainId import (
    FuzzySystemId,
    FuzzyVariableId,
    FuzzyTermId,
    FuzzyRuleId,
)
from FuzzyService.Domain.Enums.EntityStatus import FuzzySystemStatus
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
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError

_logger = logging.getLogger(__name__)


class CloneFuzzySystemHandler(CommandHandler[CloneFuzzySystemCommand]):
    """Handler del comando CloneFuzzySystemCommand.

    Clonación optimizada (deep copy) usando operaciones bulk:
    1. Obtener sistema original + variables + términos + reglas.
    2. Crear el sistema clon vacío (→ system_id real).
    3. Bulk-insert variables nuevas → mapa old→new variable_id.
    4. Bulk-insert términos nuevos con variable_id remapeado → mapa old→new term_id.
    5. Bulk-update variables con sus nuevos term_ids.
    6. Bulk-insert reglas con system_id real y IDs remapeados.
    7. Actualizar sistema con variable_ids y rule_ids finales.
    """

    async def __call__(self, request: CloneFuzzySystemCommand) -> None:
        system_repo: IFuzzySystemRepository = di[IFuzzySystemRepository]
        variable_repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]
        term_repo: IFuzzyTermRepository = di[IFuzzyTermRepository]
        rule_repo: IFuzzyRuleRepository = di[IFuzzyRuleRepository]

        original_system_id = FuzzySystemId(request.id)

        # ── 1. Obtener el sistema original ──────────────────────────
        original_system = await system_repo.get_by_id(original_system_id)
        if original_system is None:
            raise EntityNotFoundError(f"Sistema difuso con id '{request.id}' no encontrado")

        _logger.info("Clonando sistema '%s' (id=%s)", original_system.name, request.id)
        clone_name = request.name.strip() if request.name else f"Copia de {original_system.name}"

        # ── 2. Cargar variables originales (una por una, pero necesario por IDs sueltos) ──
        all_original_var_ids = list(original_system.input_variable_ids) + list(original_system.output_variable_ids)
        original_vars: List[FuzzyVariable] = []
        for vid in all_original_var_ids:
            v = await variable_repo.get_by_id(vid)
            if v is not None:
                original_vars.append(v)
            else:
                _logger.warning("Variable '%s' referenciada pero no encontrada, omitiendo", str(vid))

        # Cargar todos los términos de todas las variables de golpe
        all_original_terms: List[FuzzyTerm] = []
        for v in original_vars:
            terms = await term_repo.get_by_variable_id(v.id)
            all_original_terms.extend(terms)

        # Cargar todas las reglas del sistema de golpe
        original_rules = await rule_repo.get_by_system_id(original_system_id, skip=0, limit=10000)

        _logger.info(
            "Datos cargados: %d vars, %d terms, %d rules",
            len(original_vars), len(all_original_terms), len(original_rules),
        )

        # ── 3. Crear sistema clon vacío ─────────────────────────────
        empty_system = FuzzySystem(
            name=clone_name,
            status=FuzzySystemStatus.DRAFT,
            defuzzification_method=original_system.defuzzification_method,
            operators=OperatorsConfig(
                and_method=original_system.operators.and_method,
                or_method=original_system.operators.or_method,
                not_method=original_system.operators.not_method,
            ),
            input_variable_ids=[],
            output_variable_ids=[],
            rule_ids=[],
            created_by=original_system.created_by,
        )
        created_system = await system_repo.create(empty_system)
        new_system_id = created_system.id
        _logger.info("Sistema clon creado (vacío): id=%s", str(new_system_id))

        # ── 4. Bulk-crear variables ─────────────────────────────────
        variable_id_map: Dict[str, FuzzyVariableId] = {}
        vars_to_insert: List[FuzzyVariable] = []
        for orig_var in original_vars:
            new_var = FuzzyVariable(
                name=orig_var.name,
                description=orig_var.description,
                variable_type=orig_var.variable_type,
                actuator_type=orig_var.actuator_type,
                defuzzification_threshold=orig_var.defuzzification_threshold,
                universe_min=orig_var.universe_min,
                universe_max=orig_var.universe_max,
                reference_code=orig_var.reference_code,
                terms=[],
            )
            vars_to_insert.append(new_var)

        created_vars = await variable_repo.create_many(vars_to_insert)
        for orig_var, new_var in zip(original_vars, created_vars):
            variable_id_map[str(orig_var.id)] = new_var.id

        # Clasificar en input / output
        input_id_set = {str(v) for v in original_system.input_variable_ids}
        output_id_set = {str(v) for v in original_system.output_variable_ids}
        new_input_ids = [variable_id_map[str(v.id)] for v in original_vars if str(v.id) in input_id_set]
        new_output_ids = [variable_id_map[str(v.id)] for v in original_vars if str(v.id) in output_id_set]

        # ── 5. Bulk-crear términos ──────────────────────────────────
        term_id_map: Dict[str, FuzzyTermId] = {}
        terms_to_insert: List[FuzzyTerm] = []
        for orig_term in all_original_terms:
            new_var_id = variable_id_map.get(str(orig_term.variable_id))
            if new_var_id is None:
                _logger.warning("Término '%s' referencia variable no mapeada, omitiendo", str(orig_term.id))
                continue
            new_term = FuzzyTerm(
                variable_id=new_var_id,
                label=orig_term.label,
                membership_function=MembershipFunction(
                    function_type=orig_term.membership_function.function_type,
                    parameters=list(orig_term.membership_function.parameters),
                    universe_min=orig_term.membership_function.universe_min,
                    universe_max=orig_term.membership_function.universe_max,
                ),
            )
            terms_to_insert.append(new_term)

        created_terms = await term_repo.create_many(terms_to_insert)
        # Construir el mapa old_term_id → new_term_id
        insert_idx = 0
        for orig_term in all_original_terms:
            if str(orig_term.variable_id) in variable_id_map:
                term_id_map[str(orig_term.id)] = created_terms[insert_idx].id
                insert_idx += 1

        # ── 6. Bulk-update variables con sus term_ids ───────────────
        # Agrupar terms por new_variable_id
        var_term_map: Dict[str, List[FuzzyTermId]] = {}
        for ct in created_terms:
            key = str(ct.variable_id)
            var_term_map.setdefault(key, []).append(ct.id)

        term_updates = [
            (new_var.id, var_term_map.get(str(new_var.id), []))
            for new_var in created_vars
            if str(new_var.id) in var_term_map
        ]
        await variable_repo.update_many_terms(term_updates)

        # ── 7. Bulk-crear reglas con system_id real ─────────────────
        rules_to_insert: List[FuzzyRule] = []
        for orig_rule in original_rules:
            # Remapear condiciones
            new_conditions = []
            for cond in orig_rule.conditions:
                old_cond_var = str(cond.get("variableId", ""))
                new_cond_var = variable_id_map.get(old_cond_var, cond.get("variableId"))
                new_conditions.append({
                    "variableId": new_cond_var,
                    "operator": cond.get("operator"),
                    "value": cond.get("value"),
                })

            # Remapear consecuentes
            new_consequents = []
            for cons in orig_rule.consequents:
                new_cons_var = variable_id_map.get(str(cons.variable_id), cons.variable_id)
                new_cons_terms = [term_id_map.get(str(t), t) for t in cons.terms]
                new_consequents.append(RuleConsequent(
                    variable_id=new_cons_var,
                    terms=new_cons_terms,
                    aggregation_method=cons.aggregation_method,
                ))

            rules_to_insert.append(FuzzyRule(
                name=orig_rule.name,
                system_id=new_system_id,
                description=orig_rule.description,
                conditions=new_conditions,
                connectors=list(orig_rule.connectors),
                consequents=new_consequents,
            ))

        created_rules = await rule_repo.create_many(rules_to_insert)
        new_rule_ids = [r.id for r in created_rules]

        # ── 8. Actualizar sistema con variable_ids y rule_ids ───────
        created_system.input_variable_ids = new_input_ids
        created_system.output_variable_ids = new_output_ids
        created_system.rule_ids = new_rule_ids
        final_system = await system_repo.update(created_system)

        _logger.info(
            "Sistema clonado exitosamente: '%s' → '%s' (id=%s, %d vars, %d terms, %d rules)",
            original_system.name,
            final_system.name,
            str(final_system.id),
            len(new_input_ids) + len(new_output_ids),
            len(term_id_map),
            len(new_rule_ids),
        )

        request._result = FuzzySystemDto.from_entity(final_system)
