from __future__ import annotations

import logging
from datetime import datetime, timezone
from typing import Any, Dict, List, Optional

from bson import ObjectId
from pymongo.errors import DuplicateKeyError

from FuzzyService.Domain.Entities.fuzzy_system import FuzzySystem
from FuzzyService.Domain.Enums.EntityStatus import FuzzySystemStatus
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId, FuzzyVariableId, FuzzyRuleId
from FuzzyService.Domain.ValueObjects.OperatorsConfig import OperatorsConfig
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Errors.DomainErrors import DuplicateEntityError, EntityNotFoundError, ValidationError
from FuzzyService.Infrastructure.Configuration.DatabaseConfiguration import get_collection
from FuzzyService.Infrastructure.Constants.RepositoryConstants import (
    MONGO_SIZE_OPERATOR,
    MONGO_EXPR_OPERATOR,
    MONGO_ADD_OPERATOR,
    MONGO_GT_OPERATOR,
    MONGO_GTE_OPERATOR,
    MONGO_LTE_OPERATOR,
    MONGO_AND_OPERATOR,
)

_logger = logging.getLogger(__name__)

# Full projection for FuzzySystem documents — keeps all fields needed by FuzzySystemDto
_SYSTEM_PROJECTION = {
    "_id": 1, "name": 1, "status": 1, "defuzzification_method": 1,
    "operators": 1, "input_variable_ids": 1, "output_variable_ids": 1,
    "rule_ids": 1, "created_at": 1, "updated_at": 1, "created_by": 1,
}


