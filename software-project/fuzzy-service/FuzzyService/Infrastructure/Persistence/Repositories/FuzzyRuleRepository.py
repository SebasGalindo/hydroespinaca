from __future__ import annotations

import logging
import re

from datetime import datetime, timezone
from typing import Any, Dict, List, Optional

from bson import ObjectId
from pymongo.errors import DuplicateKeyError

from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository
from FuzzyService.Domain.ValueObjects.DomainId import (
    FuzzyRuleId,
    FuzzySystemId,
    FuzzyVariableId,
)
from FuzzyService.Domain.Enums import RuleConnector
from FuzzyService.Domain.Errors.DomainErrors import DuplicateEntityError, EntityNotFoundError, ValidationError
    
_logger = logging.getLogger(__name__)


class FuzzyRuleRepository(IFuzzyRuleRepository):
    """MongoDB-based repository for FuzzyRule entities (async PyMongo)."""

    def __init__(self, collection) -> None:
        self._coll = collection  # AsyncCollection

    # ---------------------------- Indexes ----------------------------
    async def ensure_indexes(self) -> None:
        try:
            # unique rule name per system
            await self._coll.create_index([("system_id", 1), ("name", 1)], unique=True, name="uq_rule_system_name")
            await self._coll.create_index([("system_id", 1)], name="ix_system_id")
            await self._coll.create_index([("created_at", -1)], name="ix_created_at_desc")
            await self._coll.create_index([("conditions.variableId", 1)], name="ix_conditions_variable")
            await self._coll.create_index([("connectors", 1)], name="ix_connectors_array")
            await self._coll.create_index([("name", "text")], name="ix_text_name")
            _logger.info("FuzzyRule indexes ensured.")
        except Exception:
            _logger.exception("Failed ensuring indexes for FuzzyRule")
            raise

    # ---------------------------- Mappers ----------------------------
    @staticmethod
    def _to_object_id(id_value: Optional[FuzzyRuleId | FuzzySystemId | FuzzyVariableId | str]) -> Optional[Any]:
        if id_value is None:
            return None
        s = str(id_value)
        try:
            return ObjectId(s)
        except Exception:
            return s

    @staticmethod
    def _to_rule_id(db_id: Any) -> FuzzyRuleId:
        return FuzzyRuleId(str(db_id))

    @staticmethod
    def _entity_to_doc(r: FuzzyRule) -> Dict[str, Any]:
        conds = [
            {
                "variableId": str(c.get("variableId")),
                "operator": str(c.get("operator")),
                "value": c.get("value"),
            }
            for c in (r.conditions or [])
        ]
        conns = [str(c.value) if hasattr(c, "value") else str(c) for c in (r.connectors or [])]

        return {
            "_id": FuzzyRuleRepository._to_object_id(r.id) or ObjectId(),
            "name": r.name,
            "system_id": str(r.system_id) if r.system_id else None,
            "description": r.description,
            "conditions": conds,
            "connectors": conns,
            "consequents": [c.to_dict() for c in r.consequents],
            "created_at": r.created_at or datetime.now(timezone.utc),
        }

    @staticmethod
    def _doc_to_entity(doc: Dict[str, Any]) -> FuzzyRule:
        from FuzzyService.Domain.Entities.rule_consequent import RuleConsequent

        # Parsear consecuentes Mamdani
        consequents_data = doc.get("consequents", [])
        consequents = [RuleConsequent.from_dict(cons_dict) for cons_dict in consequents_data]

        return FuzzyRule(
            id=FuzzyRuleRepository._to_rule_id(doc.get("_id")),
            name=doc.get("name", ""),
            system_id=FuzzySystemId(doc.get("system_id")) if doc.get("system_id") else None,
            description=doc.get("description"),
            conditions=[
                {
                    "variableId": FuzzyVariableId(c.get("variableId")) if c.get("variableId") else None,
                    "operator": c.get("operator"),
                    "value": c.get("value"),
                }
                for c in (doc.get("conditions") or [])
            ],
            connectors=[RuleConnector(c) for c in (doc.get("connectors") or [])],
            consequents=consequents,
            created_at=doc.get("created_at"),
        )

    # ---------------------------- CRUD ----------------------------
    async def create(self, fuzzy_rule: FuzzyRule) -> FuzzyRule:
        if not fuzzy_rule.system_id:
            raise ValidationError("La regla debe pertenecer a un sistema (system_id requerido)")
        doc = self._entity_to_doc(fuzzy_rule)
        try:
            res = await self._coll.insert_one(doc)
        except DuplicateKeyError:
            _logger.warning("Duplicate rule name in system: %s", fuzzy_rule.name)
            raise DuplicateEntityError("Ya existe una regla con ese nombre en el sistema")
        created = await self.get_by_id(FuzzyRuleId(str(res.inserted_id)))
        assert created is not None
        _logger.info("FuzzyRule created: id=%s", str(created.id))
        return created

    async def get_by_id(self, rule_id: FuzzyRuleId) -> Optional[FuzzyRule]:
        key = self._to_object_id(rule_id)
        doc = await self._coll.find_one({"_id": key})
        return self._doc_to_entity(doc) if doc else None

    async def get_all(self, skip: int = 0, limit: int = 100) -> List[FuzzyRule]:
        cursor = self._coll.find({}, projection={"_id": 1, "name": 1, "system_id": 1, "description": 1, "conditions": 1, "connectors": 1, "consequents": 1, "created_at": 1}).skip(int(skip)).limit(int(limit)).sort("created_at", -1)
        return [self._doc_to_entity(d) async for d in cursor]

    async def update(self, fuzzy_rule: FuzzyRule) -> FuzzyRule:
        if fuzzy_rule.id is None:
            raise ValidationError("La regla a actualizar debe tener id")
        if not fuzzy_rule.system_id:
            raise ValidationError("La regla debe pertenecer a un sistema (system_id requerido)")
        key = self._to_object_id(fuzzy_rule.id)
        to_set = self._entity_to_doc(fuzzy_rule)
        to_set.pop("_id", None)
        try:
            res = await self._coll.update_one({"_id": key}, {"$set": to_set})
        except DuplicateKeyError:
            _logger.warning("Duplicate rule name on update: %s", fuzzy_rule.name)
            raise DuplicateEntityError("Ya existe una regla con ese nombre en el sistema")
        if res.matched_count == 0:
            _logger.warning("FuzzyRule not found for update: id=%s", str(fuzzy_rule.id))
            raise EntityNotFoundError("Regla no encontrada para actualizar")
        updated = await self.get_by_id(FuzzyRuleId(str(fuzzy_rule.id)))
        assert updated is not None
        _logger.info("FuzzyRule updated: id=%s", str(fuzzy_rule.id))
        return updated

    async def delete(self, rule_id: FuzzyRuleId) -> bool:
        key = self._to_object_id(rule_id)
        res = await self._coll.delete_one({"_id": key})
        deleted = res.deleted_count > 0
        if deleted:
            _logger.info("FuzzyRule deleted: id=%s", str(rule_id))
        return deleted

    async def exists(self, rule_id: FuzzyRuleId) -> bool:
        key = self._to_object_id(rule_id)
        count = await self._coll.count_documents({"_id": key}, limit=1)
        return count > 0

    # ---------------------------- System-specific queries ----------------------------
    async def get_by_system_id(self, system_id: FuzzySystemId, skip: int = 0, limit: int = 100) -> List[FuzzyRule]:
        cursor = self._coll.find({"system_id": str(system_id)}, projection={"_id": 1, "name": 1, "system_id": 1, "description": 1, "conditions": 1, "connectors": 1, "consequents": 1, "created_at": 1}).skip(int(skip)).limit(int(limit)).sort("created_at", -1)
        return [self._doc_to_entity(d) async for d in cursor]

    async def get_by_name(self, system_id: FuzzySystemId, name: str) -> Optional[FuzzyRule]:
        doc = await self._coll.find_one({"system_id": str(system_id), "name": name}, projection={"_id": 1, "name": 1, "system_id": 1, "description": 1, "conditions": 1, "connectors": 1, "consequents": 1, "created_at": 1})
        return self._doc_to_entity(doc) if doc else None

    # ---------------------------- Conditions/connectors queries ----------------------------
    async def get_rules_using_variable(self, variable_id: FuzzyVariableId, skip: int = 0, limit: int = 100) -> List[FuzzyRule]:
        cursor = self._coll.find({"conditions.variableId": str(variable_id)}, projection={"_id": 1, "name": 1, "system_id": 1, "conditions": 1, "connectors": 1, "consequents": 1, "created_at": 1}).skip(int(skip)).limit(int(limit))
        return [self._doc_to_entity(d) async for d in cursor]

    async def get_rules_with_connector(self, connector: RuleConnector, skip: int = 0, limit: int = 100) -> List[FuzzyRule]:
        conn_value = connector.value if hasattr(connector, "value") else str(connector)
        cursor = self._coll.find({"connectors": conn_value}, projection={"_id": 1, "name": 1, "system_id": 1, "conditions": 1, "connectors": 1, "consequents": 1, "created_at": 1}).skip(int(skip)).limit(int(limit))
        return [self._doc_to_entity(d) async for d in cursor]

    # ---------------------------- Search & filter ----------------------------
    async def search_by_name(self, system_id: FuzzySystemId, name_pattern: str, skip: int = 0, limit: int = 100) -> List[FuzzyRule]:
        cursor = self._coll.find({"$and": [{"system_id": str(system_id)}, {"$text": {"$search": name_pattern}}]}, projection={"_id": 1, "name": 1, "system_id": 1, "description": 1, "conditions": 1, "connectors": 1, "consequents": 1, "created_at": 1}).skip(int(skip)).limit(int(limit))
        return [self._doc_to_entity(d) async for d in cursor]

    async def filter_rules(self, filters: Dict[str, Any], skip: int = 0, limit: int = 100) -> List[FuzzyRule]:
        query: Dict[str, Any] = {}
        if (sid := filters.get("system_id")):
            query["system_id"] = str(sid)
        if (name_contains := filters.get("name_contains")):
            query["$text"] = {"$search": str(name_contains)}
        if (uses_connector := filters.get("uses_connector")):
            query["connectors"] = str(uses_connector)
        if (has_consequent := filters.get("has_consequent")) is not None:
            query["consequents"] = {"$ne": []} if bool(has_consequent) else []
        if (variable_id := filters.get("variable_id")):
            query["conditions.variableId"] = str(variable_id)
        if (created_from := filters.get("created_from")) or (created_to := filters.get("created_to")):
            dr: Dict[str, Any] = {}
            if created_from:
                dr["$gte"] = created_from
            if created_to:
                dr["$lte"] = created_to
            query["created_at"] = dr
        # Clean possible empty array value set for consequents when has_consequent=False
        if query.get("consequents") == []:
            query["consequents"] = []
        cursor = self._coll.find(query, projection={"_id": 1, "name": 1, "system_id": 1, "description": 1, "conditions": 1, "connectors": 1, "consequents": 1, "created_at": 1}).skip(int(skip)).limit(int(limit))
        return [self._doc_to_entity(d) async for d in cursor]



    async def get_all_rules_name_description(self, skip: int = 0, limit: int = 100) -> list[dict[str, Any]]:
        cursor = (
            self._coll.find({}, projection={"_id": 1, "name": 1, "description": 1})
            .skip(skip)
            .limit(limit)
        )
        docs = await cursor.to_list(length=limit)

        # Extrae número de "Regla X" (soporta "Regla 3A", "Regla 3B", etc.)
        def extract_rule_number(name: str) -> tuple[int, str]:
            match = re.search(r"Regla\s+(\d+)([A-Z]?)", name or "", re.IGNORECASE)
            if not match:
                return (9999, "")  # las que no tengan número van al final
            num = int(match.group(1))
            suffix = match.group(2)
            return (num, suffix)

        # Ordena por número, y luego por sufijo (3A < 3B)
        sorted_docs = sorted(docs, key=lambda d: extract_rule_number(d.get("name", "")))

        return [
            {
                "id": str(d["_id"]) if "_id" in d else None,
                "name": d.get("name"),
                "description": d.get("description"),
            }
            for d in sorted_docs
        ]



    # ---------------------------- Counting & date ----------------------------
    async def count_by_system(self, system_id: FuzzySystemId) -> int:
        return int(await self._coll.count_documents({"system_id": str(system_id)}))

    async def count_total(self) -> int:
        return int(await self._coll.count_documents({}))

    async def get_by_date_range(self, start_date: datetime, end_date: datetime, skip: int = 0, limit: int = 100) -> List[FuzzyRule]:
        query = {"created_at": {"$gte": start_date, "$lte": end_date}}
        cursor = self._coll.find(query, projection={"_id": 1, "name": 1, "system_id": 1, "description": 1, "conditions": 1, "connectors": 1, "consequents": 1, "created_at": 1}).skip(int(skip)).limit(int(limit))
        return [self._doc_to_entity(d) async for d in cursor]
