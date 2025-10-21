from typing import List, Dict, Any, Optional
from datetime import datetime, timezone
from dataclasses import dataclass

from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule as DomainFuzzyRule
from FuzzyService.Domain.Entities.fuzzy_variable import FuzzyVariable as DomainFuzzyVariable
from FuzzyService.Domain.Entities.fuzzy_term import FuzzyTerm as DomainFuzzyTerm
from FuzzyService.Domain.Entities.fuzzy_evaluation import (
    FuzzyEvaluation,
    InputValue,
    OutputValue,
    RuleActivation
)
from FuzzyService.Domain.ValueObjects.DomainId import (
    FuzzyRuleId,
    FuzzyVariableId,
    FuzzyTermId,
    FuzzySystemId,
    FuzzyEvaluationId
)
from FuzzyService.Domain.ValueObjects.MembershipFunction import MembershipFunction
from FuzzyService.Domain.Enums import (
    LogicalOperator as DomainLogicalOperator,
    RuleConnector,
    MembershipFunctionType
)


# Intentar importar tipos de infraestructura; si no existen, usar placeholders compatibles
try:
    from FuzzyService.Infrastructure.FuzzyEngine.RuleEvaluationEngine import (
        InfraFuzzyRule,
        RuleCondition as InfraRuleCondition,
        RuleConsequent as InfraRuleConsequent,
        LogicalOperator as InfraLogicalOperator,
        RuleEvaluationResult
    )
    from FuzzyService.Infrastructure.FuzzyEngine.FuzzificationEngine import (
        FuzzyVariable as InfraFuzzyVariable,
        FuzzyTerm as InfraFuzzyTerm
    )
    from FuzzyService.Infrastructure.FuzzyEngine.ScikitFuzzyEngine import (
        FuzzyEvaluationResponse
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

    @dataclass
    class FuzzyEvaluationResponse:  # type: ignore
        request_id: str
        system_id: str
        timestamp: datetime
        rule_activations: Dict[str, float]
        crisp_outputs: Dict[str, Dict[str, Dict[int, float]]]
        processing_time_ms: float = 0.0


    class InfrastructureToDomainMapper:
        """Mapper centralizado para convertir entidades de Infrastructure a Domain."""
    @staticmethod
    def map_logical_operator(infra_operator: InfraLogicalOperator) -> DomainLogicalOperator:
        """Convierte un InfraLogicalOperator a LogicalOperator del dominio."""
        try:
            return DomainLogicalOperator(infra_operator.value)
        except (ValueError, AttributeError):
            return DomainLogicalOperator.AND  # Default fallback
    
    @staticmethod
    def map_rules_batch(infra_rules: List[InfraFuzzyRule],
                       system_id: Optional[FuzzySystemId] = None) -> List[DomainFuzzyRule]:
        """Convierte una lista de reglas de infraestructura a dominio."""
        return [InfrastructureToDomainMapper.map_fuzzy_rule(rule, system_id) for rule in infra_rules]
    
    @staticmethod
    def map_variables_batch(infra_variables: List[InfraFuzzyVariable]) -> List[DomainFuzzyVariable]:
        """Convierte una lista de variables de infraestructura a dominio."""
        return [InfrastructureToDomainMapper.map_fuzzy_variable(var) for var in infra_variables]
    
    @staticmethod
    def map_terms_batch(infra_terms: List[InfraFuzzyTerm],
                       variable_id: Optional[FuzzyVariableId] = None) -> List[DomainFuzzyTerm]:
        """Convierte una lista de términos de infraestructura a dominio."""
        return [InfrastructureToDomainMapper.map_fuzzy_term(term, variable_id) for term in infra_terms]