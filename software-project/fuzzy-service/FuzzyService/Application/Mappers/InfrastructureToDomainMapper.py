from typing import List, Dict, Any, Optional
from datetime import datetime, timezone

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
    FuzzyEvaluationId,
    FuzzyRoutineId
)
from FuzzyService.Domain.ValueObjects.MembershipFunction import MembershipFunction
from FuzzyService.Domain.Enums import (
    LogicalOperator as DomainLogicalOperator,
    RuleConnector,
    MembershipFunctionType
)

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


class InfrastructureToDomainMapper:
    """Mapper centralizado para convertir entidades de Infrastructure a Domain.
    
    Complementa el DomainToInfrastructureMapper para eliminar duplicación
    y proporcionar conversiones bidireccionales limpias.
    """
    
    @staticmethod
    def map_fuzzy_rule(infra_rule: InfraFuzzyRule, 
                      system_id: Optional[FuzzySystemId] = None) -> DomainFuzzyRule:
        """Convierte una InfraFuzzyRule a FuzzyRule del dominio."""
        # Convertir condiciones a formato de dominio
        conditions = []
        for condition in infra_rule.conditions:
            condition_dict = {
                "sensor_id": condition.sensor_id,
                "variable_name": condition.variable_name,
                "term_name": condition.term_name
            }
            conditions.append(condition_dict)
        
        # Extraer consecuente (simplificado)
        consequent = None
        if infra_rule.consequents:
            first_consequent = infra_rule.consequents[0]
            if hasattr(first_consequent, 'routine_id') and first_consequent.routine_id:
                consequent = FuzzyRoutineId(first_consequent.routine_id)
        
        return DomainFuzzyRule(
            id=FuzzyRuleId(infra_rule.rule_id) if infra_rule.rule_id else None,
            name=infra_rule.description or "Unnamed Rule",
            system_id=system_id,
            description=infra_rule.description,
            conditions=conditions,
            connectors=[],  # Se necesitaría lógica adicional para mapear conectores
            consequent=consequent,
            created_at=datetime.now(timezone.utc)
        )
    
    @staticmethod
    def map_fuzzy_variable(infra_variable: InfraFuzzyVariable) -> DomainFuzzyVariable:
        """Convierte una InfraFuzzyVariable a FuzzyVariable del dominio."""
        # Convertir términos
        term_ids = []
        for term_name, infra_term in infra_variable.terms.items():
            if hasattr(infra_term, 'term_id') and infra_term.term_id:
                term_ids.append(FuzzyTermId(infra_term.term_id))
        
        return DomainFuzzyVariable(
            id=FuzzyVariableId(infra_variable.variable_id) if infra_variable.variable_id else None,
            name=infra_variable.name,
            description=f"Variable with range {infra_variable.universe_range}",
            variable_type="input",  # Default, se necesitaría más contexto
            device_id=infra_variable.sensor_mapping,
            terms=term_ids,
            created_at=datetime.now(timezone.utc),
            updated_at=datetime.now(timezone.utc)
        )
    
    @staticmethod
    def map_fuzzy_term(infra_term: InfraFuzzyTerm, 
                      variable_id: Optional[FuzzyVariableId] = None) -> DomainFuzzyTerm:
        """Convierte un InfraFuzzyTerm a FuzzyTerm del dominio."""
        # Mapear tipo de función de membresía
        mf_type = MembershipFunctionType.TRIANGULAR  # Default
        try:
            mf_type = MembershipFunctionType(infra_term.function_type.upper())
        except (ValueError, AttributeError):
            pass
        
        # Crear función de membresía
        membership_function = MembershipFunction(
            type=mf_type,
            params=infra_term.parameters or {}
        )
        
        return DomainFuzzyTerm(
            id=FuzzyTermId(infra_term.term_id) if infra_term.term_id else None,
            label=infra_term.name,
            variable_id=variable_id,
            mf=membership_function,
            created_at=datetime.now(timezone.utc),
            updated_at=datetime.now(timezone.utc)
        )
    
    @staticmethod
    def map_evaluation_response(response: FuzzyEvaluationResponse,
                               system_id: FuzzySystemId,
                               inputs: List[InputValue]) -> FuzzyEvaluation:
        """Convierte una FuzzyEvaluationResponse a FuzzyEvaluation del dominio."""
        # Convertir outputs
        outputs = []
        for routine_id, variables in response.crisp_outputs.items():
            for variable_name, steps in variables.items():
                for step_number, value in steps.items():
                    output = OutputValue(
                        actuator_id=f"actuator_{variable_name}",
                        value=value,
                        step_number=step_number
                    )
                    outputs.append(output)
        
        # Convertir activaciones de reglas
        rule_activations = []
        for rule_id, strength in response.rule_activations.items():
            activation = RuleActivation(
                rule_id=FuzzyRuleId(rule_id),
                firing_strength=strength
            )
            rule_activations.append(activation)
        
        return FuzzyEvaluation(
            id=FuzzyEvaluationId(response.request_id),
            system_id=system_id,
            inputs=inputs,
            outputs=outputs,
            rule_activations=rule_activations,
            evaluation_timestamp=response.timestamp,
            processing_time_ms=getattr(response, 'processing_time_ms', 0.0),
            created_at=datetime.now(timezone.utc)
        )
    
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