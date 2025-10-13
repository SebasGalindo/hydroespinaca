from __future__ import annotations

import logging
from datetime import datetime, timezone
from typing import Any, Dict, List, Optional

from bson import ObjectId

from FuzzyService.Domain.Entities.fuzzy_evaluation import (
    FuzzyEvaluation,
    InputValue,
    RuleActivation,
    OutputValue,
)
from FuzzyService.Domain.Interfaces.IFuzzyEvaluationRepository import IFuzzyEvaluationRepository
from FuzzyService.Domain.ValueObjects.DomainId import (
    FuzzyEvaluationId,
    FuzzySystemId,
    FuzzyRuleId,
)
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError, ValidationError

_logger = logging.getLogger(__name__)


class FuzzyEvaluationRepository(IFuzzyEvaluationRepository):
    """MongoDB-based repository for FuzzyEvaluation entities (async PyMongo)."""

    def __init__(self, collection) -> None:
        self._coll = collection  # AsyncCollection

    # ---------------------------- Indexes ----------------------------
    async def ensure_indexes(self) -> None:
        try:
            # Common query patterns
            await self._coll.create_index([("system_id", 1), ("timestamp", -1)], name="ix_system_time")
            await self._coll.create_index([("timestamp", -1)], name="ix_timestamp_desc")
            await self._coll.create_index([("inputs.sensor_id", 1)], name="ix_inputs_sensor")
            await self._coll.create_index([("activated_rules.ruleId", 1)], name="ix_activations_rule")
            await self._coll.create_index([("activated_rules.firingStrength", -1)], name="ix_activations_strength_desc")
            await self._coll.create_index([("activated_rules.output_values.reference_code", 1)], name="ix_outputs_reference")
            _logger.info("FuzzyEvaluation indexes ensured.")
        except Exception:
            _logger.exception("Failed ensuring indexes for FuzzyEvaluation")
            raise

    # ---------------------------- Mappers ----------------------------
    @staticmethod
    def _to_object_id(id_value: Optional[FuzzyEvaluationId | FuzzySystemId | FuzzyRuleId | str]) -> Optional[Any]:
        if id_value is None:
            return None
        s = str(id_value)
        try:
            return ObjectId(s)
        except Exception:
            return s

    @staticmethod
    def _to_eval_id(db_id: Any) -> FuzzyEvaluationId:
        return FuzzyEvaluationId(str(db_id))

    @staticmethod
    def _entity_to_doc(e: FuzzyEvaluation) -> Dict[str, Any]:
        # Normalize timestamp
        ts = e.timestamp or datetime.now(timezone.utc)
        inputs = [
            {"sensor_id": iv.sensor_id, "value": float(iv.value)}
            for iv in (e.inputs or [])
        ]
        activations = []
        for ra in (e.activated_rules or []):
            output_values = []
            for ov in (ra.output_values or []):
                output_dict = {
                    "reference_code": ov.reference_code,
                    "duration": float(ov.duration),
                }
                if ov.power is not None:
                    output_dict["power"] = ov.power
                elif ov.dutyCycle is not None:
                    output_dict["dutyCycle"] = float(ov.dutyCycle)

                output_values.append(output_dict)

            activations.append({
                "ruleId": str(ra.rule_id) if ra.rule_id else None,
                "firingStrength": float(ra.firing_strength),
                "output_values": output_values,
            })
        return {
            "_id": FuzzyEvaluationRepository._to_object_id(e.id) or ObjectId(),
            "system_id": str(e.system_id) if e.system_id else None,
            "timestamp": ts,
            "inputs": inputs,
            "activated_rules": activations,
        }

    @staticmethod
    def _doc_to_entity(doc: Dict[str, Any]) -> FuzzyEvaluation:
        inputs = [InputValue.from_dict(iv) for iv in (doc.get("inputs") or [])]
        activations = [RuleActivation.from_dict(ra) for ra in (doc.get("activated_rules") or [])]
        return FuzzyEvaluation(
            id=FuzzyEvaluationRepository._to_eval_id(doc.get("_id")),
            system_id=FuzzySystemId(doc.get("system_id")) if doc.get("system_id") else None,
            timestamp=doc.get("timestamp"),
            inputs=inputs,
            activated_rules=activations,
        )

    # ---------------------------- CRUD ----------------------------
    async def create(self, evaluation: FuzzyEvaluation) -> FuzzyEvaluation:
        if not evaluation.system_id:
            raise ValidationError("La evaluación debe pertenecer a un sistema (system_id requerido)")
        if evaluation.timestamp is None:
            evaluation.timestamp = datetime.now(timezone.utc)
        doc = self._entity_to_doc(evaluation)
        res = await self._coll.insert_one(doc)
        created = await self.get_by_id(FuzzyEvaluationId(str(res.inserted_id)))
        assert created is not None
        _logger.info("FuzzyEvaluation created: id=%s", str(created.id))
        return created

    async def get_by_id(self, evaluation_id: FuzzyEvaluationId) -> Optional[FuzzyEvaluation]:
        key = self._to_object_id(evaluation_id)
        doc = await self._coll.find_one({"_id": key})
        return self._doc_to_entity(doc) if doc else None

    async def get_all(self, skip: int = 0, limit: int = 100) -> List[FuzzyEvaluation]:
        cursor = (
            self._coll.find(
                {},
                projection={
                    "_id": 1,
                    "system_id": 1,
                    "timestamp": 1,
                    "inputs": 1,
                    "activated_rules": 1,
                },
            )
            .skip(int(skip))
            .limit(int(limit))
            .sort("timestamp", -1)
        )
        return [self._doc_to_entity(d) async for d in cursor]

    async def update(self, evaluation: FuzzyEvaluation) -> FuzzyEvaluation:
        if evaluation.id is None:
            raise ValidationError("La evaluación a actualizar debe tener id")
        key = self._to_object_id(evaluation.id)
        to_set = self._entity_to_doc(evaluation)
        to_set.pop("_id", None)
        res = await self._coll.update_one({"_id": key}, {"$set": to_set})
        if res.matched_count == 0:
            _logger.warning("FuzzyEvaluation not found for update: id=%s", str(evaluation.id))
            raise EntityNotFoundError("Evaluación no encontrada para actualizar")
        updated = await self.get_by_id(FuzzyEvaluationId(str(evaluation.id)))
        assert updated is not None
        _logger.info("FuzzyEvaluation updated: id=%s", str(evaluation.id))
        return updated

    async def delete(self, evaluation_id: FuzzyEvaluationId) -> bool:
        key = self._to_object_id(evaluation_id)
        res = await self._coll.delete_one({"_id": key})
        deleted = res.deleted_count > 0
        if deleted:
            _logger.info("FuzzyEvaluation deleted: id=%s", str(evaluation_id))
        else:
            _logger.warning("FuzzyEvaluation not found to delete: id=%s", str(evaluation_id))
        return deleted

    # ---------------------------- Queries ----------------------------
    async def get_by_system_id(self, system_id: FuzzySystemId, skip: int = 0, limit: int = 100) -> List[FuzzyEvaluation]:
        cursor = (
            self._coll.find({"system_id": str(system_id)})
            .skip(int(skip))
            .limit(int(limit))
            .sort("timestamp", -1)
        )
        return [self._doc_to_entity(d) async for d in cursor]

    async def get_by_rule_id(self, rule_id: FuzzyRuleId, skip: int = 0, limit: int = 100) -> List[FuzzyEvaluation]:
        cursor = (
            self._coll.find({"activated_rules.ruleId": str(rule_id)})
            .skip(int(skip))
            .limit(int(limit))
            .sort("timestamp", -1)
        )
        return [self._doc_to_entity(d) async for d in cursor]

    async def get_by_date_range(self, start_date: datetime, end_date: datetime, skip: int = 0, limit: int = 100) -> List[FuzzyEvaluation]:
        cursor = (
            self._coll.find({"timestamp": {"$gte": start_date, "$lte": end_date}})
            .skip(int(skip))
            .limit(int(limit))
            .sort("timestamp", -1)
        )
        return [self._doc_to_entity(d) async for d in cursor]

    async def filter_evaluations(self, filters: Dict[str, Any], skip: int = 0, limit: int = 100) -> List[FuzzyEvaluation]:
        query: Dict[str, Any] = {}
        if not isinstance(filters, dict):
            filters = {}
        # System filter
        sys_id = filters.get("system_id")
        if sys_id:
            query["system_id"] = str(sys_id)
        # Rule filter
        rule_id = filters.get("rule_id")
        if rule_id:
            query["activated_rules.ruleId"] = str(rule_id)
        # Sensor filter
        sensor_id = filters.get("sensor_id")
        if sensor_id:
            query["inputs.sensor_id"] = str(sensor_id)
        # Reference code filter
        reference_code = filters.get("reference_code")
        if reference_code:
            query["activated_rules.output_values.reference_code"] = str(reference_code)
        # Firing strength range
        min_fs = filters.get("min_firing_strength")
        max_fs = filters.get("max_firing_strength")
        fs_cond: Dict[str, Any] = {}
        if isinstance(min_fs, (int, float)):
            fs_cond["$gte"] = float(min_fs)
        if isinstance(max_fs, (int, float)):
            fs_cond["$lte"] = float(max_fs)
        if fs_cond:
            query["activated_rules.firingStrength"] = fs_cond
        # Timestamp range
        start_ts = filters.get("min_timestamp")
        end_ts = filters.get("max_timestamp")
        ts_cond: Dict[str, Any] = {}
        if isinstance(start_ts, datetime):
            ts_cond["$gte"] = start_ts
        if isinstance(end_ts, datetime):
            ts_cond["$lte"] = end_ts
        if ts_cond:
            query["timestamp"] = ts_cond

        cursor = (
            self._coll.find(query)
            .skip(int(skip))
            .limit(int(limit))
            .sort("timestamp", -1)
        )
        return [self._doc_to_entity(d) async for d in cursor]

    async def count_by_system(self, system_id: FuzzySystemId) -> int:
        return await self._coll.count_documents({"system_id": str(system_id)})

    async def count_total(self) -> int:
        return await self._coll.count_documents({})
