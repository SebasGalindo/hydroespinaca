from typing import List, Dict, Any, Optional
from datetime import datetime

from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule as DomainFuzzyRule
from FuzzyService.Domain.ValueObjects.RuleCondition import RuleCondition as DomainRuleCondition
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyRuleId, FuzzySystemId, FuzzyRoutineId
from FuzzyService.Domain.Enums import LogicalOperator as DomainLogicalOperator, RuleConnector

from FuzzyService.Infrastructure.FuzzyEngine.RuleEvaluationEngine import (
    InfraFuzzyRule,
    RuleCondition as InfraRuleCondition,
    RuleConsequent as InfraRuleConsequent,
    LogicalOperator as InfraLogicalOperator
)


class FuzzyRuleMapper:
    """Mapper específico para conversiones entre FuzzyRule de dominio e infraestructura."""
    
    @staticmethod
    def to_infra(domain_rule: DomainFuzzyRule) -> InfraFuzzyRule:
        """Convierte una FuzzyRule del dominio a InfraFuzzyRule."""
        # Convertir condiciones
        infra_conditions = []
        for condition_dict in domain_rule.conditions:
            infra_condition = InfraRuleCondition(
                sensor_id=condition_dict.get("sensor_id", ""),
                variable_name=condition_dict.get("variable_name", ""),
                term_name=condition_dict.get("term_name", "")
            )
            infra_conditions.append(infra_condition)
        
        # Convertir consecuentes
        infra_consequents = []
        if domain_rule.consequent:
            infra_consequent = InfraRuleConsequent(
                variable_name="output",
                term_name="activated",
                routine_id=str(domain_rule.consequent),
                step_number=1
            )
            infra_consequents.append(infra_consequent)
        
        # Convertir operador lógico
        infra_operator = FuzzyRuleMapper._map_logical_operator_to_infra(
            domain_rule.connector
        )
        
        return InfraFuzzyRule(
            rule_id=str(domain_rule.id),
            description=domain_rule.description,
            conditions=infra_conditions,
            consequents=infra_consequents,
            logical_operator=infra_operator,
            priority=domain_rule.priority,
            is_active=domain_rule.is_active
        )
    
    @staticmethod
    def to_domain(infra_rule: InfraFuzzyRule, system_id: str) -> DomainFuzzyRule:
        """Convierte una InfraFuzzyRule a FuzzyRule del dominio."""
        # Convertir condiciones
        domain_conditions = []
        for infra_condition in infra_rule.conditions:
            condition_dict = {
                "sensor_id": infra_condition.sensor_id,
                "variable_name": infra_condition.variable_name,
                "term_name": infra_condition.term_name
            }
            domain_conditions.append(condition_dict)
        
        # Obtener consecuente (asumiendo que hay uno)
        consequent = None
        if infra_rule.consequents:
            consequent = FuzzyRoutineId(infra_rule.consequents[0].routine_id)
        
        # Convertir operador lógico
        domain_connector = FuzzyRuleMapper._map_logical_operator_to_domain(
            infra_rule.logical_operator
        )
        
        return DomainFuzzyRule(
            id=FuzzyRuleId(infra_rule.rule_id),
            system_id=FuzzySystemId(system_id),
            description=infra_rule.description,
            conditions=domain_conditions,
            consequent=consequent,
            connector=domain_connector,
            priority=infra_rule.priority,
            is_active=infra_rule.is_active
        )
    
    @staticmethod
    def _map_logical_operator_to_infra(domain_operator: RuleConnector) -> InfraLogicalOperator:
        """Convierte operador lógico del dominio a infraestructura."""
        mapping = {
            RuleConnector.AND: InfraLogicalOperator.AND,
            RuleConnector.OR: InfraLogicalOperator.OR
        }
        return mapping.get(domain_operator, InfraLogicalOperator.AND)
    
    @staticmethod
    def _map_logical_operator_to_domain(infra_operator: InfraLogicalOperator) -> RuleConnector:
        """Convierte operador lógico de infraestructura a dominio."""
        mapping = {
            InfraLogicalOperator.AND: RuleConnector.AND,
            InfraLogicalOperator.OR: RuleConnector.OR
        }
        return mapping.get(infra_operator, RuleConnector.AND)