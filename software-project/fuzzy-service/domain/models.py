"""Modelos de dominio para el fuzzy-service.

Esta capa contiene las entidades de negocio puras sin dependencias externas.
Representa los conceptos centrales del sistema de control difuso.
"""

from __future__ import annotations

from datetime import datetime, timezone
from enum import Enum
from typing import Any, Dict, List, Optional, Tuple

from pydantic import BaseModel, Field


class MembershipFunctionType(str, Enum):
    """Tipos de funciones de membresía disponibles."""
    TRIANGULAR = "triangular"
    TRAPEZOIDAL = "trapezoidal"
    GAUSSIAN = "gaussian"
    SIGMOID = "sigmoid"


class FuzzySet(BaseModel):
    """Conjunto difuso con función de membresía.
    
    Define un conjunto difuso con su nombre, tipo de función de membresía
    y parámetros específicos para la función.
    """
    name: str
    membership_type: MembershipFunctionType
    parameters: List[float]  # Parámetros específicos del tipo de función
    description: Optional[str] = None

    def validate_parameters(self) -> bool:
        """Valida que los parámetros sean correctos para el tipo de función."""
        if self.membership_type == MembershipFunctionType.TRIANGULAR:
            return len(self.parameters) == 3  # [a, b, c]
        elif self.membership_type == MembershipFunctionType.TRAPEZOIDAL:
            return len(self.parameters) == 4  # [a, b, c, d]
        elif self.membership_type == MembershipFunctionType.GAUSSIAN:
            return len(self.parameters) == 2  # [mean, sigma]
        elif self.membership_type == MembershipFunctionType.SIGMOID:
            return len(self.parameters) == 2  # [a, c]
        return False


class Variable(BaseModel):
    """Variable del sistema con definiciones de conjuntos difusos.
    
    Representa una magnitud física que puede ser medida por sensores
    y utilizada como entrada para el sistema de control difuso.
    """
    id: str
    name: str
    unit: Optional[str] = None
    description: Optional[str] = None
    # Rango de valores válidos para la variable
    min_value: Optional[float] = None
    max_value: Optional[float] = None
    # Conjuntos difusos definidos para esta variable
    fuzzy_sets: List[FuzzySet] = []
    
    def add_fuzzy_set(self, fuzzy_set: FuzzySet) -> None:
        """Añade un conjunto difuso a la variable."""
        if fuzzy_set.validate_parameters():
            self.fuzzy_sets.append(fuzzy_set)
        else:
            raise ValueError(f"Parámetros inválidos para función {fuzzy_set.membership_type}")
    
    def get_fuzzy_set(self, name: str) -> Optional[FuzzySet]:
        """Obtiene un conjunto difuso por nombre."""
        return next((fs for fs in self.fuzzy_sets if fs.name == name), None)


class FuzzyCondition(BaseModel):
    """Condición difusa para una regla.
    
    Define una condición del tipo "variable X es conjunto_difuso Y".
    """
    variable_id: str
    fuzzy_set_name: str
    weight: float = Field(default=1.0, ge=0.0, le=1.0)  # Peso de la condición


class LogicalOperator(str, Enum):
    """Operadores lógicos para combinar condiciones."""
    AND = "and"
    OR = "or"


class FuzzyRule(BaseModel):
    """Regla difusa completa.
    
    Define una regla del tipo "SI condición1 Y/O condición2 ENTONCES acción".
    """
    id: str
    name: str
    description: Optional[str] = None
    # Antecedente (condiciones)
    conditions: List[FuzzyCondition]
    operator: LogicalOperator = LogicalOperator.AND
    # Consecuente (acción)
    output_variable_id: str
    output_fuzzy_set_name: str
    # Metadatos
    priority: int = Field(default=1, ge=1, le=10)  # Prioridad de la regla
    active: bool = True
    
    def evaluate_conditions_text(self) -> str:
        """Genera descripción textual de las condiciones."""
        if not self.conditions:
            return "Sin condiciones"
        
        condition_texts = []
        for cond in self.conditions:
            text = f"{cond.variable_id} es {cond.fuzzy_set_name}"
            if cond.weight != 1.0:
                text += f" (peso: {cond.weight})"
            condition_texts.append(text)
        
        operator_text = " Y " if self.operator == LogicalOperator.AND else " O "
        return operator_text.join(condition_texts)


class ThresholdRule(BaseModel):
    """Regla simplificada de umbral (compatibilidad hacia atrás).
    
    Define una condición simple: si variable > umbral entonces activar salida.
    Mantenida para compatibilidad con implementaciones existentes.
    """
    variableId: str
    greater_than: float
    output_name: str  # nombre lógico de la salida (ej: "fanPower")
    target: int = Field(ge=0, le=100)  # porcentaje PWM objetivo
    hold_seconds: int = Field(ge=1)  # duración total del comando


class ActuatorType(str, Enum):
    """Tipos de actuadores soportados."""
    ON_OFF = "on_off"      # Actuadores binarios (bomba, válvula)
    VARIABLE = "variable"  # Actuadores regulables (LED, ventilador)


