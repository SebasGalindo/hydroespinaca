from datetime import datetime, timezone
from typing import List, Optional, Dict, Any

from pydantic import Field, field_validator, model_validator

from ..ValueObjects import (
    FuzzyRoutineId,
    DomainId,
)
from ..Common import DomainBaseModel
from ..Utils import extract_oid, parse_timestamp_utc


class RoutineStep(DomainBaseModel):
    step_id: int
    condition: str
    power_tag_id: DomainId
    duration_tag_id: DomainId

    @field_validator("step_id")
    @classmethod
    def _validate_step_id(cls, v: int) -> int:
        if not isinstance(v, int) or v < 0:
            raise ValueError("step_id debe ser un entero no negativo")
        return int(v)

    @field_validator("condition")
    @classmethod
    def _validate_condition(cls, v: str) -> str:
        if not isinstance(v, str) or not v.strip():
            raise ValueError("condition debe ser un string no vacío")
        return v.strip()

    @field_validator("power_tag_id", "duration_tag_id", mode="before")
    @classmethod
    def _coerce_domain_id(cls, v: Any):
        # Aceptar DomainId directo, string plano o dict estilo Mongo
        if isinstance(v, DomainId):
            return v
        if isinstance(v, dict):
            return extract_oid(v)
        return v  # Pydantic intentará convertir str -> DomainId

    def to_dict(self) -> Dict[str, Any]:
        return {
            "stepId": self.step_id,
            "condition": self.condition,
            "power_tag_id": str(self.power_tag_id),
            "duration_tag_id": str(self.duration_tag_id),
        }

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "RoutineStep":
        sid = data.get("stepId")
        cond = data.get("condition")
        ptag_raw = data.get("power_tag_id")
        dtag_raw = data.get("duration_tag_id")

        ptag = extract_oid(ptag_raw) if not isinstance(ptag_raw, str) else ptag_raw
        dtag = extract_oid(dtag_raw) if not isinstance(dtag_raw, str) else dtag_raw

        return cls(
            step_id=int(sid) if sid is not None else -1,
            condition=str(cond) if cond is not None else "",
            power_tag_id=ptag or "",
            duration_tag_id=dtag or "",
        )


class FuzzyRoutine(DomainBaseModel):
    id: Optional[FuzzyRoutineId] = None
    routine_name: str = ""
    created_at: Optional[datetime] = None

    steps: List[RoutineStep] = Field(default_factory=list)

    @field_validator("routine_name")
    @classmethod
    def _validate_routine_name(cls, v: str) -> str:
        if not isinstance(v, str) or not (1 <= len(v.strip()) <= 100):
            raise ValueError("routine_name debe ser string con longitud 1..100")
        return v.strip()

    @model_validator(mode="after")
    def _post_init(self):
        if self.created_at is None:
            self.created_at = datetime.now(timezone.utc)
        # Validar unicidad de step_id
        ids = [s.step_id for s in self.steps]
        if len(ids) != len(set(ids)):
            raise ValueError("stepId repetido en steps")
        return self

    # Métodos de negocio
    def add_step(self, step: RoutineStep):
        if any(s.step_id == step.step_id for s in self.steps):
            raise ValueError(f"Ya existe un step con stepId={step.step_id}")
        self.steps.append(step)

    def remove_step(self, step_id: int):
        self.steps = [s for s in self.steps if s.step_id != step_id]

    def to_dict(self) -> Dict[str, Any]:
        return {
            "routineId": str(self.id) if self.id else None,
            "routineName": self.routine_name,
            "createdAt": self.created_at.isoformat() if self.created_at else None,
            "steps": [s.to_dict() for s in self.steps],
        }

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "FuzzyRoutine":
        rid_raw = data.get("routineId")
        rname = data.get("routineName")
        cat = data.get("createdAt")
        steps_raw = data.get("steps", [])

        rid = extract_oid(rid_raw) if not isinstance(rid_raw, str) else rid_raw
        steps = [RoutineStep.from_dict(s) for s in steps_raw]

        # Validación de unicidad de stepId
        step_ids = [s.step_id for s in steps]
        if len(step_ids) != len(set(step_ids)):
            raise ValueError("Duplicated stepId en steps de FuzzyRoutine.from_dict")

        return cls(
            id=FuzzyRoutineId(rid) if rid else None,
            routine_name=str(rname) if rname is not None else "",
            created_at=parse_timestamp_utc(cat),
            steps=steps,
        )
