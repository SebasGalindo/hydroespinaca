from typing import List, Optional, Dict, Any
from datetime import datetime, timezone

from pydantic import Field, field_validator, model_validator

from ..ValueObjects import FuzzyRuleId, FuzzySystemId, FuzzyVariableId
from ..Enums import RuleConnector, LogicalOperator
from ..Common import DomainBaseModel
from ..Utils import extract_oid
from .rule_consequent import RuleConsequent


class FuzzyRule(DomainBaseModel):
    """
    Entidad de dominio que representa una regla difusa con consecuentes directos Mamdani.

    Estructura: IF <conditions> THEN <consequents>

    Permite agregación multi-regla: múltiples reglas pueden contribuir a la misma
    variable de salida, y el motor fuzzy agregará sus funciones de membresía
    antes de defuzzificar.
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

    # Consecuentes directos Mamdani: lista de variables de salida con sus términos
    consequents: List[RuleConsequent] = Field(default_factory=list)

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

    @field_validator("id", "system_id", mode="before")
    @classmethod
    def _coerce_ids(cls, v):
        if isinstance(v, dict):
            return extract_oid(v)
        return v

    @field_validator("consequents", mode="before")
    @classmethod
    def _coerce_consequents(cls, v):
        if v is None:
            return []
        if not isinstance(v, list):
            raise ValueError("consequents debe ser una lista")
        result: List[RuleConsequent] = []
        for item in v:
            if isinstance(item, RuleConsequent):
                result.append(item)
            elif isinstance(item, dict):
                result.append(RuleConsequent.from_dict(item))
            else:
                raise ValueError("Cada consecuente debe ser RuleConsequent o dict")
        return result

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

        # Validación del consecuente: debe tener al menos un consecuente Mamdani
        if not self.consequents or len(self.consequents) == 0:
            raise ValueError(
                "La regla debe tener al menos un consecuente. "
                "Use rule.add_consequent() para agregar consecuentes."
            )

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

    def set_connectors(self, connectors: List[RuleConnector | str]):
        """Reemplaza todos los conectores, validando cardinalidad (N condiciones => N-1 conectores)."""
        coerced: List[RuleConnector] = []
        for i, c in enumerate(connectors or []):
            coerced.append(c if isinstance(c, RuleConnector) else RuleConnector(c))
        expected = max(0, len(self.conditions) - 1)
        if len(coerced) != expected:
            raise ValueError(f"Número de conectores inválido: se esperaban {expected} y se recibieron {len(coerced)}")
        self.connectors = coerced

    # -------------------------
    # Métodos de negocio para consecuentes
    # -------------------------
    def add_consequent(self, consequent: RuleConsequent):
        """Agrega un consecuente Mamdani a la regla."""
        # Validar que no exista ya un consecuente para la misma variable
        for existing in self.consequents:
            if existing.variable_id == consequent.variable_id:
                raise ValueError(
                    f"Ya existe un consecuente para la variable {consequent.variable_id}"
                )
        self.consequents.append(consequent)

    def remove_consequent(self, variable_id: FuzzyVariableId):
        """Remueve un consecuente por variable_id."""
        self.consequents = [
            c for c in self.consequents if c.variable_id != variable_id
        ]

    def get_consequent_for_variable(
        self, variable_id: FuzzyVariableId
    ) -> Optional[RuleConsequent]:
        """Obtiene el consecuente para una variable específica."""
        for c in self.consequents:
            if c.variable_id == variable_id:
                return c
        return None

    def has_consequents(self) -> bool:
        """Verifica si la regla tiene consecuentes definidos."""
        return len(self.consequents) > 0

    def get_consequent_count(self) -> int:
        """Retorna el número de consecuentes."""
        return len(self.consequents)

    def update_description(self, description: Optional[str]):
        self.description = description

    def get_condition_count(self) -> int:
        return len(self.conditions)

    def has_condition_for_variable(self, variable_id: FuzzyVariableId | str) -> bool:
        var_id = variable_id if isinstance(variable_id, FuzzyVariableId) else FuzzyVariableId(variable_id)
        return any(c.get("variableId") == var_id for c in self.conditions)

    def get_condition_by_variable(self, variable_id: FuzzyVariableId | str) -> Optional[Dict[str, Any]]:
        var_id = variable_id if isinstance(variable_id, FuzzyVariableId) else FuzzyVariableId(variable_id)
        for c in self.conditions:
            if c.get("variableId") == var_id:
                return c
        return None

    def get_variables_used(self) -> List[FuzzyVariableId]:
        return [c.get("variableId") for c in self.conditions]

    def get_rule_text(self) -> str:
        parts: List[str] = []
        for idx, c in enumerate(self.conditions):
            var = c.get("variableId")
            op = c.get("operator")
            val = c.get("value")
            parts.append(f"{var} {op.value if hasattr(op, 'value') else op} {val}")
            if idx < len(self.connectors):
                connector = self.connectors[idx]
                # Handle both enum and string connectors
                if hasattr(connector, 'value'):
                    parts.append(connector.value)
                else:
                    parts.append(str(connector))
        return " ".join(parts)

    def to_dict(self) -> Dict[str, Any]:
        return {
            "id": {"$oid": str(self.id)} if self.id else None,
            "name": self.name,
            "systemId": {"$oid": str(self.system_id)} if self.system_id else None,
            "description": self.description,
            "conditions": [
                {
                    "variableId": {"$oid": str(c.get("variableId"))} if isinstance(c.get("variableId"), FuzzyVariableId) else c.get("variableId"),
                    "operator": c.get("operator").value if hasattr(c.get("operator"), "value") else str(c.get("operator")),
                    "value": c.get("value"),
                }
                for c in self.conditions
            ],
            "connectors": [c.value if hasattr(c, "value") else str(c) for c in self.connectors],
            "consequents": [c.to_dict() for c in self.consequents],
            "createdAt": self.created_at.isoformat() if self.created_at else None,
        }

    def __str__(self) -> str:
        return f"FuzzyRule(id={self.id}, name={self.name})"

    def __repr__(self) -> str:
        return self.__str__()
