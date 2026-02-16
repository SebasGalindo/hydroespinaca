from __future__ import annotations

import logging
from typing import Dict

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
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError, BusinessRuleViolationError

_logger = logging.getLogger(__name__)


class CloneFuzzySystemHandler(CommandHandler[CloneFuzzySystemCommand]):
    """Handler del comando CloneFuzzySystemCommand.
    
    Lógica de clonación (deep copy):
    1. Obtener el sistema original por ID (404 si no existe).
    2. Cargar todas las variables del sistema.
    3. Cargar todos los términos de cada variable.
    4. Cargar todas las reglas del sistema.
    5. Crear nuevas entidades con nuevos IDs, mapeando referencias:
       - variable_id viejo → variable_id nuevo
       - term_id viejo → term_id nuevo
       - system_id viejo → system_id nuevo
    6. Persistir todo en orden: variables → términos → reglas → sistema.
    7. Retornar el sistema clonado.
    """

    async def __call__(self, request: CloneFuzzySystemCommand) -> None:
        system_repo: IFuzzySystemRepository = di[IFuzzySystemRepository]
        variable_repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]
        term_repo: IFuzzyTermRepository = di[IFuzzyTermRepository]
        rule_repo: IFuzzyRuleRepository = di[IFuzzyRuleRepository]

        original_system_id = FuzzySystemId(request.id)

        # 1. Obtener el sistema original
        original_system = await system_repo.get_by_id(original_system_id)
        if original_system is None:
            raise EntityNotFoundError(f"Sistema difuso con id '{request.id}' no encontrado")

        _logger.info("Clonando sistema '%s' (id=%s)", original_system.name, request.id)

        # 2. Determinar nombre del clon
        clone_name = request.name.strip() if request.name else f"Copia de {original_system.name}"

        # Mapas de IDs viejos → nuevos
        variable_id_map: Dict[str, FuzzyVariableId] = {}  # old_var_id_str → new_var_id
        term_id_map: Dict[str, FuzzyTermId] = {}  # old_term_id_str → new_term_id

        # 3. Clonar variables
        all_original_var_ids = list(original_system.input_variable_ids) + list(original_system.output_variable_ids)
        new_input_variable_ids = []
        new_output_variable_ids = []

        for old_var_id in all_original_var_ids:
            original_var = await variable_repo.get_by_id(old_var_id)
            if original_var is None:
                _logger.warning("Variable '%s' referenciada en sistema pero no encontrada, omitiendo", str(old_var_id))
                continue

            # Crear nueva variable (sin ID para que el repo genere uno nuevo)
            new_var = FuzzyVariable(
                name=f"{original_var.name}",
                description=original_var.description,
                variable_type=original_var.variable_type,
                actuator_type=original_var.actuator_type,
                defuzzification_threshold=original_var.defuzzification_threshold,
                universe_min=original_var.universe_min,
                universe_max=original_var.universe_max,
                reference_code=original_var.reference_code,
                terms=[],  # Se actualizarán después de crear los términos
            )
            created_var = await variable_repo.create(new_var)
            variable_id_map[str(old_var_id)] = created_var.id
            _logger.debug("Variable clonada: '%s' → nuevo id=%s", original_var.name, str(created_var.id))

            # Clasificar en input/output
            if old_var_id in original_system.input_variable_ids:
                new_input_variable_ids.append(created_var.id)
            if old_var_id in original_system.output_variable_ids:
                new_output_variable_ids.append(created_var.id)

            # 4. Clonar términos de esta variable
            original_terms = await term_repo.get_by_variable_id(old_var_id)
            new_term_ids_for_var = []
            for original_term in original_terms:
                new_term = FuzzyTerm(
                    variable_id=created_var.id,
                    label=original_term.label,
                    membership_function=MembershipFunction(
                        function_type=original_term.membership_function.function_type,
                        parameters=list(original_term.membership_function.parameters),
                        universe_min=original_term.membership_function.universe_min,
                        universe_max=original_term.membership_function.universe_max,
                    ),
                )
                created_term = await term_repo.create(new_term)
                term_id_map[str(original_term.id)] = created_term.id
                new_term_ids_for_var.append(created_term.id)
                _logger.debug("Término clonado: '%s' → nuevo id=%s", original_term.label, str(created_term.id))

            # Actualizar la variable con los nuevos IDs de términos
            if new_term_ids_for_var:
                created_var.terms = new_term_ids_for_var
                await variable_repo.update(created_var)

        # 5. Clonar reglas
        new_rule_ids = []
        for old_rule_id in original_system.rule_ids:
            original_rule = await rule_repo.get_by_id(old_rule_id)
            if original_rule is None:
                _logger.warning("Regla '%s' referenciada en sistema pero no encontrada, omitiendo", str(old_rule_id))
                continue

            # Remapear condiciones: actualizar variableId a los nuevos IDs
            new_conditions = []
            for condition in original_rule.conditions:
                old_cond_var_id = str(condition.get("variableId", ""))
                new_cond_var_id = variable_id_map.get(old_cond_var_id)
                if new_cond_var_id is None:
                    _logger.warning(
                        "Variable de condición '%s' no encontrada en el mapa de clonación, manteniendo original",
                        old_cond_var_id,
                    )
                    new_cond_var_id = condition.get("variableId")
                new_conditions.append({
                    "variableId": new_cond_var_id,
                    "operator": condition.get("operator"),
                    "value": condition.get("value"),
                })

            # Remapear consecuentes: actualizar variable_id y terms a nuevos IDs
            new_consequents = []
            for consequent in original_rule.consequents:
                old_cons_var_id = str(consequent.variable_id)
                new_cons_var_id = variable_id_map.get(old_cons_var_id)
                if new_cons_var_id is None:
                    _logger.warning(
                        "Variable de consecuente '%s' no encontrada en mapa, manteniendo original",
                        old_cons_var_id,
                    )
                    new_cons_var_id = consequent.variable_id

                new_cons_terms = []
                for old_term_id in consequent.terms:
                    new_term_id = term_id_map.get(str(old_term_id))
                    if new_term_id is None:
                        _logger.warning(
                            "Término de consecuente '%s' no encontrado en mapa, manteniendo original",
                            str(old_term_id),
                        )
                        new_term_id = old_term_id
                    new_cons_terms.append(new_term_id)

                new_consequents.append(RuleConsequent(
                    variable_id=new_cons_var_id,
                    terms=new_cons_terms,
                    aggregation_method=consequent.aggregation_method,
                ))

            # Crear la nueva regla
            new_rule = FuzzyRule(
                name=original_rule.name,
                system_id=None,  # Se asignará después de crear el sistema
                description=original_rule.description,
                conditions=new_conditions,
                connectors=list(original_rule.connectors),
                consequents=new_consequents,
            )
            created_rule = await rule_repo.create(new_rule)
            new_rule_ids.append(created_rule.id)
            _logger.debug("Regla clonada: '%s' → nuevo id=%s", original_rule.name, str(created_rule.id))

        # 6. Crear el sistema clonado
        new_system = FuzzySystem(
            name=clone_name,
            status=FuzzySystemStatus.DRAFT,
            defuzzification_method=original_system.defuzzification_method,
            operators=OperatorsConfig(
                and_method=original_system.operators.and_method,
                or_method=original_system.operators.or_method,
                not_method=original_system.operators.not_method,
            ),
            input_variable_ids=new_input_variable_ids,
            output_variable_ids=new_output_variable_ids,
            rule_ids=new_rule_ids,
            created_by=original_system.created_by,
        )
        created_system = await system_repo.create(new_system)

        # 7. Actualizar system_id en las reglas clonadas
        for new_rule_id in new_rule_ids:
            rule = await rule_repo.get_by_id(new_rule_id)
            if rule is not None:
                rule.system_id = created_system.id
                await rule_repo.update(rule)

        _logger.info(
            "Sistema clonado exitosamente: '%s' → '%s' (id=%s, %d vars, %d terms, %d rules)",
            original_system.name,
            created_system.name,
            str(created_system.id),
            len(new_input_variable_ids) + len(new_output_variable_ids),
            len(term_id_map),
            len(new_rule_ids),
        )

        # 8. Re-fetch para retornar datos actualizados
        final_system = await system_repo.get_by_id(created_system.id)
        request._result = FuzzySystemDto.from_entity(final_system)
