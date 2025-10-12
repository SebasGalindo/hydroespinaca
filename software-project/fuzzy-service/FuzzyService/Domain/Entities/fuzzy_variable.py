from datetime import datetime, timezone
from typing import List, Optional, Dict, Any

from pydantic import Field, field_validator, model_validator

from ..ValueObjects import FuzzyVariableId, FuzzyTermId
from ..Common import DomainBaseModel


class FuzzyVariable(DomainBaseModel):
    """Entidad del dominio que representa una variable difusa.

    Estructura actualizada con reference_code (identificador estable):
    {
      "_id": { "$oid": "var_temp_air" },
      "name": "Temperatura del aire",
      "description": "Variable que representa la temperatura del aire en el invernadero",
      "variable_type": "input|output",
      "reference_code": "T_AMB",  # Código estable del sensor (sensor-service)
      "reference_id": { "$oid": "..." },  # [DEPRECATED] MongoDB ID volátil
      "terms": [ { "$oid": "term_temp_low" }, ... ],
      "createdAt": "2025-01-27T23:39:17.917+00:00",
      "updatedAt": "2025-01-27T23:39:17.917+00:00"
    }

    Relaciones entre servicios (usando códigos estables):
    - Si variable_type = "input": reference_code mapea a Code de sensor-service
      Ejemplo: "T_AMB", "HUM", "LUMINOSITY", etc.
    - Si variable_type = "output": reference_code mapea a Code de actuator-service ControlOutput
      Ejemplo: "OUTPUT_VENTILADOR_POTENCIA", "OUTPUT_LUZ_CONTROL", etc.

    Agrupación de variables de salida (actuator_code):
    - Para outputs, actuator_code agrupa las 2 variables que controlan el mismo actuador
    - Ejemplo: actuator_code="Ventiladores" agrupa:
      * "Potencia del Ventilador" (reference_code="OUTPUT_VENTILADOR_POTENCIA")
      * "Duración de Ventilación" (reference_code="OUTPUT_VENTILADOR_DURACION")
    - Esto permite emparejar Control+Duración sin depender de coincidencias de nombres

    IMPORTANTE: reference_id está deprecado. Usar reference_code para nuevas integraciones.
    """

    # Propiedades de identificación
    id: Optional[FuzzyVariableId] = None
    name: str = ""
    description: str = ""

    # Configuración de la variable
    variable_type: str = "input"  # "input" | "output"
    actuator_type: Optional[str] = None  # "PWM" | "DIGITAL" (solo para outputs)

    # Configuración de defuzzificación (solo para outputs)
    defuzzification_threshold: float = 50.0  # Threshold para actuadores DIGITAL (0-100)
    universe_min: Optional[float] = None  # Mínimo del universo de discurso
    universe_max: Optional[float] = None  # Máximo del universo de discurso

    # Relaciones
    reference_code: Optional[str] = None  # Código estable de variable/output (ej: "T_AMB", "OUTPUT_VENTILADOR_POTENCIA")

    # Agrupación de variables por actuador (solo para outputs)
    # Ejemplo: "Ventiladores" agrupa "Potencia del Ventilador" + "Duración de Ventilación"
    actuator_code: Optional[str] = None

    terms: List[FuzzyTermId] = Field(default_factory=list)

    # Metadatos básicos
    created_at: Optional[datetime] = None
    updated_at: Optional[datetime] = None

    # -------------------------
    # Validaciones Pydantic
    # -------------------------
    @field_validator("name")
    @classmethod
    def _validate_name(cls, v: str) -> str:
        if not v or not v.strip():
            raise ValueError("El nombre de la variable no puede estar vacío")
        if len(v.strip()) > 100:
            raise ValueError("El nombre de la variable no puede exceder 100 caracteres")
        return v.strip()

    @field_validator("description")
    @classmethod
    def _validate_description(cls, v: str) -> str:
        if v and len(v) > 500:
            raise ValueError("La descripción no puede exceder 500 caracteres")
        return v

    @field_validator("variable_type")
    @classmethod
    def _validate_type(cls, v: str) -> str:
        valid_types = ["input", "output"]
        if v not in valid_types:
            raise ValueError(f"Tipo de variable inválido: {v}. Debe ser uno de: {valid_types}")
        return v

    @field_validator("actuator_type")
    @classmethod
    def _validate_actuator_type(cls, v: Optional[str]) -> Optional[str]:
        if v is not None:
            valid_types = ["PWM", "DIGITAL"]
            if v not in valid_types:
                raise ValueError(f"actuator_type inválido: {v}. Debe ser uno de: {valid_types}")
        return v

    @field_validator("defuzzification_threshold")
    @classmethod
    def _validate_defuzzification_threshold(cls, v: float) -> float:
        if not 0 <= v <= 100:
            raise ValueError(f"defuzzification_threshold debe estar entre 0 y 100, recibido: {v}")
        return v

    @field_validator("universe_min")
    @classmethod
    def _validate_universe_min(cls, v: Optional[float]) -> Optional[float]:
        if v is not None and v < 0:
            raise ValueError(f"universe_min no puede ser negativo, recibido: {v}")
        return v

    @field_validator("universe_max")
    @classmethod
    def _validate_universe_max(cls, v: Optional[float]) -> Optional[float]:
        if v is not None and v < 0:
            raise ValueError(f"universe_max no puede ser negativo, recibido: {v}")
        return v

    @model_validator(mode="after")
    def _ensure_timestamps(self):
        if self.created_at is None:
            self.created_at = datetime.now(timezone.utc)
        if self.updated_at is None:
            self.updated_at = self.created_at
        return self

    @model_validator(mode="after")
    def _validate_actuator_type_for_outputs(self):
        """Validar que las variables de salida tengan actuator_type definido."""
        if self.variable_type == "output" and self.actuator_type is None:
            raise ValueError(
                f"Las variables de tipo 'output' deben tener actuator_type definido ('PWM' o 'DIGITAL'). "
                f"Variable: {self.name}"
            )
        if self.variable_type == "input" and self.actuator_type is not None:
            raise ValueError(
                f"Las variables de tipo 'input' no deben tener actuator_type. Variable: {self.name}"
            )
        return self

    @model_validator(mode="after")
    def _validate_universe_consistency(self):
        """Validar que universe_max > universe_min si ambos están definidos."""
        if self.universe_min is not None and self.universe_max is not None:
            if self.universe_max <= self.universe_min:
                raise ValueError(
                    f"universe_max ({self.universe_max}) debe ser mayor que universe_min ({self.universe_min})"
                )
        return self

    # -------------------------
    # Métodos de negocio para gestión de términos
    # -------------------------
    def add_term(self, term_id: FuzzyTermId):
        if term_id not in self.terms:
            self.terms.append(term_id)
            self.updated_at = datetime.now(timezone.utc)

    def remove_term(self, term_id: FuzzyTermId):
        if term_id in self.terms:
            self.terms.remove(term_id)
            self.updated_at = datetime.now(timezone.utc)

    def reorder_terms(self, new_order: List[FuzzyTermId]):
        if set(new_order) != set(self.terms):
            raise ValueError("El nuevo orden debe contener exactamente los mismos términos")
        self.terms = new_order
        self.updated_at = datetime.now(timezone.utc)

    # -------------------------
    # Métodos de configuración
    # -------------------------
    def update_description(self, description: str):
        self.description = description
        # validate_assignment=True hará correr validadores
        self.updated_at = datetime.now(timezone.utc)

    def change_type(self, new_type: str):
        self.variable_type = new_type
        self.updated_at = datetime.now(timezone.utc)

    def update_reference_id(self, reference_id: str):
        """Actualiza el reference_id que apunta a sensor-service o actuator-service."""
        if not reference_id or not reference_id.strip():
            raise ValueError("El reference_id no puede estar vacío")
        self.reference_id = reference_id.strip()
        self.updated_at = datetime.now(timezone.utc)

    # -------------------------
    # Métodos de consulta
    # -------------------------
    def is_input(self) -> bool:
        return self.variable_type == "input"

    def is_output(self) -> bool:
        return self.variable_type == "output"

    def get_term_count(self) -> int:
        return len(self.terms)

    def has_term(self, term_id: FuzzyTermId) -> bool:
        return term_id in self.terms

    def has_reference(self) -> bool:
        """Verifica si la variable tiene una referencia externa válida."""
        return self.reference_id is not None and len(self.reference_id.strip()) > 0

    # -------------------------
    # Serialización utilitaria
    # -------------------------
    def to_dict(self) -> Dict[str, Any]:
        result = {
            "_id": str(self.id) if self.id else None,
            "name": self.name,
            "description": self.description,
            "variable_type": self.variable_type,
            "reference_id": self.reference_id,
            "terms": [str(tid) for tid in self.terms],
            "createdAt": self.created_at.isoformat() if self.created_at else None,
            "updatedAt": self.updated_at.isoformat() if self.updated_at else None,
        }
        if self.actuator_type is not None:
            result["actuator_type"] = self.actuator_type
        if self.universe_min is not None:
            result["universe_min"] = self.universe_min
        if self.universe_max is not None:
            result["universe_max"] = self.universe_max
        # Siempre incluir defuzzification_threshold (tiene valor default)
        result["defuzzification_threshold"] = self.defuzzification_threshold
        return result

    def __str__(self) -> str:
        return f"FuzzyVariable({self.name}, {self.variable_type}, {self.get_term_count()} terms)"
