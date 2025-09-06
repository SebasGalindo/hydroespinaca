from typing import List, Optional, Dict, Any
from datetime import datetime, timezone

from pydantic import Field, field_validator, model_validator

from ..ValueObjects import FuzzyRuleId, FuzzySystemId, FuzzyVariableId, FuzzyRoutineId
from ..Enums import RuleConnector, LogicalOperator
from ..Common import DomainBaseModel
from ..Utils import extract_oid


class FuzzyRule(DomainBaseModel):
    """
    Entidad de dominio que representa una regla difusa.
    Estructura basada en el JSON especificado en el plan de implementación.
    """

    # Identificación
    id: Optional[FuzzyRuleId] = None
    name: str = ""
    system_id: Optional[FuzzySystemId] = None
    description: Optional[str] = None

    # Configuración de la regla
    # conditions: lista de condiciones con estructura { "variableId": FuzzyVariableId, "operator": LogicalOperator (IS/IS_NOT), "value": str }
    conditions: List[Dict[str, Any]] = Field(default_factory=list)
    # connectors: lista de conectores (N condiciones => N-1 conectores) entre condiciones consecutivas
    connectors: List[RuleConnector] = Field(default_factory=list)
    consequent: Optional[FuzzyRoutineId] = None  # ID de la rutina consecuente

    # Metadatos
    created_at: Optional[datetime] = None

    # -------------------------
    # Validaciones de campo
    # -------------------------
    @field_validator("name")
    @classmethod
    def _validate_name(cls, v: str) -> str:
        if not v or not v.strip():
            raise ValueError("El nombre de la regla no puede estar vacío")
        if len(v.strip()) > 100:
            raise ValueError("El nombre de la regla no puede exceder 100 caracteres")
        return v.strip()

    @field_validator("id", "system_id", "consequent", mode="before")
    @classmethod
    def _coerce_ids(cls, v):
        if isinstance(v, dict):
            return extract_oid(v)
        return v

    @field_validator("connectors", mode="before")
    @classmethod
    def _coerce_connectors(cls, v):
        if v is None:
            return []
        if not isinstance(v, list):
            raise ValueError("connectors debe ser lista")
        result: List[RuleConnector] = []
        for i, c in enumerate(v):
            if isinstance(c, RuleConnector):
                result.append(c)
            else:
                try:
                    result.append(RuleConnector(str(c)))
                except Exception:
                    raise ValueError(f"Conector en posición {i} inválido; debe ser AND u OR")
        return result

    @field_validator("conditions", mode="before")
    @classmethod
    def _normalize_conditions(cls, v):
        if v is None:
            return []
        if not isinstance(v, list):
            raise ValueError("conditions debe ser una lista")
        normalized: List[Dict[str, Any]] = []
        for i, c in enumerate(v):
            if not isinstance(c, dict):
                raise ValueError("Cada condición debe ser un diccionario")
            var = c.get("variableId")
            op = c.get("operator")
            val = c.get("value")
            # Coerción de variableId
            if isinstance(var, dict):
                var = extract_oid(var)
            var_id = var if isinstance(var, FuzzyVariableId) else (FuzzyVariableId(var) if var is not None else None)
            if var_id is None:
                raise ValueError(f"Condición #{i}: variableId es requerido")
            # Coerción de operador
            op_enum = op if isinstance(op, LogicalOperator) else LogicalOperator(str(op))
            if op_enum not in [LogicalOperator.IS, LogicalOperator.IS_NOT]:
                raise ValueError(f"Condición #{i}: operador inválido; sólo IS o IS_NOT")
            # Validación de valor
            if not isinstance(val, str) or not val.strip():
                raise ValueError(f"Condición #{i}: la etiqueta 'value' no puede estar vacía")
            normalized.append({
                "variableId": var_id,
                "operator": op_enum,
                "value": val.strip(),
            })
        return normalized

    # -------------------------
    # Validación cruzada y normalización final
    # -------------------------
    @model_validator(mode="after")
    def _validate_and_finalize(self):
        if self.created_at is None:
            self.created_at = datetime.now(timezone.utc)
        # Cardinalidad de conectores
        expected = max(0, len(self.conditions) - 1)
        if len(self.connectors) != expected:
            raise ValueError(
                f"Número de conectores inválido: se esperaban {expected} y se recibieron {len(self.connectors)}"
            )
        # Unicidad de variables en condiciones
        vars_seen = [c.get("variableId") for c in self.conditions]
        if len(vars_seen) != len(set(vars_seen)):
            raise ValueError("Ya existe una condición para alguna variable (variableId duplicado)")
        # Validación del consecuente
        if self.consequent is None:
            raise ValueError("El consecuente (ID de rutina) no puede estar vacío")
        if isinstance(self.consequent, str):
            self.consequent = FuzzyRoutineId(self.consequent)
        return self

    # -------------------------
    # Métodos de negocio
    # -------------------------
    def add_condition(self, variable_id: FuzzyVariableId | str, operator: LogicalOperator | str, value: str, connector: RuleConnector | str | None = None):
        """Agrega una condición a la regla. Si ya existe al menos una condición, se debe suministrar el conector que une la última condición existente con la nueva."""
        var_id = variable_id if isinstance(variable_id, FuzzyVariableId) else FuzzyVariableId(variable_id)
        op_enum = operator if isinstance(operator, LogicalOperator) else LogicalOperator(operator)
        if op_enum not in [LogicalOperator.IS, LogicalOperator.IS_NOT]:
            raise ValueError("Solo se permiten operadores IS o IS_NOT para etiquetas lingüísticas")
        if not isinstance(value, str) or not value.strip():
            raise ValueError("La etiqueta lingüística 'value' no puede estar vacía")
        # Verificar duplicado por variable
        for existing_condition in self.conditions:
            if existing_condition.get("variableId") == var_id:
                raise ValueError(f"Ya existe una condición para la variable '{var_id}'")
        # Manejo de conectores
        if len(self.conditions) == 0:
            if self.connectors:
                raise ValueError("No debe haber conectores cuando no hay condiciones previas")
            # primera condición; no se requiere conector
        else:
            if connector is None:
                raise ValueError("Debe especificarse un conector para unir la nueva condición con la anterior")
            conn_enum = connector if isinstance(connector, RuleConnector) else RuleConnector(connector)
            self.connectors.append(conn_enum)
        # Agregar condición
        self.conditions.append({
            "variableId": var_id,
            "operator": op_enum,
            "value": value.strip()
        })

    def remove_condition(self, variable_id: FuzzyVariableId | str):
        """Remueve una condición de la regla por variable y ajusta los conectores en consecuencia."""
        if len(self.conditions) <= 1:
            raise ValueError("Una regla debe tener al menos una condición")
        var_id = variable_id if isinstance(variable_id, FuzzyVariableId) else FuzzyVariableId(variable_id)
        # localizar índice
        idx = None
        for i, c in enumerate(self.conditions):
            if c.get("variableId") == var_id:
                idx = i
                break
        if idx is None:
            raise ValueError(f"No se encontró condición para la variable '{var_id}'")
        # ajustar conectores: eliminar el conector adyacente que corresponde
        if self.connectors:
            if idx == 0:
                # Se elimina el primer conector (entre 0 y 1)
                self.connectors.pop(0)
            else:
                # Se elimina el conector que unía idx-1 e idx
                self.connectors.pop(idx - 1)
        # eliminar condición
        self.conditions.pop(idx)
        # validar consistencia
        expected = max(0, len(self.conditions) - 1)
        if len(self.connectors) != expected:
            raise ValueError(
                f"Número de conectores inválido: se esperaban {expected} y se recibieron {len(self.connectors)}"
            )

    def update_condition(self, variable_id: FuzzyVariableId | str, operator: LogicalOperator | str, value: str):
        """Actualiza una condición específica (no modifica conectores)."""
        var_id = variable_id if isinstance(variable_id, FuzzyVariableId) else FuzzyVariableId(variable_id)
        op_enum = operator if isinstance(operator, LogicalOperator) else LogicalOperator(operator)
        if op_enum not in [LogicalOperator.IS, LogicalOperator.IS_NOT]:
            raise ValueError("Solo se permiten operadores IS o IS_NOT para etiquetas lingüísticas")
        if not isinstance(value, str) or not value.strip():
            raise ValueError("La etiqueta lingüística 'value' no puede estar vacía")
        for condition in self.conditions:
            if condition.get("variableId") == var_id:
                condition["operator"] = op_enum
                condition["value"] = value.strip()
                return
        raise ValueError(f"No se encontró condición para la variable '{var_id}'")

    def set_connector_at(self, index: int, connector: RuleConnector | str):
        """Actualiza un conector en la posición indicada."""
        if index < 0 or index >= len(self.connectors):
            raise IndexError("Índice de conector fuera de rango")
        conn_enum = connector if isinstance(connector, RuleConnector) else RuleConnector(connector)
        self.connectors[index] = conn_enum
        # validar consistencia
        expected = max(0, len(self.conditions) - 1)
        if len(self.connectors) != expected:
            raise ValueError(
                f"Número de conectores inválido: se esperaban {expected} y se recibieron {len(self.connectors)}"
            )

    def set_connectors(self, connectors: List[RuleConnector | str]):
        """Reemplaza la lista completa de conectores. Debe cumplir la cardinalidad N-1 respecto a condiciones."""
        # normalizar
        new_connectors: List[RuleConnector] = []
        for i, c in enumerate(connectors):
            if isinstance(c, RuleConnector):
                new_connectors.append(c)
            else:
                try:
                    new_connectors.append(RuleConnector(c))
                except Exception:
                    raise ValueError(f"Conector en posición {i} inválido; debe ser AND u OR")
        self.connectors = new_connectors
        # validar consistencia
        expected = max(0, len(self.conditions) - 1)
        if len(self.connectors) != expected:
            raise ValueError(
                f"Número de conectores inválido: se esperaban {expected} y se recibieron {len(self.connectors)}"
            )

    def update_consequent(self, routine_id: FuzzyRoutineId | str):
        """Actualiza el consecuente (ID de rutina)."""
        self.consequent = routine_id if isinstance(routine_id, FuzzyRoutineId) else FuzzyRoutineId(routine_id)

    def update_description(self, description: Optional[str]):
        """Actualiza la descripción de la regla."""
        if description and len(description) > 500:
            raise ValueError("La descripción no puede exceder 500 caracteres")
        self.description = description

    # Métodos de consulta
    def get_condition_count(self) -> int:
        """Retorna el número de condiciones."""
        return len(self.conditions)

    def has_condition_for_variable(self, variable_id: FuzzyVariableId | str) -> bool:
        """Verifica si la regla tiene una condición para una variable específica."""
        var_id = variable_id if isinstance(variable_id, FuzzyVariableId) else FuzzyVariableId(variable_id)
        return any(c.get("variableId") == var_id for c in self.conditions)

    def get_condition_by_variable(self, variable_id: FuzzyVariableId | str) -> Optional[Dict[str, Any]]:
        """Obtiene la condición para una variable específica."""
        var_id = variable_id if isinstance(variable_id, FuzzyVariableId) else FuzzyVariableId(variable_id)
        for condition in self.conditions:
            if condition.get("variableId") == var_id:
                return condition
        return None

    def get_variables_used(self) -> List[FuzzyVariableId]:
        """Retorna la lista de variables utilizadas en las condiciones."""
        return [c.get("variableId") for c in self.conditions]

    def get_rule_text(self) -> str:
        """Genera una representación textual de la regla intercalando conectores."""
        if not self.conditions:
            return f"IF <no conditions> THEN {self.consequent}"
        # construir piezas: cond0, conn0, cond1, conn1, ...
        parts: List[str] = []
        for i, condition in enumerate(self.conditions):
            variable = str(condition.get("variableId", ""))
            operator = condition.get("operator").value if isinstance(condition.get("operator"), LogicalOperator) else str(condition.get("operator", ""))
            value = condition.get("value", "")
            parts.append(f"{variable} {operator} {value}")
            if i < len(self.connectors):
                # Handle both RuleConnector objects and strings
                connector = self.connectors[i]
                connector_value = connector.value if hasattr(connector, 'value') else str(connector)
                parts.append(connector_value)
        return f"IF {' '.join(parts)} THEN {self.consequent}"

    # Métodos utilitarios
    def to_dict(self) -> Dict[str, Any]:
        """Convierte la entidad a diccionario para serialización (formato MongoDB)."""
        return {
            "_id": str(self.id) if self.id else None,
            "name": self.name,
            "systemId": str(self.system_id) if self.system_id else None,
            "description": self.description,
            "conditions": [
                {
                    "variableId": str(c["variableId"]) if isinstance(c.get("variableId"), FuzzyVariableId) else str(c.get("variableId")),
                    "operator": c["operator"].value if isinstance(c.get("operator"), LogicalOperator) else str(c.get("operator")),
                    "value": c.get("value")
                }
                for c in self.conditions
            ],
            "connectors": [c.value for c in self.connectors],
            "consequent": str(self.consequent) if self.consequent else None,
            "createdAt": self.created_at.isoformat() if self.created_at else None
        }

    def __str__(self) -> str:
        condition_count = len(self.conditions)
        return f"FuzzyRule({self.name}, {condition_count} conditions)"

    def __repr__(self) -> str:
        return (f"FuzzyRule(id={self.id}, name='{self.name}', "
                f"conditions={len(self.conditions)}, connectors={len(self.connectors)})")
