from typing import List, Dict, Any, Union
from dataclasses import dataclass

from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule as DomainFuzzyRule
from FuzzyService.Domain.ValueObjects.DomainId import (
    FuzzyRuleId, FuzzySystemId, FuzzyRoutineId, FuzzyVariableId
)
from FuzzyService.Domain.Enums import LogicalOperator as DomainLogicalOperator, RuleConnector

# Intentar importar tipos de infraestructura; si no existen, usar clases placeholder compatibles
try:
    from FuzzyService.Infrastructure.FuzzyEngine.RuleEvaluationEngine import (
        InfraFuzzyRule,
        RuleCondition as InfraRuleCondition,
        RuleConsequent as InfraRuleConsequent,
    )
    from FuzzyService.Infrastructure.FuzzyEngine.FuzzyEngineConfiguration import (
        LogicalOperator as InfraLogicalOperator,
    )
    INFRA_AVAILABLE = True
except Exception:
    INFRA_AVAILABLE = False

    @dataclass
    class InfraRuleCondition:  # type: ignore
        sensor_id: str
        variable_name: str
        term_name: str
        operator: Any | None = None

    @dataclass
    class InfraRuleConsequent:  # type: ignore
        variable_name: str
        term_name: str
        routine_id: str
        step_number: int = 1

    class InfraLogicalOperator:  # type: ignore
        AND = "AND"
        OR = "OR"

    @dataclass
    class InfraFuzzyRule:  # type: ignore
        rule_id: str
        conditions: List[InfraRuleCondition]
        consequents: List[InfraRuleConsequent]
        logical_operator: Any
        description: str = ""
        priority: int | None = None


