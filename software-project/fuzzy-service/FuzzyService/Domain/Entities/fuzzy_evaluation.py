from datetime import datetime, timezone
from typing import List, Optional, Dict, Any, ClassVar

from pydantic import Field, field_validator, model_validator

from ..ValueObjects import (
    FuzzyEvaluationId,
    FuzzySystemId,
    FuzzyRuleId,
    ActuatorId,
)
from ..Enums import PowerRange, DurationRange
from ..Utils import extract_oid, parse_timestamp_utc
from ..Common import DomainBaseModel


# Tipos de apoyo para la evaluación
class InputValue(DomainBaseModel):
    sensor_id: str
    value: float

    @field_validator("sensor_id")
    @classmethod
    def _validate_sensor_id(cls, v: str) -> str:
        if not isinstance(v, str) or not v.strip():
            raise ValueError("sensor_id debe ser un string no vacío")
        return v.strip()

    @field_validator("value", mode="before")
    @classmethod
    def _validate_value(cls, v):
        if not isinstance(v, (int, float)):
            raise ValueError("value debe ser numérico")
        return float(v)

    def to_dict(self) -> Dict[str, Any]:
        return {
            "sensor_id": self.sensor_id,
            "value": self.value,
        }

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "InputValue":
        sid = data.get("sensor_id")
        sid_norm = extract_oid(sid) if not isinstance(sid, str) else sid
        return cls(
            sensor_id=sid_norm or "",
            value=data.get("value"),
        )


class OutputValue(DomainBaseModel):
    actuator_id: ActuatorId | str
    power: float | int
    duration: float | int

    # Configuración por defecto basada en enums
    DEFAULT_POWER_RANGE: ClassVar[PowerRange] = PowerRange.PERCENT_0_100
    DEFAULT_DURATION_RANGE: ClassVar[DurationRange] = DurationRange.SECONDS_5_60

    @field_validator("actuator_id", mode="before")
    @classmethod
    def _coerce_actuator_id(cls, v):
        if isinstance(v, dict):
            v = extract_oid(v)
        return v

    @field_validator("power", "duration", mode="before")
    @classmethod
    def _coerce_numeric(cls, v):
        if not isinstance(v, (int, float)):
            raise ValueError("power/duration deben ser numéricos")
        return float(v)

    @model_validator(mode="after")
    def _validate_ranges(self):
        pmin, pmax = self.DEFAULT_POWER_RANGE.min, self.DEFAULT_POWER_RANGE.max
        if not (pmin <= float(self.power) <= pmax):
            raise ValueError(f"power fuera de rango permitido [{pmin}, {pmax}]")
        dmin, dmax = self.DEFAULT_DURATION_RANGE.min, self.DEFAULT_DURATION_RANGE.max
        if not (dmin <= float(self.duration) <= dmax):
            raise ValueError(f"duration fuera de rango permitido [{dmin}, {dmax}]")
        return self

    def to_dict(self) -> Dict[str, Any]:
        return {
            "actuator_id": str(self.actuator_id),
            "power": float(self.power),
            "duration": float(self.duration),
        }

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "OutputValue":
        aid_raw = data.get("actuator_id")
        if aid_raw is None:
            aid_raw = data.get("actuator")
        aid = extract_oid(aid_raw) if not isinstance(aid_raw, str) else aid_raw
        return cls(
            actuator_id=aid or "",
            power=data.get("power"),
            duration=data.get("duration"),
        )


class RuleActivation(DomainBaseModel):
    rule_id: FuzzyRuleId | str
    firing_strength: float
    output_values: List[OutputValue] = Field(default_factory=list)

    @field_validator("rule_id", mode="before")
    @classmethod
    def _coerce_rule_id(cls, v):
        if isinstance(v, dict):
            v = extract_oid(v)
        return v

    @field_validator("firing_strength", mode="before")
    @classmethod
    def _validate_strength(cls, v):
        if not isinstance(v, (int, float)):
            raise ValueError("firingStrength debe ser numérico")
        fv = float(v)
        if not (0.0 <= fv <= 1.0):
            raise ValueError("firingStrength debe ser un número entre 0 y 1")
        return fv

    def to_dict(self) -> Dict[str, Any]:
        return {
            "ruleId": str(self.rule_id),
            "firingStrength": float(self.firing_strength),
            "output_values": [ov.to_dict() for ov in self.output_values],
        }

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "RuleActivation":
        rid_raw = data.get("ruleId")
        rid = extract_oid(rid_raw) if not isinstance(rid_raw, str) else rid_raw
        outputs = [OutputValue.from_dict(ov) for ov in data.get("output_values", [])]
        return cls(
            rule_id=rid or "",
            firing_strength=data.get("firingStrength"),
            output_values=outputs,
        )


class FuzzyEvaluation(DomainBaseModel):
    """
    Registro de evaluación del sistema difuso.
    Basado en la estructura proporcionada:
    {
      "evalId": { "$oid": "eval_uuid_12345" },
      "systemId": { "$oid": "fuzzy_1" },
      "timestamp": "2025-01-27T23:39:17.917+00:00",
      "inputs": [ {"sensor_id": {"$oid": "..."}, "value": 6.5}, ... ],
      "activated_rules": [ {"ruleId": {"$oid": "..."}, "firingStrength": 0.8, "output_values": [...] } ]
    }
    """

    id: Optional[FuzzyEvaluationId] = None
    system_id: Optional[FuzzySystemId] = None
    timestamp: Optional[datetime] = None

    inputs: List[InputValue] = Field(default_factory=list)
    activated_rules: List[RuleActivation] = Field(default_factory=list)

    @field_validator("id", "system_id", mode="before")
    @classmethod
    def _coerce_ids(cls, v):
        if isinstance(v, dict):
            return extract_oid(v)
        return v

    @model_validator(mode="after")
    def _set_timestamp(self):
        if self.timestamp is None:
            self.timestamp = datetime.now(timezone.utc)
        return self

    # Métodos de negocio
    def add_input(self, sensor_id: str, value: float | int):
        self.inputs.append(InputValue(sensor_id=sensor_id, value=value))

    def add_rule_activation(self, rule_activation: RuleActivation):
        self.activated_rules.append(rule_activation)

    def to_dict(self) -> Dict[str, Any]:
        return {
            "evalId": str(self.id) if self.id else None,
            "systemId": str(self.system_id) if self.system_id else None,
            "timestamp": self.timestamp.isoformat() if self.timestamp else None,
            "inputs": [i.to_dict() for i in self.inputs],
            "activated_rules": [ra.to_dict() for ra in self.activated_rules],
        }

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "FuzzyEvaluation":
        eid_raw = data.get("evalId")
        sid_raw = data.get("systemId")
        ts_raw = data.get("timestamp")

        eid = extract_oid(eid_raw) if not isinstance(eid_raw, str) else eid_raw
        sid = extract_oid(sid_raw) if not isinstance(sid_raw, str) else sid_raw

        inputs = [InputValue.from_dict(iv) for iv in data.get("inputs", [])]
        activations = [RuleActivation.from_dict(ra) for ra in data.get("activated_rules", [])]

        return cls(
            id=FuzzyEvaluationId(eid) if eid else None,
            system_id=FuzzySystemId(sid) if sid else None,
            timestamp=parse_timestamp_utc(ts_raw),
            inputs=inputs,
            activated_rules=activations,
        )
