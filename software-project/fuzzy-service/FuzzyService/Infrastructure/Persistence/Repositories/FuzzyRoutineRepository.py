from __future__ import annotations

import logging
from datetime import datetime, timezone
from typing import Any, Dict, List, Optional

from bson import ObjectId
from pymongo.errors import DuplicateKeyError

from FuzzyService.Domain.Entities.fuzzy_routine import FuzzyRoutine, RoutineStep
from FuzzyService.Domain.Interfaces.IFuzzyRoutineRepository import IFuzzyRoutineRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyRoutineId
from FuzzyService.Domain.Errors.DomainErrors import DuplicateEntityError, EntityNotFoundError, ValidationError

_logger = logging.getLogger(__name__)


class FuzzyRoutineRepository(IFuzzyRoutineRepository):
    """MongoDB-based repository for FuzzyRoutine entities (async PyMongo)."""

    def __init__(self, collection) -> None:
        self._coll = collection  # AsyncCollection

    # ---------------------------- Indexes ----------------------------
    async def ensure_indexes(self) -> None:
        try:
            await self._coll.create_index([("routine_name", 1)], unique=True, name="uq_routine_name")
            await self._coll.create_index([("created_at", -1)], name="ix_created_at_desc")
            await self._coll.create_index([("steps.step_id", 1)], name="ix_steps_step_id")
            await self._coll.create_index([("routine_name", "text")], name="ix_text_routine_name")
            _logger.info("FuzzyRoutine indexes ensured.")
        except Exception:
            _logger.exception("Failed ensuring indexes for FuzzyRoutine")
            raise

    # ---------------------------- Mappers ----------------------------
    @staticmethod
    def _to_object_id(id_value: Optional[FuzzyRoutineId | str]) -> Optional[Any]:
        if id_value is None:
            return None
        s = str(id_value)
        try:
            return ObjectId(s)
        except Exception:
            return s

    @staticmethod
    def _to_domain_id(db_id: Any) -> FuzzyRoutineId:
        return FuzzyRoutineId(str(db_id))

    @staticmethod
    def _entity_to_doc(r: FuzzyRoutine) -> Dict[str, Any]:
        now_utc = datetime.now(timezone.utc)
        created_at = r.created_at or now_utc
        steps = [
            {
                "step_id": int(s.step_id),
                "condition": s.condition,
                "power_term_id": str(s.power_term_id),
                "duration_term_id": str(s.duration_term_id),
            }
            for s in (r.steps or [])
        ]
        return {
            "_id": FuzzyRoutineRepository._to_object_id(r.id) or ObjectId(),
            "routine_name": r.routine_name,
            "created_at": created_at,
            "steps": steps,
        }

    @staticmethod
    def _doc_to_entity(doc: Dict[str, Any]) -> FuzzyRoutine:
        steps_raw = doc.get("steps") or []
        steps = [
            RoutineStep(
                step_id=int(s.get("step_id", 0)),
                condition=str(s.get("condition", "")),
                power_term_id=str(s.get("power_term_id", "")),
                duration_term_id=str(s.get("duration_term_id", "")),
            )
            for s in steps_raw
        ]
        return FuzzyRoutine(
            id=FuzzyRoutineRepository._to_domain_id(doc.get("_id")),
            routine_name=doc.get("routine_name", ""),
            created_at=doc.get("created_at"),
            steps=steps,
        )

    # ---------------------------- CRUD ----------------------------
    async def create(self, routine: FuzzyRoutine) -> FuzzyRoutine:
        doc = self._entity_to_doc(routine)
        try:
            res = await self._coll.insert_one(doc)
        except DuplicateKeyError:
            _logger.warning("Duplicate routine name: %s", routine.routine_name)
            raise DuplicateEntityError("Ya existe una rutina con ese nombre")
        created = await self.get_by_id(FuzzyRoutineId(str(res.inserted_id)))
        assert created is not None
        _logger.info("FuzzyRoutine created: id=%s", str(created.id))
        return created

    async def get_by_id(self, routine_id: FuzzyRoutineId) -> Optional[FuzzyRoutine]:
        key = self._to_object_id(routine_id)
        doc = await self._coll.find_one({"_id": key})
        return self._doc_to_entity(doc) if doc else None

    async def get_by_name(self, name: str) -> Optional[FuzzyRoutine]:
        doc = await self._coll.find_one({
            "routine_name": name
        }, projection={"_id": 1, "routine_name": 1, "created_at": 1, "steps": 1})
        return self._doc_to_entity(doc) if doc else None

    async def get_all(self, skip: int = 0, limit: int = 100) -> List[FuzzyRoutine]:
        cursor = self._coll.find({}, projection={"_id": 1, "routine_name": 1, "created_at": 1, "steps": 1}).skip(int(skip)).limit(int(limit)).sort("created_at", -1)
        return [self._doc_to_entity(d) async for d in cursor]

    async def update(self, routine: FuzzyRoutine) -> FuzzyRoutine:
        if routine.id is None:
            raise ValidationError("La rutina a actualizar debe tener id")
        key = self._to_object_id(routine.id)
        to_set = self._entity_to_doc(routine)
        to_set.pop("_id", None)
        try:
            res = await self._coll.update_one({"_id": key}, {"$set": to_set})
        except DuplicateKeyError:
            _logger.warning("Duplicate routine name on update: %s", routine.routine_name)
            raise DuplicateEntityError("Ya existe una rutina con ese nombre")
        if res.matched_count == 0:
            _logger.warning("FuzzyRoutine not found for update: id=%s", str(routine.id))
            raise EntityNotFoundError("Rutina no encontrada para actualizar")
        updated = await self.get_by_id(FuzzyRoutineId(str(routine.id)))
        assert updated is not None
        _logger.info("FuzzyRoutine updated: id=%s", str(routine.id))
        return updated

    async def delete(self, routine_id: FuzzyRoutineId) -> bool:
        key = self._to_object_id(routine_id)
        res = await self._coll.delete_one({"_id": key})
        deleted = res.deleted_count > 0
        if deleted:
            _logger.info("FuzzyRoutine deleted: id=%s", str(routine_id))
        return deleted

    async def exists(self, routine_id: FuzzyRoutineId) -> bool:
        key = self._to_object_id(routine_id)
        count = await self._coll.count_documents({"_id": key}, limit=1)
        return count > 0

    # ---------------------------- Search & Filter ----------------------------
    async def search_by_name(self, name_pattern: str, skip: int = 0, limit: int = 100) -> List[FuzzyRoutine]:
        cursor = self._coll.find({"$text": {"$search": name_pattern}}, projection={"_id": 1, "routine_name": 1, "created_at": 1, "steps": 1}).skip(int(skip)).limit(int(limit))
        return [self._doc_to_entity(d) async for d in cursor]

    async def filter_routines(self, filters: Dict[str, Any], skip: int = 0, limit: int = 100) -> List[FuzzyRoutine]:
        query: Dict[str, Any] = {}
        if (name_contains := filters.get("name_contains")):
            query["$text"] = {"$search": str(name_contains)}
        # step count bounds
        min_steps = int(filters.get("min_steps", 0))
        max_steps = filters.get("max_steps")
        size_expr = {"$size": "$steps"}
        expr = {"$gte": [size_expr, min_steps]}
        if max_steps is not None:
            expr = {"$and": [expr, {"$lte": [size_expr, int(max_steps)]}]}
        query = {"$and": [query, {"$expr": expr}]} if query else {"$expr": expr}
        # date range
        created_from = filters.get("created_from")
        created_to = filters.get("created_to")
        if created_from or created_to:
            dr: Dict[str, Any] = {}
            if created_from:
                dr["$gte"] = created_from
            if created_to:
                dr["$lte"] = created_to
            query["created_at"] = dr
        cursor = self._coll.find(query, projection={"_id": 1, "routine_name": 1, "created_at": 1, "steps": 1}).skip(int(skip)).limit(int(limit))
        return [self._doc_to_entity(d) async for d in cursor]

    # ---------------------------- Counting & Ranges ----------------------------
    async def get_routines_with_step_count(self, min_steps: int = 0, max_steps: Optional[int] = None) -> List[FuzzyRoutine]:
        size_expr = {"$size": "$steps"}
        query: Dict[str, Any] = {"$expr": {"$gte": [size_expr, int(min_steps)]}}
        if max_steps is not None:
            query = {"$and": [query, {"$expr": {"$lte": [size_expr, int(max_steps)]}}]}
        cursor = self._coll.find(query, projection={"_id": 1, "routine_name": 1, "created_at": 1, "steps": 1})
        return [self._doc_to_entity(d) async for d in cursor]

    async def count_total(self) -> int:
        return int(await self._coll.count_documents({}))

    async def get_by_date_range(self, start_date: datetime, end_date: datetime, skip: int = 0, limit: int = 100) -> List[FuzzyRoutine]:
        query = {"created_at": {"$gte": start_date, "$lte": end_date}}
        cursor = self._coll.find(query, projection={"_id": 1, "routine_name": 1, "created_at": 1, "steps": 1}).skip(int(skip)).limit(int(limit))
        return [self._doc_to_entity(d) async for d in cursor]