class FuzzyRuleMapper:
    """Mapper para conversiones entre FuzzyRule de dominio y estructura Infra para el motor."""

    @staticmethod
    def to_infra(domain_rule: DomainFuzzyRule) -> Any:
        """Convierte una FuzzyRule del dominio a InfraFuzzyRule (o placeholder compatible)."""
        # Mapear condiciones
        infra_conditions: List[InfraRuleCondition] = []
        for c in domain_rule.conditions:
            var_id = c.get("variableId")
            var_id_str = str(var_id) if var_id is not None else ""
            term_name = c.get("value")
            infra_conditions.append(
                InfraRuleCondition(
                    sensor_id=var_id_str,
                    variable_name=var_id_str,
                    term_name=term_name,
                )
            )

        # Consecuentes (uno por ahora)
        infra_consequents: List[InfraRuleConsequent] = []
        if domain_rule.consequent:
            infra_consequents.append(
                InfraRuleConsequent(
                    variable_name="output",
                    term_name="activated",
                    routine_id=str(domain_rule.consequent),
                    step_number=1,
                )
            )

        # Operador lógico global según conectores (si hay múltiples condiciones)
        if len(domain_rule.conditions) <= 1:
            logical_op = InfraLogicalOperator.AND
        else:
            # Si existen conectores, se exige consistencia: todos iguales para proyectarlos a un operador global
            if not domain_rule.connectors or len(domain_rule.connectors) != len(domain_rule.conditions) - 1:
                raise ValueError("Cardinalidad de conectores inválida para proyectar operador lógico global")
            first = domain_rule.connectors[0]
            if any(c != first for c in domain_rule.connectors):
                # Evitar semánticas ambiguas
                raise ValueError("Mezcla de conectores no soportada para operador lógico global del motor")
            logical_op = InfraLogicalOperator.AND if first == RuleConnector.AND else InfraLogicalOperator.OR

        return InfraFuzzyRule(
            rule_id=str(domain_rule.id) if domain_rule.id else "",
            conditions=infra_conditions,
            consequents=infra_consequents,
            logical_operator=logical_op,
            description=domain_rule.description or "",
        )

    @staticmethod
    def to_dict(domain_rule: DomainFuzzyRule) -> Dict[str, Any]:
        """Convierte una FuzzyRule de dominio a un dict compatible con ScikitFuzzyEngine._normalize_rules."""
        # Condiciones en formato flexible
        conds: List[Dict[str, Any]] = []
        for c in (domain_rule.conditions or []):
            var_id = c.get("variableId")
            var_str = str(var_id) if var_id is not None else ""
            term = c.get("value")
            conds.append({
                "sensor_id": var_str,
                "variable_id": var_str,
                "variable_name": var_str,
                "term_name": term,
            })

        # Conectores (si existen)
        connectors: List[str] = []
        for conn in (domain_rule.connectors or []):
            connectors.append(conn.value if hasattr(conn, "value") else str(conn))

        # Consecuente simplificado (el motor admite 'consequent' simple)
        payload: Dict[str, Any] = {
            "rule_id": str(domain_rule.id) if domain_rule.id else "",
            "conditions": conds,
            "description": domain_rule.description or "",
        }
        if domain_rule.consequent:
            payload["consequent"] = str(domain_rule.consequent)
        if connectors:
            payload["connectors"] = connectors

        return payload

    @staticmethod
    def to_domain(infra_obj: Union[Dict[str, Any], Any], system_id: str | None) -> DomainFuzzyRule:
        """Convierte un objeto InfraFuzzyRule o dict serializado a FuzzyRule del dominio."""
        # Normalizar a objeto infra
        if isinstance(infra_obj, dict):
            conds = infra_obj.get("conditions") or []
            cons_raw = infra_obj.get("consequent")

            domain_conditions: List[Dict[str, Any]] = []
            for c in conds:
                var = c.get("variableId") or c.get("variable_id") or c.get("variable_name")
                var_id = FuzzyVariableId(var) if var is not None else None
                op = c.get("operator") or DomainLogicalOperator.IS
                val = c.get("value") or c.get("term_name")
                domain_conditions.append({
                    "variableId": var_id,
                    "operator": op if isinstance(op, DomainLogicalOperator) else DomainLogicalOperator(str(op)),
                    "value": val,
                })

            # Conectores
            connectors: List[RuleConnector] = []
            raw_connectors = infra_obj.get("connectors") or []
            for c in raw_connectors:
                connectors.append(c if isinstance(c, RuleConnector) else RuleConnector(str(c)))

            consequent = FuzzyRoutineId(cons_raw) if cons_raw else None

            return DomainFuzzyRule(
                id=FuzzyRuleId(infra_obj.get("rule_id")) if infra_obj.get("rule_id") else None,
                name=infra_obj.get("name") or "Generated Rule",
                system_id=FuzzySystemId(system_id) if system_id else None,
                description=infra_obj.get("description"),
                conditions=domain_conditions,
                connectors=connectors,
                consequent=consequent,
                created_at=None,
            )

        # Caso: objeto InfraFuzzyRule
        infra_rule = infra_obj

        # Reconstruir condiciones a formato de dominio
        domain_conditions: List[Dict[str, Any]] = []
        for ic in (getattr(infra_rule, "conditions", []) or []):
            var_name = getattr(ic, "variable_name", None) or getattr(ic, "sensor_id", None)
            term_name = getattr(ic, "term_name", None)
            domain_conditions.append({
                "variableId": FuzzyVariableId(var_name) if var_name else None,
                "operator": DomainLogicalOperator.IS,
                "value": term_name,
            })

        # Mapear conectores según el operador lógico global, manteniendo cardinalidad
        connectors: List[RuleConnector] = []
        if len(domain_conditions) > 1:
            op_val = getattr(infra_rule, "logical_operator", getattr(InfraLogicalOperator, "AND", "AND"))
            mapped = RuleConnector.AND if op_val == getattr(InfraLogicalOperator, "AND", "AND") else RuleConnector.OR
            connectors = [mapped] * (len(domain_conditions) - 1)

        # Consecuente
        consequent = None
        conseq_list = getattr(infra_rule, "consequents", None)
        if conseq_list:
            first = conseq_list[0]
            rid = getattr(first, "routine_id", None)
            if rid:
                consequent = FuzzyRoutineId(rid)

        return DomainFuzzyRule(
            id=FuzzyRuleId(getattr(infra_rule, "rule_id", None)) if getattr(infra_rule, "rule_id", None) else None,
            name="Generated Rule",
            system_id=FuzzySystemId(system_id) if system_id else None,
            description=getattr(infra_rule, "description", None),
            conditions=domain_conditions,
            connectors=connectors,
            consequent=consequent,
            created_at=None,
        )