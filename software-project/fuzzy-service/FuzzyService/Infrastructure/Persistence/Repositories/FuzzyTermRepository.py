from __future__ import annotations

import logging
from datetime import datetime, timezone
from typing import Any, Dict, List, Optional

from bson import ObjectId
from pymongo.errors import DuplicateKeyError

from FuzzyService.Domain.Entities.fuzzy_term import FuzzyTerm
from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyTermId, FuzzyVariableId
from FuzzyService.Domain.ValueObjects.MembershipFunction import MembershipFunction
from FuzzyService.Domain.Errors.DomainErrors import DuplicateEntityError, EntityNotFoundError, ValidationError
from FuzzyService.Infrastructure.Configuration.DatabaseConfiguration import get_collection

_logger = logging.getLogger(__name__)


class FuzzyTermRepository(IFuzzyTermRepository):
    """MongoDB-based repository for FuzzyTerm entities (async PyMongo)."""

    def __init__(self, collection) -> None:
        self._coll = collection  # AsyncCollection
        self._variables = get_collection("variables")

    # ---------------------------- Indexes ----------------------------
    async def ensure_indexes(self) -> None:
        try:
            # Unique label per variable
            await self._coll.create_index([("variable_id", 1), ("label", 1)], unique=True, name="uq_variable_label")
            await self._coll.create_index([("variable_id", 1)], name="ix_variable_id")
            await self._coll.create_index([("created_at", -1)], name="ix_created_at_desc")
            # Text index for label searches
            await self._coll.create_index([("label", "text")], name="ix_text_label")
            _logger.info("FuzzyTerm indexes ensured.")
        except Exception:
            _logger.exception("Failed ensuring indexes for FuzzyTerm")
            raise

    # ---------------------------- Mappers ----------------------------
    @staticmethod
    def _to_object_id(id_value: Optional[FuzzyTermId | FuzzyVariableId]) -> Optional[Any]:
        if id_value is None:
            return None
        s = str(id_value)
        try:
            return ObjectId(s)
        except Exception:
            return s

    @staticmethod
    def _to_domain_term_id(db_id: Any) -> FuzzyTermId:
        return FuzzyTermId(str(db_id))

    @staticmethod
    def _entity_to_doc(t: FuzzyTerm) -> Dict[str, Any]:
        now_utc = datetime.now(timezone.utc)
        created_at = t.created_at or now_utc
        updated_at = t.updated_at or now_utc
        doc: Dict[str, Any] = {
            "_id": FuzzyTermRepository._to_object_id(t.id) or ObjectId(),
            "variable_id": str(t.variable_id) if t.variable_id is not None else None,
            "label": t.label,
            "membership_function": t.membership_function.to_dict(),
            "created_at": created_at,
            "updated_at": updated_at,
        }
        return doc

    @staticmethod
    def _doc_to_entity(doc: Dict[str, Any]) -> FuzzyTerm:
        return FuzzyTerm(
            id=FuzzyTermRepository._to_domain_term_id(doc.get("_id")),
            variable_id=FuzzyVariableId(str(doc.get("variable_id"))) if doc.get("variable_id") else None,
            label=doc.get("label", ""),
            membership_function=MembershipFunction.from_dict(doc.get("membership_function") or {}),
            created_at=doc.get("created_at"),
            updated_at=doc.get("updated_at"),
        )

    # ---------------------------- Helpers ----------------------------
    async def _assert_variable_exists(self, variable_id: Optional[FuzzyVariableId]) -> None:
        if variable_id is None:
            raise ValidationError("El término debe estar asociado a una variable (variable_id requerido)")
        vid = str(variable_id)
        exists = await self._variables.count_documents({"_id": self._to_object_id(variable_id)}, limit=1)
        if exists == 0:
            raise EntityNotFoundError(f"Variable referida no existe: {vid}")

    # ---------------------------- CRUD ----------------------------
    async def create(self, fuzzy_term: FuzzyTerm) -> FuzzyTerm:
        await self._assert_variable_exists(fuzzy_term.variable_id)
        doc = self._entity_to_doc(fuzzy_term)
        try:
            _logger.debug("Inserting FuzzyTerm doc: %s", {k: doc[k] for k in doc if k != "_id"})
            res = await self._coll.insert_one(doc)
        except DuplicateKeyError:
            _logger.warning("Duplicate term label on create: %s for variable %s", fuzzy_term.label, str(fuzzy_term.variable_id))
            raise DuplicateEntityError(
                f"Ya existe un término con la etiqueta '{fuzzy_term.label}' para la variable {fuzzy_term.variable_id}"
            )
        db_id = res.inserted_id if getattr(res, "inserted_id", None) else doc["_id"]
        _logger.info("FuzzyTerm created with id=%s", str(db_id))
        created = await self.get_by_id(self._to_domain_term_id(db_id))
        assert created is not None
        return created

    async def get_by_id(self, term_id: FuzzyTermId) -> Optional[FuzzyTerm]:
        key = self._to_object_id(term_id)
        doc = await self._coll.find_one({"_id": key})
        return self._doc_to_entity(doc) if doc else None

    async def get_all(self, skip: int = 0, limit: int = 100) -> List[FuzzyTerm]:
        cursor = self._coll.find({}).skip(int(skip)).limit(int(limit)).sort("created_at", -1)
        return [self._doc_to_entity(d) async for d in cursor]

    async def update(self, fuzzy_term: FuzzyTerm) -> FuzzyTerm:
        if fuzzy_term.id is None:
            raise ValidationError("Término sin ID no puede actualizarse")
        await self._assert_variable_exists(fuzzy_term.variable_id)
        # Enforce unique label per variable on update
        existing = await self._coll.find_one({"_id": self._to_object_id(fuzzy_term.id)}, projection={"_id": 1, "variable_id": 1})
        if not existing:
            raise EntityNotFoundError("Término no encontrado para actualizar")
        # Validar duplicados
        duplicate = await self._coll.find_one({
            "variable_id": self._to_object_id(fuzzy_term.variable_id),
            "label": fuzzy_term.label,
            "_id": {"$ne": self._to_object_id(fuzzy_term.id)}
        })
        if duplicate:
            raise DuplicateEntityError(f"Ya existe un término con la etiqueta '{fuzzy_term.label}' en esta variable")

        key = self._to_object_id(fuzzy_term.id)
        update_doc = {
            "$set": {
                "variable_id": str(fuzzy_term.variable_id) if fuzzy_term.variable_id else None,
                "label": fuzzy_term.label,
                "membership_function": fuzzy_term.membership_function.to_dict(),
                "updated_at": datetime.now(timezone.utc),
            }
        }
        res = await self._coll.update_one({"_id": key}, update_doc)
        if res.matched_count == 0:
            _logger.warning("FuzzyTerm not found for update: id=%s", str(fuzzy_term.id))
            raise EntityNotFoundError("Término no encontrado para actualizar")
        updated = await self.get_by_id(FuzzyTermId(str(fuzzy_term.id)))
        assert updated is not None
        _logger.info("FuzzyTerm updated: id=%s", str(fuzzy_term.id))
        return updated

    async def _is_term_in_variable_terms_array(self, term_id: FuzzyTermId) -> bool:
        """Verifica si el término está siendo referenciado en el array de términos de alguna variable."""
        from FuzzyService.Infrastructure.Configuration.DatabaseConfiguration import get_collection
        
        variables_coll = get_collection("fuzzy_variables")
        term_object_id = self._to_object_id(term_id)
        
        # Buscar si alguna variable tiene este término en su array de términos
        result = await variables_coll.find_one({"terms": term_object_id})
        return result is not None

    async def delete(self, term_id: FuzzyTermId) -> bool:
        # Impedir eliminar si la variable aún referencia el término en su arreglo 'terms'
        if await self._is_term_in_variable_terms_array(term_id):
            raise ValidationError("No se puede eliminar el término porque la variable aún lo referencia en su lista de términos")
        res = await self._coll.delete_one({"_id": self._to_object_id(term_id)})
        if res.deleted_count == 0:
            _logger.warning("FuzzyTerm not found for delete: id=%s", str(term_id))
        else:
            _logger.info("FuzzyTerm deleted: id=%s", str(term_id))
        return res.deleted_count > 0

    async def exists(self, term_id: FuzzyTermId) -> bool:
        key = self._to_object_id(term_id)
        count = await self._coll.count_documents({"_id": key}, limit=1)
        return count > 0

    # ---------------------------- Variable-specific queries ----------------------------
    async def get_by_variable_id(self, variable_id: FuzzyVariableId, skip: int = 0, limit: int = 100) -> List[FuzzyTerm]:
        vid = str(variable_id)
        cursor = self._coll.find({"variable_id": vid}, projection={"_id": 1, "variable_id": 1, "label": 1, "membership_function": 1, "created_at": 1, "updated_at": 1}).skip(int(skip)).limit(int(limit))
        return [self._doc_to_entity(d) async for d in cursor]

    async def get_by_label(self, variable_id: FuzzyVariableId, label: str) -> Optional[FuzzyTerm]:
        vid = str(variable_id)
        doc = await self._coll.find_one({"variable_id": vid, "label": label}, projection={"_id": 1, "variable_id": 1, "label": 1, "membership_function": 1, "created_at": 1, "updated_at": 1})
        return self._doc_to_entity(doc) if doc else None

    # ---------------------------- Search and filtering ----------------------------
    async def search_by_label(self, variable_id: FuzzyVariableId, label_pattern: str, skip: int = 0, limit: int = 100) -> List[FuzzyTerm]:
        vid = str(variable_id)
        cursor = self._coll.find({"variable_id": vid, "$text": {"$search": label_pattern}}, projection={"_id": 1, "variable_id": 1, "label": 1, "membership_function": 1, "created_at": 1, "updated_at": 1}).skip(int(skip)).limit(int(limit))
        return [self._doc_to_entity(d) async for d in cursor]

    async def filter_terms(self, filters: Dict[str, Any], skip: int = 0, limit: int = 100) -> List[FuzzyTerm]:
        query: Dict[str, Any] = {}
        if (vid := filters.get("variable_id")):
            query["variable_id"] = str(vid)
        if (label_contains := filters.get("label_contains")):
            query["$text"] = {"$search": label_contains}
        cursor = self._coll.find(query, projection={"_id": 1, "variable_id": 1, "label": 1, "membership_function": 1, "created_at": 1, "updated_at": 1}).skip(int(skip)).limit(int(limit))
        return [self._doc_to_entity(d) async for d in cursor]

    # ---------------------------- Counting ----------------------------
    async def count_by_variable(self, variable_id: FuzzyVariableId) -> int:
        return await self._coll.count_documents({"variable_id": str(variable_id)})

    async def count_total(self) -> int:
        return await self._coll.count_documents({})
