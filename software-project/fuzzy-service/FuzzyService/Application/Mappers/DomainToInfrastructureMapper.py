from typing import List, Dict, Any, Optional
from datetime import datetime
from dataclasses import dataclass

from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule as DomainFuzzyRule
from FuzzyService.Domain.Entities.fuzzy_variable import FuzzyVariable as DomainFuzzyVariable
from FuzzyService.Domain.Entities.fuzzy_term import FuzzyTerm as DomainFuzzyTerm
from FuzzyService.Domain.ValueObjects.RuleCondition import RuleCondition as DomainRuleCondition
from FuzzyService.Domain.Enums import LogicalOperator as DomainLogicalOperator


# Intentar importar tipos de infraestructura; si no existen, usar placeholders compatibles
try:
    from FuzzyService.Infrastructure.FuzzyEngine.RuleEvaluationEngine import (
        InfraFuzzyRule,
        RuleCondition as InfraRuleCondition,
        RuleConsequent as InfraRuleConsequent,
        LogicalOperator as InfraLogicalOperator,
        )
    from FuzzyService.Infrastructure.FuzzyEngine.FuzzificationEngine import (
       FuzzyVariable as InfraFuzzyVariable,
       FuzzyTerm as InfraFuzzyTerm,
    )
    INFRA_AVAILABLE = True
except Exception:
   INFRA_AVAILABLE = False

@dataclass
class InfraRuleCondition:  # type: ignore
    sensor_id: str
    variable_name: str
    term_name: str

@dataclass
class InfraRuleConsequent:  # type: ignore
    variable_name: str
    term_name: str
    routine_id: str
    step_number: int = 1

class InfraLogicalOperator:  # type: ignore
    AND = "AND"
    OR = "OR"
    def __init__(self, value: str = "AND") -> None:
        self.value = value

@dataclass
class InfraFuzzyRule:  # type: ignore
    rule_id: str
    conditions: List[InfraRuleCondition]
    consequents: List[InfraRuleConsequent]
    logical_operator: Any | None = None
    description: str | None = None

@dataclass
class InfraFuzzyTerm:  # type: ignore
    term_id: str
    name: str
    function_type: str
    parameters: Dict[str, Any] | List[float] | None = None

@dataclass
class InfraFuzzyVariable:  # type: ignore
    variable_id: str
    name: str
    universe_range: tuple[float, float] | None = None
    terms: Dict[str, InfraFuzzyTerm] = None  # type: ignore
    sensor_mapping: Optional[str] = None


class DomainToInfrastructureMapper:
    """Mapper centralizado para convertir entidades de Domain a Infrastructure.
    
    Elimina la duplicación de entidades proporcionando conversiones limpias
    entre las representaciones de dominio e infraestructura.
    """
    
    @staticmethod
    def map_fuzzy_rule(domain_rule: DomainFuzzyRule) -> InfraFuzzyRule:
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
        
        # Mapear operador lógico
        logical_op = getattr(InfraLogicalOperator, 'AND', "AND")  # Default
        if hasattr(domain_rule, 'logical_operator') and domain_rule.logical_operator:
            try:
                logical_op = InfraLogicalOperator(domain_rule.logical_operator.value)
            except Exception:
                logical_op = getattr(InfraLogicalOperator, 'AND', "AND")
        
        return InfraFuzzyRule(
            rule_id=str(domain_rule.id) if domain_rule.id else "",
            conditions=infra_conditions,
            consequents=infra_consequents,
            logical_operator=logical_op,
            description=domain_rule.description or ""
        )
    
    @staticmethod
    def map_fuzzy_variable(domain_variable: DomainFuzzyVariable, 
                          terms: List[DomainFuzzyTerm]) -> InfraFuzzyVariable:
        """Convierte una FuzzyVariable del dominio a InfraFuzzyVariable."""
        # Calcular rango del universo basado en los términos
        min_val, max_val = 0.0, 100.0  # Default range
        if terms:
            all_params = []
            for term in terms:
                if term.mf and term.mf.params:
                    all_params.extend(term.mf.params.values())
            if all_params:
                min_val = min(all_params)
                max_val = max(all_params)
        
        # Convertir términos
        infra_terms = {}
        for term in terms:
            if term.variable_id == domain_variable.id:
                infra_term = DomainToInfrastructureMapper.map_fuzzy_term(term)
                infra_terms[term.label] = infra_term
        
        return InfraFuzzyVariable(
            variable_id=str(domain_variable.id) if domain_variable.id else "",
            name=domain_variable.name,
            universe_range=(min_val, max_val),
            terms=infra_terms,
            sensor_mapping=domain_variable.device_id
        )
    
    @staticmethod
    def map_fuzzy_term(domain_term: DomainFuzzyTerm) -> InfraFuzzyTerm:
        """Convierte un FuzzyTerm del dominio a InfraFuzzyTerm."""
        function_type = "triangular"  # Default
        parameters = {}
        
        if domain_term.mf:
            if domain_term.mf.type:
                function_type = domain_term.mf.type.value if hasattr(domain_term.mf.type, 'value') else str(domain_term.mf.type)
            if domain_term.mf.params:
                parameters = domain_term.mf.params
        
        return InfraFuzzyTerm(
            term_id=str(domain_term.id) if domain_term.id else "",
            name=domain_term.label,
            function_type=function_type,
            parameters=parameters
        )
    
    @staticmethod
    def map_rule_condition(domain_condition: DomainRuleCondition) -> InfraRuleCondition:
        """Convierte una RuleCondition del dominio a InfraRuleCondition."""
        return InfraRuleCondition(
            sensor_id=getattr(domain_condition, 'sensor_id', ''),
            variable_name=getattr(domain_condition, 'variable_name', ''),
            term_name=getattr(domain_condition, 'term_name', '')
        )
    
    @staticmethod
    def map_logical_operator(domain_operator: DomainLogicalOperator) -> InfraLogicalOperator:
        """Convierte un LogicalOperator del dominio a InfraLogicalOperator."""
        try:
            return InfraLogicalOperator(domain_operator.value)
        except Exception:
            return getattr(InfraLogicalOperator, 'AND', "AND")  # Default fallback
    
    @staticmethod
    def map_rules_batch(domain_rules: List[DomainFuzzyRule]) -> List[InfraFuzzyRule]:
        """Convierte una lista de reglas del dominio a infraestructura."""
        return [DomainToInfrastructureMapper.map_fuzzy_rule(rule) for rule in domain_rules]
    
    @staticmethod
    def map_variables_batch(domain_variables: List[DomainFuzzyVariable], 
                           all_terms: List[DomainFuzzyTerm]) -> List[InfraFuzzyVariable]:
        """Convierte una lista de variables del dominio a infraestructura."""
        infra_variables = []
        for variable in domain_variables:
            # Filtrar términos para esta variable
            variable_terms = [term for term in all_terms if term.variable_id == variable.id]
            infra_variable = DomainToInfrastructureMapper.map_fuzzy_variable(variable, variable_terms)
            infra_variables.append(infra_variable)
        return infra_variables