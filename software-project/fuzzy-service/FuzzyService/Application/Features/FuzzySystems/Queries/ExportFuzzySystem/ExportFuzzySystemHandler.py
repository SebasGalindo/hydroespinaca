from __future__ import annotations

import logging
from datetime import datetime, timezone
from typing import Any, Dict, List

from medyator import QueryHandler
from kink import di

from FuzzyService.Application.Features.FuzzySystems.Queries.ExportFuzzySystem.ExportFuzzySystemQuery import ExportFuzzySystemQuery
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError

_logger = logging.getLogger(__name__)


class ExportFuzzySystemHandler(QueryHandler[ExportFuzzySystemQuery, Dict[str, Any]]):
    """Handler de la query ExportFuzzySystemQuery.
    
    Genera un JSON portátil del sistema completo.
    Usa índices de posición en lugar de IDs para que el archivo sea portable.
    
    Formato de salida:
    {
        "version": "1.0",
        "exported_at": "...",
        "system": { name, defuzzification_method, operators },
        "variables": [ { name, variable_type, ... } ],
        "terms": [ { variable_ref (índice), label, membership_function } ],
        "rules": [ { name, conditions (con variable_ref), connectors, consequents (con variable_ref, term_refs) } ]
    }
    """

    async def __call__(self, request: ExportFuzzySystemQuery) -> Dict[str, Any]:  # type: ignore[override]
        system_repo: IFuzzySystemRepository = di[IFuzzySystemRepository]
        variable_repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]
        term_repo: IFuzzyTermRepository = di[IFuzzyTermRepository]
        rule_repo: IFuzzyRuleRepository = di[IFuzzyRuleRepository]

        system_id = FuzzySystemId(request.id)

        # 1. Obtener el sistema
        system = await system_repo.get_by_id(system_id)
        if system is None:
            raise EntityNotFoundError(f"Sistema difuso con id '{request.id}' no encontrado")

        # 2. Cargar todas las variables y construir mapa de posición
        all_var_ids = list(system.input_variable_ids) + list(system.output_variable_ids)
        variables_data: List[Dict[str, Any]] = []
        var_id_to_index: Dict[str, int] = {}  # var_id_str → posición en el array

        for idx, var_id in enumerate(all_var_ids):
            var = await variable_repo.get_by_id(var_id)
            if var is None:
                _logger.warning("Variable '%s' no encontrada, omitiendo en export", str(var_id))
                continue

            var_id_to_index[str(var_id)] = len(variables_data)
            variables_data.append({
                "name": var.name,
                "description": var.description,
                "variable_type": var.variable_type,
                "actuator_type": var.actuator_type,
                "defuzzification_threshold": var.defuzzification_threshold,
                "universe_min": var.universe_min,
                "universe_max": var.universe_max,
                "reference_code": var.reference_code,
            })

        # 3. Cargar todos los términos y construir mapa de posición
        terms_data: List[Dict[str, Any]] = []
        term_id_to_index: Dict[str, int] = {}  # term_id_str → posición en el array

        for var_id in all_var_ids:
            var_str = str(var_id)
            if var_str not in var_id_to_index:
                continue
            var_ref = var_id_to_index[var_str]

            original_terms = await term_repo.get_by_variable_id(var_id)
            for term in original_terms:
                term_id_to_index[str(term.id)] = len(terms_data)
                mf = term.membership_function
                terms_data.append({
                    "variable_ref": var_ref,
                    "label": term.label,
                    "membership_function": {
                        "function_type": mf.function_type.value if hasattr(mf.function_type, 'value') else str(mf.function_type),
                        "parameters": list(mf.parameters),
                        "universe_min": mf.universe_min,
                        "universe_max": mf.universe_max,
                    },
                })

        # 4. Cargar reglas y remapear a índices
        rules_data: List[Dict[str, Any]] = []

        for rule_id in system.rule_ids:
            rule = await rule_repo.get_by_id(rule_id)
            if rule is None:
                _logger.warning("Regla '%s' no encontrada, omitiendo en export", str(rule_id))
                continue

            # Remapear condiciones
            export_conditions = []
            for cond in rule.conditions:
                var_id_str = str(cond.get("variableId", ""))
                var_ref = var_id_to_index.get(var_id_str)
                operator = cond.get("operator")
                op_value = operator.value if hasattr(operator, 'value') else str(operator)
                export_conditions.append({
                    "variable_ref": var_ref,
                    "operator": op_value,
                    "value": cond.get("value"),
                })

            # Remapear conectores
            export_connectors = []
            for conn in rule.connectors:
                export_connectors.append(conn.value if hasattr(conn, 'value') else str(conn))

            # Remapear consecuentes
            export_consequents = []
            for cons in rule.consequents:
                var_id_str = str(cons.variable_id)
                var_ref = var_id_to_index.get(var_id_str)
                term_refs = []
                for tid in cons.terms:
                    t_ref = term_id_to_index.get(str(tid))
                    term_refs.append(t_ref)
                export_consequents.append({
                    "variable_ref": var_ref,
                    "term_refs": term_refs,
                    "aggregation_method": cons.aggregation_method,
                })

            rules_data.append({
                "name": rule.name,
                "description": rule.description,
                "conditions": export_conditions,
                "connectors": export_connectors,
                "consequents": export_consequents,
            })

        # 5. Construir el JSON de exportación
        operators = system.operators
        export_data = {
            "version": "1.0",
            "exported_at": datetime.now(timezone.utc).isoformat(),
            "system": {
                "name": system.name,
                "defuzzification_method": system.defuzzification_method.value if hasattr(system.defuzzification_method, 'value') else str(system.defuzzification_method),
                "operators": operators.to_dict() if hasattr(operators, 'to_dict') else operators,
            },
            "variables": variables_data,
            "terms": terms_data,
            "rules": rules_data,
        }

        _logger.info(
            "Sistema '%s' exportado: %d variables, %d términos, %d reglas",
            system.name, len(variables_data), len(terms_data), len(rules_data),
        )

        return export_data
