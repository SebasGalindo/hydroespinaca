from __future__ import annotations

import logging
from typing import Dict, List, Any, Optional
from datetime import datetime, timezone


class FuzzificationResult:
    """Resultado de la fuzzificación de una variable de entrada.
    
    Contiene el valor crisp de entrada, los términos activados con sus grados
    de pertenencia, y métrica de tiempo de procesamiento.
    """
    
    def __init__(self, variable_name: str, sensor_id: str, crisp_value: float, variable_id: Optional[str] = None):
        self.variable_name = variable_name
        self.sensor_id = sensor_id
        self.crisp_value = crisp_value
        self.variable_id = variable_id
        self.activated_terms: Dict[str, float] = {}  # term_label -> membership_degree
        self.processing_time_ms: float = 0.0
        
    def add_term_activation(self, term_label: str, membership_degree: float):
        """Añade la activación de un término con su grado de pertenencia."""
        self.activated_terms[term_label] = max(0.0, min(1.0, membership_degree))
        
    def get_dominant_term(self) -> tuple[Optional[str], float]:
        """Retorna el término con mayor grado de pertenencia y su valor."""
        if not self.activated_terms:
            return None, 0.0
        max_item = max(self.activated_terms.items(), key=lambda x: x[1])
        return max_item[0], max_item[1]


class RuleActivationResult:
    """Resultado de la activación de una regla individual."""
    
    def __init__(self, rule_id: str, rule_name: str, consequent: Optional[str] = None):
        self.rule_id = rule_id
        self.rule_name = rule_name
        self.firing_strength: float = 0.0
        self.condition_evaluations: List[Dict[str, Any]] = []
        self.is_activated: bool = False
        self.evaluation_time_ms: float = 0.0
        self.error_message: Optional[str] = None
        self.consequent: Optional[str] = consequent
        
    def add_condition_evaluation(self, variable_name: str, term_label: str, 
                               membership_degree: float, sensor_value: float):
        """Añade la evaluación de una condición individual."""
        self.condition_evaluations.append({
            "variable_name": variable_name,
            "term_label": term_label,
            "membership_degree": membership_degree,
            "sensor_value": sensor_value
        })
        
    def set_firing_strength(self, strength: float):
        """Establece la fuerza de activación de la regla."""
        self.firing_strength = max(0.0, min(1.0, strength))  # Clamp entre 0 y 1
        self.is_activated = self.firing_strength > 0.0


class BatchRuleEvaluationResult:
    """Resultado de la evaluación de un lote de reglas."""
    
    def __init__(self):
        self.rule_results: List[RuleActivationResult] = []
        self.total_processing_time_ms: float = 0.0
        self.rules_evaluated: int = 0
        self.rules_activated: int = 0
        self.evaluation_timestamp: datetime = datetime.now(timezone.utc)
        
    def add_rule_result(self, result: RuleActivationResult):
        """Añade el resultado de evaluación de una regla."""
        self.rule_results.append(result)
        self.rules_evaluated += 1
        if result.is_activated:
            self.rules_activated += 1
            
    def get_activated_rules(self) -> List[RuleActivationResult]:
        """Retorna solo las reglas que fueron activadas."""
        return [r for r in self.rule_results if r.is_activated]