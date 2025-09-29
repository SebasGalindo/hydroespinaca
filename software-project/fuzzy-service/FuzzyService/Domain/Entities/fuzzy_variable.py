from datetime import datetime, timezone
from typing import List, Optional, Dict, Any

from pydantic import Field, field_validator, model_validator

from ..ValueObjects import FuzzyVariableId, FuzzyTermId
from ..Common import DomainBaseModel


class FuzzyVariable(DomainBaseModel):
    """Entidad del dominio que representa una variable difusa.
    
    Estructura basada en el JSON del plan:
    {
      "_id": { "$oid": "var_temp_air" },
      "name": "Temperatura del aire",
      "description": "Variable que representa la temperatura del aire en el invernadero",
      "type": "input|output",
      "deviceId": { "$oid": "sensor_temp_001" },
      "terms": [ { "$oid": "term_temp_low" }, ... ],
      "createdAt": "2025-01-27T23:39:17.917+00:00",
      "updatedAt": "2025-01-27T23:39:17.917+00:00"
    }
    """

    # Propiedades de identificación
    id: Optional[FuzzyVariableId] = None
    name: str = ""
    description: str = ""

    # Configuración de la variable
    variable_type: str = "input"  # "input" | "output"

    # Relaciones
    device_id: Optional[str] = None  # ID del sensor o actuador asociado
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

    @model_validator(mode="after")
    def _ensure_timestamps(self):
        if self.created_at is None:
            self.created_at = datetime.now(timezone.utc)
        if self.updated_at is None:
            self.updated_at = self.created_at
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

    def update_device_id(self, device_id: str):
        self.device_id = device_id
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

    def has_device(self) -> bool:
        return self.device_id is not None

    # -------------------------
    # Serialización utilitaria
    # -------------------------
    def to_dict(self) -> Dict[str, Any]:
        return {
            "_id": str(self.id) if self.id else None,
            "name": self.name,
            "description": self.description,
            "type": self.variable_type,
            "deviceId": self.device_id,
            "terms": [str(tid) for tid in self.terms],
            "createdAt": self.created_at.isoformat() if self.created_at else None,
            "updatedAt": self.updated_at.isoformat() if self.updated_at else None,
        }

    def __str__(self) -> str:
        return f"FuzzyVariable({self.name}, {self.variable_type}, {self.get_term_count()} terms)"
