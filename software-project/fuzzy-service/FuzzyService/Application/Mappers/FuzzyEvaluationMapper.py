from typing import List, Dict, Any, Optional
from datetime import datetime, timezone

from FuzzyService.Domain.Entities.fuzzy_evaluation import (
    FuzzyEvaluation,
    InputValue,
    OutputValue,
    RuleActivation
)
from FuzzyService.Domain.ValueObjects.DomainId import (
    FuzzyEvaluationId,
    FuzzySystemId,
    FuzzyRuleId
)

from FuzzyService.Infrastructure.FuzzyEngine.ScikitFuzzyEngine import (
    FuzzyEvaluationResponse
)
from FuzzyService.Infrastructure.FuzzyEngine.RuleEvaluationEngine import (
    RuleEvaluationResult
)


class FuzzyEvaluationMapper:
    """Mapper específico para conversiones entre FuzzyEvaluation de dominio e infraestructura."""
    
    @staticmethod
    def to_infra_request(domain_evaluation: FuzzyEvaluation) -> Dict[str, Any]:
        """Convierte una FuzzyEvaluation del dominio a formato de request de infraestructura."""
        # Convertir valores de entrada a formato de infraestructura
        input_data = {}
        for input_value in domain_evaluation.input_values:
            input_data[input_value.variable_name] = input_value.value
        
        return {
            "system_id": str(domain_evaluation.system_id),
            "input_data": input_data,
            "evaluation_id": str(domain_evaluation.id),
            "timestamp": domain_evaluation.timestamp.isoformat()
        }
    
    @staticmethod
    def to_domain(infra_response: FuzzyEvaluationResponse, system_id: str, evaluation_id: str = None) -> FuzzyEvaluation:
        """Convierte una FuzzyEvaluationResponse de infraestructura a FuzzyEvaluation del dominio."""
        # Convertir valores de entrada
        input_values = []
        for variable_name, value in infra_response.input_data.items():
            input_value = InputValue(
                variable_name=variable_name,
                value=float(value),
                timestamp=infra_response.timestamp
            )
            input_values.append(input_value)
        
        # Convertir valores de salida
        output_values = []
        for variable_name, value in infra_response.output_data.items():
            output_value = OutputValue(
                variable_name=variable_name,
                value=float(value),
                confidence=getattr(infra_response, 'confidence', 1.0)
            )
            output_values.append(output_value)
        
        # Convertir activaciones de reglas
        rule_activations = []
        if hasattr(infra_response, 'rule_activations'):
            for rule_activation_data in infra_response.rule_activations:
                rule_activation = RuleActivation(
                    rule_id=FuzzyRuleId(rule_activation_data.get('rule_id', '')),
                    activation_level=float(rule_activation_data.get('activation_level', 0.0)),
                    contribution=float(rule_activation_data.get('contribution', 0.0))
                )
                rule_activations.append(rule_activation)
        
        return FuzzyEvaluation(
            id=FuzzyEvaluationId(evaluation_id) if evaluation_id else FuzzyEvaluationId.generate(),
            system_id=FuzzySystemId(system_id),
            input_values=input_values,
            output_values=output_values,
            rule_activations=rule_activations,
            timestamp=infra_response.timestamp,
            execution_time_ms=getattr(infra_response, 'execution_time_ms', 0.0),
            success=getattr(infra_response, 'success', True),
            error_message=getattr(infra_response, 'error_message', None)
        )
    
    @staticmethod
    def from_rule_evaluation_result(rule_result: RuleEvaluationResult, system_id: str, evaluation_id: str = None) -> FuzzyEvaluation:
        """Convierte un RuleEvaluationResult a FuzzyEvaluation del dominio."""
        # Convertir valores de entrada (si están disponibles)
        input_values = []
        if hasattr(rule_result, 'input_data'):
            for variable_name, value in rule_result.input_data.items():
                input_value = InputValue(
                    variable_name=variable_name,
                    value=float(value),
                    timestamp=datetime.now(timezone.utc)
                )
                input_values.append(input_value)
        
        # Convertir activaciones de reglas
        rule_activations = []
        for rule_id, activation_level in rule_result.rule_activations.items():
            rule_activation = RuleActivation(
                rule_id=FuzzyRuleId(rule_id),
                activation_level=float(activation_level),
                contribution=float(activation_level)  # Asumiendo que la contribución es igual a la activación
            )
            rule_activations.append(rule_activation)
        
        # Crear valores de salida basados en las activaciones
        output_values = []
        if rule_result.activated_routines:
            for routine_id in rule_result.activated_routines:
                output_value = OutputValue(
                    variable_name="activated_routine",
                    value=1.0,  # Rutina activada
                    confidence=1.0
                )
                output_values.append(output_value)
        
        return FuzzyEvaluation(
            id=FuzzyEvaluationId(evaluation_id) if evaluation_id else FuzzyEvaluationId.generate(),
            system_id=FuzzySystemId(system_id),
            input_values=input_values,
            output_values=output_values,
            rule_activations=rule_activations,
            timestamp=datetime.now(timezone.utc),
            execution_time_ms=getattr(rule_result, 'execution_time_ms', 0.0),
            success=True,
            error_message=None
        )
    
    @staticmethod
    def to_dict(domain_evaluation: FuzzyEvaluation) -> Dict[str, Any]:
        """Convierte una FuzzyEvaluation del dominio a diccionario para persistencia."""
        return {
            "id": str(domain_evaluation.id),
            "system_id": str(domain_evaluation.system_id),
            "input_values": [
                {
                    "variable_name": iv.variable_name,
                    "value": iv.value,
                    "timestamp": iv.timestamp.isoformat()
                }
                for iv in domain_evaluation.input_values
            ],
            "output_values": [
                {
                    "variable_name": ov.variable_name,
                    "value": ov.value,
                    "confidence": ov.confidence
                }
                for ov in domain_evaluation.output_values
            ],
            "rule_activations": [
                {
                    "rule_id": str(ra.rule_id),
                    "activation_level": ra.activation_level,
                    "contribution": ra.contribution
                }
                for ra in domain_evaluation.rule_activations
            ],
            "timestamp": domain_evaluation.timestamp.isoformat(),
            "execution_time_ms": domain_evaluation.execution_time_ms,
            "success": domain_evaluation.success,
            "error_message": domain_evaluation.error_message
        }
    
    @staticmethod
    def from_dict(data: Dict[str, Any]) -> FuzzyEvaluation:
        """Convierte un diccionario a FuzzyEvaluation del dominio."""
        # Convertir valores de entrada
        input_values = []
        for iv_data in data.get("input_values", []):
            input_value = InputValue(
                variable_name=iv_data["variable_name"],
                value=float(iv_data["value"]),
                timestamp=datetime.fromisoformat(iv_data["timestamp"].replace('Z', '+00:00'))
            )
            input_values.append(input_value)
        
        # Convertir valores de salida
        output_values = []
        for ov_data in data.get("output_values", []):
            output_value = OutputValue(
                variable_name=ov_data["variable_name"],
                value=float(ov_data["value"]),
                confidence=float(ov_data.get("confidence", 1.0))
            )
            output_values.append(output_value)
        
        # Convertir activaciones de reglas
        rule_activations = []
        for ra_data in data.get("rule_activations", []):
            rule_activation = RuleActivation(
                rule_id=FuzzyRuleId(ra_data["rule_id"]),
                activation_level=float(ra_data["activation_level"]),
                contribution=float(ra_data["contribution"])
            )
            rule_activations.append(rule_activation)
        
        return FuzzyEvaluation(
            id=FuzzyEvaluationId(data["id"]),
            system_id=FuzzySystemId(data["system_id"]),
            input_values=input_values,
            output_values=output_values,
            rule_activations=rule_activations,
            timestamp=datetime.fromisoformat(data["timestamp"].replace('Z', '+00:00')),
            execution_time_ms=float(data.get("execution_time_ms", 0.0)),
            success=bool(data.get("success", True)),
            error_message=data.get("error_message")
        )