class ActuatorMapping(BaseModel):
    """Mapeo de salida lógica a actuador físico.
    
    Conecta los nombres lógicos de salida (ej: "fanPower") con
    los identificadores físicos de actuadores en el sistema.
    """
    output_name: str  # nombre lógico (debe coincidir con ThresholdRule.output_name)
    actuatorId: str   # ID del actuador en el actuator-service
    esp32Id: str      # ID del ESP32 que controla este actuador
    actuator_type: ActuatorType = ActuatorType.VARIABLE  # tipo de actuador
    on_threshold: float = Field(default=50.0, ge=0.0, le=100.0)  # umbral para activar actuadores on/off
    created_at: Optional[datetime] = None
    updated_at: Optional[datetime] = None


class ControlParameters(BaseModel):
    """Parámetros de control para el sistema difuso.
    
    Define parámetros globales que afectan el comportamiento
    del sistema de control difuso.
    """
    # Parámetros de histeresis
    hysteresis_delta: float = Field(default=5.0, ge=0.0)  # Delta mínimo para cambios
    cooldown_seconds: int = Field(default=30, ge=1)  # Tiempo mínimo entre comandos
    
    # Parámetros de defuzzificación
    defuzzification_method: str = Field(default="centroid")  # centroid, bisector, mom, som, lom
    
    # Parámetros de evaluación
    evaluation_interval_seconds: int = Field(default=10, ge=1)  # Frecuencia de evaluación
    min_confidence_threshold: float = Field(default=0.1, ge=0.0, le=1.0)  # Confianza mínima


class Routine(BaseModel):
    """Rutina de control que agrupa reglas difusas y mapeos.
    
    Una rutina define un conjunto completo de reglas de control difuso
    y los mapeos necesarios para ejecutarlas en actuadores físicos.
    """
    id: str
    name: str
    description: Optional[str] = None
    active: bool = True
    
    # Reglas de control (ambos tipos para compatibilidad)
    fuzzy_rules: List[FuzzyRule] = []  # Reglas difusas completas
    threshold_rules: List[ThresholdRule] = []  # Reglas simples (compatibilidad)
    
    # Mapeos y configuración
    outputs: List[ActuatorMapping] = []
    control_parameters: ControlParameters = Field(default_factory=ControlParameters)
    
    # Variables involucradas en esta rutina
    input_variables: List[str] = []  # IDs de variables de entrada
    output_variables: List[str] = []  # IDs de variables de salida

    def get_actuator_for_output(self, output_name: str) -> Optional[ActuatorMapping]:
        """Busca el mapeo de actuador para una salida lógica."""
        return next((m for m in self.outputs if m.output_name == output_name), None)
    
    def get_active_fuzzy_rules(self) -> List[FuzzyRule]:
        """Obtiene solo las reglas difusas activas."""
        return [rule for rule in self.fuzzy_rules if rule.active]
    
    def add_fuzzy_rule(self, rule: FuzzyRule) -> None:
        """Añade una regla difusa a la rutina."""
        # Verificar que no existe una regla con el mismo ID
        if any(r.id == rule.id for r in self.fuzzy_rules):
            raise ValueError(f"Ya existe una regla con ID {rule.id}")
        self.fuzzy_rules.append(rule)
    
    def remove_fuzzy_rule(self, rule_id: str) -> bool:
        """Elimina una regla difusa por ID. Retorna True si se eliminó."""
        initial_count = len(self.fuzzy_rules)
        self.fuzzy_rules = [r for r in self.fuzzy_rules if r.id != rule_id]
        return len(self.fuzzy_rules) < initial_count


class OutputPlan(BaseModel):
    """Plan de acción para un actuador específico.
    
    Resultado de la evaluación de reglas que indica qué acción
    debe ejecutarse en un actuador determinado.
    """
    actuator: ActuatorMapping
    target: int  # porcentaje PWM objetivo (0-100)
    hold_seconds: int  # duración total del comando
    fuzzy_rule: Optional[str] = None  # descripción de la regla que generó este plan


class ActuatorState(BaseModel):
    """Estado actual de un actuador para histeresis y cooldown.
    
    Mantiene información sobre la última acción emitida para
    evitar oscilaciones y comandos redundantes.
    """
    actuatorId: str
    last_target: int
    last_emitted_at: datetime
    last_hold_seconds: int

    def should_skip_hysteresis(self, new_target: int, hysteresis_delta: float) -> bool:
        """Determina si debe saltarse por histeresis."""
        delta = abs(new_target - self.last_target)
        return delta < hysteresis_delta

    def should_skip_cooldown(self, cooldown_seconds: int) -> bool:
        """Determina si debe saltarse por cooldown."""
        elapsed = (datetime.now(timezone.utc) - self.last_emitted_at).total_seconds()
        return elapsed < cooldown_seconds
    
    def update_state(self, new_target: int) -> None:
        """Actualiza el estado del actuador con un nuevo target."""
        self.last_target = new_target
        self.last_emitted_at = datetime.now(timezone.utc)