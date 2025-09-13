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

# NOTE: Decouple mapper from infrastructure types by defining a minimal protocol-like dict interface.
from typing import TypedDict

class _InfraFuzzyEvaluationResponse(TypedDict, total=False):
    request_id: str
    system_id: str
    timestamp: Any
    inputs: Any
    activated_rules: Any
    crisp_outputs: Any


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
    def to_domain(
        infra_response: Any,
        system_id: str,
        evaluation_id: str | None = None,
        *,
        inputs: Optional[List[InputValue]] = None,
        timestamp: Optional[datetime] = None,
    ) -> FuzzyEvaluation:
        """Convierte una respuesta de infraestructura (forma flexible) a FuzzyEvaluation del dominio.
        
        - inputs: lista opcional de InputValue (sensor_id/value) provenientes del request original
        - timestamp: momento de la evaluación si se dispone
        """
        # 1) Determinar timestamp
        ts: datetime = (
            timestamp
            or getattr(infra_response, "timestamp", None)
            or datetime.now(timezone.utc)
        )

        # 2) Determinar inputs (preferir los provistos por el servicio)
        input_values: List[InputValue] = []
        if inputs is not None:
            input_values = list(inputs)
        else:
            # Intentar reconstruir desde la respuesta si expone sensor_data/inputs en dict
            sensor_data = (
                getattr(infra_response, "sensor_data", None)
                or getattr(infra_response, "inputs", None)
                or getattr(infra_response, "input_data", None)
                or {}
            )
            if isinstance(sensor_data, dict):
                for sensor_id, value in sensor_data.items():
                    try:
                        val = float(value)
                    except Exception:
                        continue
                    input_values.append(InputValue(sensor_id=str(sensor_id), value=val))

        # 3) Activaciones de reglas: si la respuesta provee información compatible, mapearla; en caso contrario dejar vacío
        rule_activations: List[RuleActivation] = []
        ra_any = (
            getattr(infra_response, "activated_rules", None)
            or getattr(infra_response, "rule_activations", None)
            or None
        )
        if isinstance(ra_any, list):
            for ra in ra_any:
                if isinstance(ra, dict):
                    rid = ra.get("ruleId") or ra.get("rule_id")
                    firing = ra.get("firingStrength") or ra.get("activation_level") or 0.0
                    try:
                        firing_f = float(firing)
                    except Exception:
                        firing_f = 0.0
                    # Salidas por regla si existen (actuadores/power/duration)
                    outputs: List[OutputValue] = []
                    for ov in ra.get("output_values", []) if isinstance(ra.get("output_values"), list) else []:
                        if isinstance(ov, dict):
                            try:
                                outputs.append(OutputValue.from_dict(ov))
                            except Exception:
                                continue
                    rule_activations.append(
                        RuleActivation(
                            rule_id=FuzzyRuleId(rid) if rid else "",
                            firing_strength=firing_f,
                            output_values=outputs,
                        )
                    )

        # 4) Construir evaluación de dominio
        return FuzzyEvaluation(
            id=FuzzyEvaluationId(evaluation_id) if evaluation_id else None,
            system_id=FuzzySystemId(system_id),
            timestamp=ts,
            inputs=input_values,
            activated_rules=rule_activations,
        )
    
    @staticmethod
    def from_rule_evaluation_result(rule_result: Any, system_id: str, evaluation_id: str | None = None) -> FuzzyEvaluation:
        """Convierte un resultado de evaluación de reglas (forma flexible) a FuzzyEvaluation del dominio."""
        # Convertir valores de entrada (si están disponibles)
        input_values: List[InputValue] = []
        input_data = getattr(rule_result, "input_data", None)
        if isinstance(input_data, dict):
            for variable_name, value in input_data.items():
                input_values.append(
                    InputValue(
                        variable_name=variable_name,
                        value=float(value),
                        timestamp=datetime.now(timezone.utc)
                    )
                )
        
        # Convertir activaciones de reglas (dict[str, float])
        rule_activations: List[RuleActivation] = []
        ra_dict = getattr(rule_result, "rule_activations", {})
        if isinstance(ra_dict, dict):
            for rule_id, activation_level in ra_dict.items():
                rule_activations.append(
                    RuleActivation(
                        rule_id=FuzzyRuleId(rule_id),
                        activation_level=float(activation_level),  # type: ignore[arg-type]
                        contribution=float(activation_level)       # type: ignore[arg-type]
                    )
                )
        
        # Crear valores de salida basados en rutinas activadas (si existen)
        output_values: List[OutputValue] = []
        activated_routines = getattr(rule_result, "activated_routines", [])
        if isinstance(activated_routines, list):
            for _ in activated_routines:
                output_values.append(
                    OutputValue(
                        variable_name="activated_routine",  # type: ignore[arg-type]
                        value=1.0,                            # type: ignore[arg-type]
                        confidence=1.0                        # type: ignore[arg-type]
                    )
                )
        
        return FuzzyEvaluation(
            id=FuzzyEvaluationId(evaluation_id) if evaluation_id else FuzzyEvaluationId.generate(),
            system_id=FuzzySystemId(system_id),
            input_values=input_values,
            output_values=output_values,
            rule_activations=rule_activations,
            timestamp=datetime.now(timezone.utc),
            execution_time_ms=float(getattr(rule_result, "execution_time_ms", 0.0)),
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
        input_values: List[InputValue] = []
        for iv_data in data.get("input_values", []):
            input_values.append(
                InputValue(
                    variable_name=iv_data["variable_name"],
                    value=float(iv_data["value"]),
                    timestamp=datetime.fromisoformat(iv_data["timestamp"].replace('Z', '+00:00'))
                )
            )
        
        # Convertir valores de salida
        output_values: List[OutputValue] = []
        for ov_data in data.get("output_values", []):
            output_values.append(
                OutputValue(
                    variable_name=ov_data["variable_name"],  # type: ignore[arg-type]
                    value=float(ov_data["value"]),          # type: ignore[arg-type]
                    confidence=float(ov_data.get("confidence", 1.0))  # type: ignore[arg-type]
                )
            )
        
        # Convertir activaciones de reglas
        rule_activations: List[RuleActivation] = []
        for ra_data in data.get("rule_activations", []):
            rule_activations.append(
                RuleActivation(
                    rule_id=FuzzyRuleId(ra_data["rule_id"]),
                    activation_level=float(ra_data["activation_level"]),   # type: ignore[arg-type]
                    contribution=float(ra_data["contribution"])            # type: ignore[arg-type]
                )
            )
        
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