class FuzzySystemRepository(IFuzzySystemRepository):
    """
    MongoDB-based repository for FuzzySystem entities.
    Works with AsyncCollection from PyMongo's asyncio API.
    """

    def __init__(self, collection) -> None:
        self._coll = collection  # expected AsyncCollection
        # cross-collection: variables for referential validations
        self._variables = get_collection("variables")

    # ---------------------------- Indexes ----------------------------
    async def ensure_indexes(self) -> None:
        try:
            await self._coll.create_index([("name", 1)], unique=True, name="uq_fuzzy_system_name")
            await self._coll.create_index([("status", 1)], name="ix_status")
            await self._coll.create_index([("created_at", -1)], name="ix_created_at_desc")
            # Text index for efficient name search
            await self._coll.create_index([("name", "text")], name="ix_text_name")
            _logger.info("FuzzySystem indexes ensured.")
        except Exception:
            _logger.exception("Failed ensuring indexes for FuzzySystem")
            raise

    # ---------------------------- Mappers ----------------------------
    @staticmethod
    def _to_object_id(id_value: Optional[FuzzySystemId | str]) -> Optional[Any]:
        if id_value is None:
            return None
        s = str(id_value)
        try:
            return ObjectId(s)
        except Exception:
            return s

    @staticmethod
    def _to_domain_id(db_id: Any) -> FuzzySystemId:
        return FuzzySystemId(str(db_id))

    @staticmethod
    def _entity_to_doc(fs: FuzzySystem) -> Dict[str, Any]:
        now_utc = datetime.now(timezone.utc)
        created_at = fs.created_at or now_utc
        updated_at = fs.updated_at or now_utc

        doc: Dict[str, Any] = {
            "_id": FuzzySystemRepository._to_object_id(fs.id) or ObjectId(),
            "name": fs.name,
            "status": fs.status.value if isinstance(fs.status, FuzzySystemStatus) else str(fs.status),
            "defuzzification_method": fs.defuzzification_method.value if hasattr(fs.defuzzification_method, "value") else str(fs.defuzzification_method),
            "operators": fs.operators.to_dict() if hasattr(fs.operators, "to_dict") else fs.operators,
            "input_variable_ids": [str(v) for v in fs.input_variable_ids],
            "output_variable_ids": [str(v) for v in fs.output_variable_ids],
            "rule_ids": [str(r) for r in fs.rule_ids],
            "created_at": created_at,
            "updated_at": updated_at,
            "created_by": fs.created_by,
        }
        return doc

    @staticmethod
    def _doc_to_entity(doc: Dict[str, Any]) -> FuzzySystem:
        # Normalizar status para asegurar conversión correcta
        raw_status = doc.get("status", "DRAFT")
        _logger.debug(f"📄 _doc_to_entity: raw_status='{raw_status}' (type={type(raw_status)})")

        try:
            # Si ya es un string, usarlo directamente; si es otro tipo, convertir
            status_str = str(raw_status).strip().upper()
            status_enum = FuzzySystemStatus(status_str)
            _logger.debug(f"✅ Status convertido: {status_enum} (type={type(status_enum)})")
        except (ValueError, KeyError) as e:
            _logger.warning(f"Invalid status value '{raw_status}' in document, defaulting to DRAFT: {e}")
            status_enum = FuzzySystemStatus.DRAFT

        return FuzzySystem(
            id=FuzzySystemRepository._to_domain_id(doc.get("_id")),
            name=doc.get("name"),
            status=status_enum,
            defuzzification_method=doc.get("defuzzification_method"),
            operators=OperatorsConfig.from_dict(doc.get("operators") or {}),
            input_variable_ids=[FuzzyVariableId(str(v)) for v in (doc.get("input_variable_ids") or [])],
            output_variable_ids=[FuzzyVariableId(str(v)) for v in (doc.get("output_variable_ids") or [])],
            rule_ids=[FuzzyRuleId(str(r)) for r in (doc.get("rule_ids") or [])],
            created_at=doc.get("created_at"),
            updated_at=doc.get("updated_at"),
            created_by=doc.get("created_by"),
        )

    # ---------------------------- CRUD ----------------------------
    async def create(self, fuzzy_system: FuzzySystem) -> FuzzySystem:
        # Basic validation
        try:
            status_value = fuzzy_system.status.value if isinstance(fuzzy_system.status, FuzzySystemStatus) else str(fuzzy_system.status)
            FuzzySystemStatus(status_value)
        except Exception:
            _logger.warning("Invalid status on create: %s", str(fuzzy_system.status))
            raise ValidationError(f"status inválido: {fuzzy_system.status}")

        # Pre-chequeo de duplicado por nombre (defensa adicional si el índice único aún no existe)
        existing = await self.get_by_name(fuzzy_system.name)
        if existing is not None:
            _logger.warning("Duplicate system name on pre-check: %s", fuzzy_system.name)
            raise DuplicateEntityError(f"Ya existe un sistema con el nombre '{fuzzy_system.name}'")

        doc = self._entity_to_doc(fuzzy_system)
        try:
            _logger.debug("Inserting FuzzySystem doc: %s", {k: doc[k] for k in doc if k != "_id"})
            res = await self._coll.insert_one(doc)
        except DuplicateKeyError:
            _logger.warning("Duplicate system name on create: %s", fuzzy_system.name)
            raise DuplicateEntityError(f"Ya existe un sistema con el nombre '{fuzzy_system.name}'")
        # Prefer the server-generated _id if present
        db_id = res.inserted_id if getattr(res, "inserted_id", None) else doc["_id"]
        _logger.info("FuzzySystem created with id=%s", str(db_id))
        created = await self.get_by_id(self._to_domain_id(db_id))
        assert created is not None
        return created

    async def get_by_id(self, system_id: FuzzySystemId) -> Optional[FuzzySystem]:
        key = self._to_object_id(system_id)
        doc = await self._coll.find_one({"_id": key})
        return self._doc_to_entity(doc) if doc else None

    async def get_by_name(self, name: str) -> Optional[FuzzySystem]:
        doc = await self._coll.find_one({"name": name}, projection=_SYSTEM_PROJECTION)
        return self._doc_to_entity(doc) if doc else None

    async def get_all(self, skip: int = 0, limit: int = 100) -> List[FuzzySystem]:
        cursor = self._coll.find({}).skip(int(skip)).limit(int(limit)).sort("created_at", -1)
        return [self._doc_to_entity(d) async for d in cursor]

    async def update(self, fuzzy_system: FuzzySystem) -> FuzzySystem:
        if fuzzy_system.id is None:
            raise ValidationError("El sistema a actualizar debe tener id")
        # Basic referential validation: referenced variables must exist
        input_ids = [str(v) for v in (fuzzy_system.input_variable_ids or [])]
        output_ids = [str(v) for v in (fuzzy_system.output_variable_ids or [])]
        all_ids = input_ids + output_ids
        if all_ids:
            db_ids: List[Any] = [self._to_object_id(s) for s in all_ids]
            found = await self._variables.count_documents({"_id": {"$in": db_ids}})
            if int(found) != len(all_ids):
                _logger.warning("Some referenced variables do not exist. expected=%d found=%d", len(all_ids), int(found))
                raise ValidationError("Algunas variables referenciadas no existen")

        # Validate status
        try:
            if isinstance(fuzzy_system.status, FuzzySystemStatus):
                status_value = fuzzy_system.status.value
            else:
                status_value = str(fuzzy_system.status)
            FuzzySystemStatus(status_value)
        except Exception:
            _logger.warning("Invalid status on update: %s", str(fuzzy_system.status))
            raise ValidationError(f"status inválido: {fuzzy_system.status}")

        key = self._to_object_id(fuzzy_system.id)
        now_utc = datetime.now(timezone.utc)
        to_set = self._entity_to_doc(fuzzy_system)
        to_set.pop("_id", None)
        to_set["updated_at"] = now_utc
        try:
            res = await self._coll.update_one({"_id": key}, {"$set": to_set})
        except DuplicateKeyError:
            _logger.warning("Duplicate system name on update: %s", fuzzy_system.name)
            raise DuplicateEntityError(f"Ya existe un sistema con el nombre '{fuzzy_system.name}'")
        if res.matched_count == 0:
            _logger.warning("FuzzySystem not found for update: id=%s", str(fuzzy_system.id))
            raise EntityNotFoundError("Sistema no encontrado")
        updated = await self._coll.find_one({"_id": key})
        assert updated is not None
        _logger.info("FuzzySystem updated: id=%s", str(fuzzy_system.id))
        return self._doc_to_entity(updated)

    async def update_status(self, system_id: FuzzySystemId, status: FuzzySystemStatus) -> FuzzySystem:
        """Updates only the status of a fuzzy system."""
        if system_id is None:
            raise ValidationError("El ID del sistema es requerido")
        
        # Validate status
        try:
            status_value = status.value if isinstance(status, FuzzySystemStatus) else str(status)
            FuzzySystemStatus(status_value)
        except Exception:
            _logger.warning("Invalid status on update_status: %s", str(status))
            raise ValidationError(f"status inválido: {status}")

        key = self._to_object_id(system_id)
        now_utc = datetime.now(timezone.utc)
        
        res = await self._coll.update_one(
            {"_id": key}, 
            {"$set": {"status": status_value, "updated_at": now_utc}}
        )
        
        if res.matched_count == 0:
            _logger.warning("FuzzySystem not found for status update: id=%s", str(system_id))
            raise EntityNotFoundError("Sistema no encontrado")
            
        updated = await self._coll.find_one({"_id": key})
        assert updated is not None
        _logger.info("FuzzySystem status updated: id=%s, new_status=%s", str(system_id), status_value)
        return self._doc_to_entity(updated)

    async def delete(self, system_id: FuzzySystemId) -> bool:
        key = self._to_object_id(system_id)
        res = await self._coll.delete_one({"_id": key})
        if res.deleted_count == 0:
            _logger.warning("FuzzySystem not found for delete: id=%s", str(system_id))
        else:
            _logger.info("FuzzySystem deleted: id=%s", str(system_id))
        return res.deleted_count > 0

    async def exists(self, system_id: FuzzySystemId) -> bool:
        key = self._to_object_id(system_id)
        count = await self._coll.count_documents({"_id": key}, limit=1)
        return count > 0

    # ---------------------------- Query methods ----------------------------
    async def search_by_name(self, name_pattern: str, skip: int = 0, limit: int = 100) -> List[FuzzySystem]:
        cursor = self._coll.find({"$text": {"$search": name_pattern}}, projection=_SYSTEM_PROJECTION) \
            .skip(int(skip)).limit(int(limit))
        return [self._doc_to_entity(d) async for d in cursor]

    async def get_by_date_range(self, start_date: datetime, end_date: datetime, skip: int = 0, limit: int = 100) -> List[FuzzySystem]:
        cursor = self._coll.find({
            "created_at": {"$gte": start_date, "$lte": end_date}
        }, projection=_SYSTEM_PROJECTION).skip(int(skip)).limit(int(limit)).sort("created_at", 1)
        return [self._doc_to_entity(d) async for d in cursor]

    async def count_by_status(self, status: FuzzySystemStatus) -> int:
        return await self._coll.count_documents({"status": status.value})

    async def count_total(self) -> int:
        return await self._coll.count_documents({})

    async def get_by_status(self, status: FuzzySystemStatus, skip: int = 0, limit: int = 100) -> List[FuzzySystem]:
        """Gets fuzzy systems by status using projection for performance."""
        q_status = status.value if isinstance(status, FuzzySystemStatus) else str(status)
        _logger.debug("Fetching systems by status: %s (skip=%s, limit=%s)", q_status, skip, limit)
        cursor = self._coll.find(
            {"status": q_status},
            projection=_SYSTEM_PROJECTION,
        ).skip(int(skip)).limit(int(limit)).sort("created_at", -1)
        return [self._doc_to_entity(d) async for d in cursor]

    async def get_active_systems(self, skip: int = 0, limit: int = 100) -> List[FuzzySystem]:
        """Gets systems with ACTIVE status."""
        _logger.debug("Fetching ACTIVE systems (skip=%s, limit=%s)", skip, limit)
        return await self.get_by_status(FuzzySystemStatus.ACTIVE, skip=skip, limit=limit)

    async def filter_systems(self, filters: Dict[str, Any], skip: int = 0, limit: int = 100) -> List[FuzzySystem]:
        query: Dict[str, Any] = {}
        if (s := filters.get("status")):
            query["status"] = s.value if isinstance(s, FuzzySystemStatus) else str(s)
        if (name_contains := filters.get("name_contains")):
            query["$text"] = {"$search": name_contains}
        if (created_after := filters.get("created_after")):
            query.setdefault("created_at", {})["$gte"] = created_after
        if (created_before := filters.get("created_before")):
            query.setdefault("created_at", {})["$lte"] = created_before
        if (has_vars := filters.get("has_variables")) is True:
            _logger.warning("Query using $expr/$size to check variable counts may be expensive. Consider maintaining a denormalized count.")
            query[MONGO_EXPR_OPERATOR] = {MONGO_GT_OPERATOR: [{MONGO_ADD_OPERATOR: [{MONGO_SIZE_OPERATOR: "$input_variable_ids"}, {MONGO_SIZE_OPERATOR: "$output_variable_ids"}]}, 0]}
        cursor = self._coll.find(query, projection=_SYSTEM_PROJECTION).skip(int(skip)).limit(int(limit))
        return [self._doc_to_entity(d) async for d in cursor]

    async def get_systems_with_variable_count(self, min_variables: int = 0, max_variables: Optional[int] = None) -> List[FuzzySystem]:
        expr = {MONGO_ADD_OPERATOR: [{MONGO_SIZE_OPERATOR: "$input_variable_ids"}, {MONGO_SIZE_OPERATOR: "$output_variable_ids"}]}
        query: Dict[str, Any] = {MONGO_EXPR_OPERATOR: {MONGO_GTE_OPERATOR: [expr, int(min_variables)]}}
        if max_variables is not None:
            query = {MONGO_AND_OPERATOR: [query, {MONGO_EXPR_OPERATOR: {MONGO_LTE_OPERATOR: [expr, int(max_variables)]}}]}
        _logger.warning("Query using $expr/$size for variable_count may be expensive on large collections.")
        cursor = self._coll.find(query, projection=_SYSTEM_PROJECTION)
        return [self._doc_to_entity(d) async for d in cursor]

    async def get_systems_with_rule_count(self, min_rules: int = 0, max_rules: Optional[int] = None) -> List[FuzzySystem]:
        expr = {MONGO_SIZE_OPERATOR: "$rule_ids"}
        query: Dict[str, Any] = {MONGO_EXPR_OPERATOR: {MONGO_GTE_OPERATOR: [expr, int(min_rules)]}}
        if max_rules is not None:
            query = {MONGO_AND_OPERATOR: [query, {MONGO_EXPR_OPERATOR: {MONGO_LTE_OPERATOR: [expr, int(max_rules)]}}]}
        _logger.warning("Query using $expr/$size for rule_count may be expensive on large collections.")
        cursor = self._coll.find(query, projection=_SYSTEM_PROJECTION)
        return [self._doc_to_entity(d) async for d in cursor]

    # ---------------------------- Variable-specific queries ----------------------------
    async def get_systems_with_input_variable(self, variable_id: FuzzyVariableId) -> List[FuzzySystem]:
        """Gets fuzzy systems that contain a specific input variable."""
        var_id_str = str(variable_id)
        cursor = self._coll.find(
            {"input_variable_ids": var_id_str},
            projection=_SYSTEM_PROJECTION
        )
        return [self._doc_to_entity(d) async for d in cursor]

    async def get_systems_with_output_variable(self, variable_id: FuzzyVariableId) -> List[FuzzySystem]:
        """Gets fuzzy systems that contain a specific output variable."""
        var_id_str = str(variable_id)
        cursor = self._coll.find(
            {"output_variable_ids": var_id_str},
            projection=_SYSTEM_PROJECTION
        )
        return [self._doc_to_entity(d) async for d in cursor]

    # ---------------------------- Activation methods ----------------------------
    async def deactivate_all_active(self) -> int:
        """Deactivates all currently active fuzzy systems (sets status to INACTIVE)."""
        now_utc = datetime.now(timezone.utc)
        res = await self._coll.update_many(
            {"status": FuzzySystemStatus.ACTIVE.value},
            {"$set": {"status": FuzzySystemStatus.INACTIVE.value, "updated_at": now_utc}}
        )
        count = res.modified_count
        if count > 0:
            _logger.info("Deactivated %d active FuzzySystem(s)", count)
        return count
