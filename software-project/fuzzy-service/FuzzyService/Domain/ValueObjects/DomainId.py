from __future__ import annotations
from typing import Any
from pydantic import RootModel, field_validator, ConfigDict
from bson import ObjectId
import uuid


class DomainId(RootModel[str]):
    """
    Value Object de ID basado en Pydantic v2 (RootModel[str]).
    Acepta strings o dicts estilo Mongo {"$oid": "..."} / {"oid": "..."} y
    valida que sea un ObjectId válido o un UUID válido.
    """

    model_config = ConfigDict(frozen=True)

    def __init__(self, root: Any | None = None):
        # Si no se provee valor, generamos un UUID válido por defecto
        if root is None:
            root = str(uuid.uuid4())
        super().__init__(root=root)

    @classmethod
    def generate(cls) -> "DomainId":
        """Genera un nuevo ID único (UUIDv4) respetando la subclase."""
        return cls(str(uuid.uuid4()))

    @field_validator("root", mode="before")
    @classmethod
    def _coerce_mongo_oid(cls, v: Any) -> str:
        if v is None:
            # Ya manejamos None en __init__, aquí mantenemos el contrato
            raise ValueError("DomainId no puede ser None")
        if isinstance(v, dict):
            if "$oid" in v and isinstance(v["$oid"], str):
                return v["$oid"]
            if "oid" in v and isinstance(v["oid"], str):
                return v["oid"]
            raise ValueError("Formato de id no soportado para DomainId")
        if isinstance(v, (bytes, bytearray)):
            v = v.decode()
        if not isinstance(v, str):
            v = str(v)
        return v

    @field_validator("root")
    @classmethod
    def _validate_id_format(cls, v: str) -> str:
        # Aceptamos Mongo ObjectId o UUID
        if cls._is_valid_object_id(v) or cls._is_valid_uuid(v):
            return v
        raise ValueError("DomainId debe ser ObjectId o UUID válido")

    @staticmethod
    def _is_valid_object_id(value: str) -> bool:
        try:
            ObjectId(str(value))
            return True
        except Exception:
            return False

    @staticmethod
    def _is_valid_uuid(value: str) -> bool:
        try:
            uuid.UUID(str(value))
            return True
        except Exception:
            return False

    def __str__(self) -> str:  # Para serialización amigable
        return self.root

    def __eq__(self, other: Any) -> bool:
        if isinstance(other, DomainId):
            return self.root == other.root
        if isinstance(other, str):
            return self.root == other
        return NotImplemented

    def __hash__(self) -> int:
        return hash(self.root)


class FuzzySystemId(DomainId):
    """ID específico para sistemas difusos."""


class FuzzyVariableId(DomainId):
    """ID específico para variables difusas."""


class FuzzyTermId(DomainId):
    """ID específico para términos difusos."""


class FuzzyRuleId(DomainId):
    """ID específico para reglas difusas."""


class FuzzyRoutineId(DomainId):
    """ID específico para rutinas difusas."""


class FuzzyEvaluationId(DomainId):
    """ID específico para evaluaciones difusas."""


class ActuatorId(DomainId):
    """ID específico para actuadores."""
