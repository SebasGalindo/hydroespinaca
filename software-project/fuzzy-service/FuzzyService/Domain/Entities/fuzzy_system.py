from typing import List, Optional
from datetime import datetime, timezone

from pydantic import Field, field_validator, model_validator

from ..ValueObjects import FuzzySystemId, FuzzyVariableId, FuzzyRuleId, OperatorsConfig
from ..Enums import FuzzySystemStatus, DefuzzificationMethod
from ..Common import DomainBaseModel


class FuzzySystem(DomainBaseModel):
    """
    Entidad de dominio que representa un sistema difuso completo.
    Contiene variables, reglas y configuración para la inferencia difusa.
    """

    # Identificación
    id: Optional[FuzzySystemId] = None
    name: str

    # Estado y configuración
    status: FuzzySystemStatus = FuzzySystemStatus.DRAFT
    defuzzification_method: DefuzzificationMethod = DefuzzificationMethod.CENTROID
    operators: OperatorsConfig = Field(default_factory=OperatorsConfig)

    # Relaciones
    input_variable_ids: List[FuzzyVariableId] = Field(default_factory=list)
    output_variable_ids: List[FuzzyVariableId] = Field(default_factory=list)
    rule_ids: List[FuzzyRuleId] = Field(default_factory=list)

    # Metadatos
    created_at: Optional[datetime] = None
    updated_at: Optional[datetime] = None
    created_by: Optional[str] = None

    # Validaciones
    @field_validator("name")
    @classmethod
    def _validate_name(cls, v: str) -> str:
        if not v or not v.strip():
            raise ValueError("El nombre del sistema no puede estar vacío")
        v = v.strip()
        if len(v) < 3:
            raise ValueError("El nombre del sistema debe tener al menos 3 caracteres")
        if len(v) > 100:
            raise ValueError("El nombre del sistema no puede exceder 100 caracteres")
        return v

    @model_validator(mode="after")
    def _validate_relationships_and_timestamps(self):
        # timestamps UTC
        if self.created_at is None:
            self.created_at = datetime.now(timezone.utc)
        # Validar duplicados en relaciones
        if len(self.input_variable_ids) != len(set(self.input_variable_ids)):
            raise ValueError("No puede haber variables de entrada duplicadas")
        if len(self.output_variable_ids) != len(set(self.output_variable_ids)):
            raise ValueError("No puede haber variables de salida duplicadas")
        if len(self.rule_ids) != len(set(self.rule_ids)):
            raise ValueError("No puede haber reglas duplicadas")
        # No solapamiento entre entrada y salida
        if set(self.input_variable_ids).intersection(self.output_variable_ids):
            raise ValueError("Una variable no puede ser tanto de entrada como de salida")
        return self

    # Métodos de negocio para variables
    def add_input_variable(self, variable_id: FuzzyVariableId):
        """Agrega una variable de entrada al sistema."""
        if variable_id in self.input_variable_ids:
            raise ValueError(f"La variable {variable_id} ya existe como entrada")
        if variable_id in self.output_variable_ids:
            raise ValueError(f"La variable {variable_id} ya existe como salida")
        self.input_variable_ids.append(variable_id)

    def add_output_variable(self, variable_id: FuzzyVariableId):
        """Agrega una variable de salida al sistema."""
        if variable_id in self.output_variable_ids:
            raise ValueError(f"La variable {variable_id} ya existe como salida")
        if variable_id in self.input_variable_ids:
            raise ValueError(f"La variable {variable_id} ya existe como entrada")
        self.output_variable_ids.append(variable_id)

    def remove_input_variable(self, variable_id: FuzzyVariableId):
        """Remueve una variable de entrada del sistema."""
        if variable_id not in self.input_variable_ids:
            raise ValueError(f"La variable {variable_id} no existe como entrada")
        self.input_variable_ids.remove(variable_id)

    def remove_output_variable(self, variable_id: FuzzyVariableId):
        """Remueve una variable de salida del sistema."""
        if variable_id not in self.output_variable_ids:
            raise ValueError(f"La variable {variable_id} no existe como salida")
        self.output_variable_ids.remove(variable_id)

    # Métodos de negocio para reglas
    def add_rule(self, rule_id: FuzzyRuleId):
        """Agrega una regla al sistema."""
        if rule_id in self.rule_ids:
            raise ValueError(f"La regla {rule_id} ya existe en el sistema")
        self.rule_ids.append(rule_id)

    def remove_rule(self, rule_id: FuzzyRuleId):
        """Remueve una regla del sistema."""
        if rule_id not in self.rule_ids:
            raise ValueError(f"La regla {rule_id} no existe en el sistema")
        self.rule_ids.remove(rule_id)

    # Métodos de estado
    def activate(self):
        """Activa el sistema difuso."""
        if self.status == FuzzySystemStatus.ACTIVE:
            return  # Ya está activo
        self._validate_for_activation()
        self.status = FuzzySystemStatus.ACTIVE

    def deactivate(self):
        """Desactiva el sistema difuso."""
        if self.status != FuzzySystemStatus.ACTIVE:
            return  # No está activo
        self.status = FuzzySystemStatus.INACTIVE

    def set_testing_mode(self):
        """Pone el sistema en modo de pruebas."""
        self._validate_for_activation()
        self.status = FuzzySystemStatus.TESTING

    def _validate_for_activation(self):
        """Valida que el sistema pueda ser activado."""
        if not self.input_variable_ids:
            raise ValueError("El sistema debe tener al menos una variable de entrada")
        if not self.output_variable_ids:
            raise ValueError("El sistema debe tener al menos una variable de salida")
        if not self.rule_ids:
            raise ValueError("El sistema debe tener al menos una regla")

    # Métodos de consulta
    def is_operational(self) -> bool:
        """Verifica si el sistema está en un estado operacional."""
        return self.status in FuzzySystemStatus.get_operational_statuses()

    def is_editable(self) -> bool:
        """Verifica si el sistema puede ser editado."""
        return self.status in FuzzySystemStatus.get_editable_statuses()

    def get_total_variables(self) -> int:
        """Retorna el número total de variables."""
        return len(self.input_variable_ids) + len(self.output_variable_ids)

    def get_total_rules(self) -> int:
        """Retorna el número total de reglas."""
        return len(self.rule_ids)

    def has_variable(self, variable_id: FuzzyVariableId) -> bool:
        """Verifica si el sistema contiene una variable específica."""
        return variable_id in self.input_variable_ids or variable_id in self.output_variable_ids

    def has_rule(self, rule_id: FuzzyRuleId) -> bool:
        """Verifica si el sistema contiene una regla específica."""
        return rule_id in self.rule_ids

    # Métodos de configuración
    def update_configuration(self,
                             defuzzification_method: Optional[DefuzzificationMethod] = None,
                             operators: Optional[OperatorsConfig] = None):
        """Actualiza la configuración del sistema."""
        if not self.is_editable():
            raise ValueError(f"No se puede modificar un sistema en estado {self.status.value}")
        if defuzzification_method:
            self.defuzzification_method = defuzzification_method
        if operators:
            self.operators = operators

    def to_dict(self) -> dict:
        return {
            "id": str(self.id) if self.id is not None else None,
            "name": self.name,
            "status": self.status.value,
            "defuzzification_method": self.defuzzification_method.value,
            "operators": self.operators.to_dict(),
            "input_variable_ids": [str(v) for v in self.input_variable_ids],
            "output_variable_ids": [str(v) for v in self.output_variable_ids],
            "rule_ids": [str(r) for r in self.rule_ids],
            "created_at": self.created_at.isoformat() if self.created_at else None,
            "updated_at": self.updated_at.isoformat() if self.updated_at else None,
            "created_by": self.created_by,
        }

    def __str__(self) -> str:
        return f"FuzzySystem({self.name}, {self.status.value}, {self.get_total_variables()} vars, {self.get_total_rules()} rules)"
