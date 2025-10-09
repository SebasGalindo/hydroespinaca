from __future__ import annotations

import logging
from datetime import datetime, timezone
from typing import Any, Dict, List, Optional

from bson import ObjectId
from pymongo.errors import DuplicateKeyError

from FuzzyService.Domain.Entities.fuzzy_variable import FuzzyVariable
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyVariableId, FuzzySystemId, FuzzyTermId
from FuzzyService.Domain.Enums.EntityStatus import FuzzyVariableType
from FuzzyService.Domain.Errors.DomainErrors import DuplicateEntityError, EntityNotFoundError, ValidationError
from FuzzyService.Infrastructure.Configuration.DatabaseConfiguration import get_collection

_logger = logging.getLogger(__name__)


class FuzzyVariableRepository(IFuzzyVariableRepository):
    """MongoDB-based repository for FuzzyVariable entities (async PyMongo)."""

    def __init__(self, collection) -> None:
        self._coll = collection  # AsyncCollection
        # For cross-collection queries (systems, terms)
        self._systems = get_collection("systems")
        self._terms = get_collection("terms")

    # ---------------------------- Indexes ----------------------------
    async def ensure_indexes(self) -> None:
        try:
            # Unique variable name (global). If later system-scoped uniqueness is needed, change to compound index
            await self._coll.create_index([("name", 1)], unique=True, name="uq_fuzzy_variable_name")
            await self._coll.create_index([("variable_type", 1)], name="ix_variable_type")
            await self._coll.create_index([("reference_id", 1)], name="ix_reference_id")
            await self._coll.create_index([("created_at", -1)], name="ix_created_at_desc")
            await self._coll.create_index([("terms", 1)], name="ix_terms_array")
            # Text index for efficient name searches
            await self._coll.create_index([("name", "text")], name="ix_text_name")
            _logger.info("FuzzyVariable indexes ensured.")
        except Exception:
            _logger.exception("Failed ensuring indexes for FuzzyVariable")
            raise

    # ---------------------------- Mappers ----------------------------
    @staticmethod
    def _to_object_id(id_value: Optional[FuzzyVariableId | FuzzySystemId | FuzzyTermId]) -> Optional[Any]:
        if id_value is None:
            return None
        s = str(id_value)
        try:
            return ObjectId(s)
        except Exception:
            return s

    @staticmethod
    def _to_domain_var_id(db_id: Any) -> FuzzyVariableId:
        return FuzzyVariableId(str(db_id))

    # ---------------------------- Helpers ----------------------------
    async def _get_system_reference_flags(self, variable_id: FuzzyVariableId) -> Dict[str, bool]:
        """Devuelve flags indicando si la variable está referenciada como input y/o output en algún sistema."""
        vid = str(variable_id)
        in_inputs = (await self._systems.count_documents({"input_variable_ids": vid}, limit=1)) > 0
        in_outputs = (await self._systems.count_documents({"output_variable_ids": vid}, limit=1)) > 0
        return {"in_inputs": in_inputs, "in_outputs": in_outputs}

    @staticmethod
    def _entity_to_doc(v: FuzzyVariable) -> Dict[str, Any]:
        now_utc = datetime.now(timezone.utc)
        created_at = v.created_at or now_utc
        updated_at = v.updated_at or now_utc
        doc: Dict[str, Any] = {
            "_id": FuzzyVariableRepository._to_object_id(v.id) or ObjectId(),
            "name": v.name,
            "description": v.description,
            # Persist lower-case strings for compatibility with entity
            "variable_type": v.variable_type.value if isinstance(v.variable_type, FuzzyVariableType) else str(v.variable_type),
            "reference_id": v.reference_id,
            "terms": [str(t) for t in v.terms],
            "created_at": created_at,
            "updated_at": updated_at,
        }
        # Incluir actuator_type solo si está definido
        if v.actuator_type is not None:
            doc["actuator_type"] = v.actuator_type
        # Incluir campos de defuzzificación para outputs
        if v.universe_min is not None:
            doc["universe_min"] = v.universe_min
        if v.universe_max is not None:
            doc["universe_max"] = v.universe_max
        # Siempre incluir defuzzification_threshold (tiene default 50.0)
        doc["defuzzification_threshold"] = v.defuzzification_threshold
        return doc

    @staticmethod
    def _doc_to_entity(doc: Dict[str, Any]) -> FuzzyVariable:
        variable_type = doc.get("variable_type", "input")
        actuator_type = doc.get("actuator_type")

        # Migración automática: Si es OUTPUT sin actuator_type, inferir por defecto
        if variable_type == "output" and not actuator_type:
            # Inferir tipo según el nombre o usar PWM por defecto
            var_name = doc.get("name", "").lower()
            if "control" in var_name or "switch" in var_name:
                actuator_type = "DIGITAL"
                _logger.warning(
                    f"Variable OUTPUT '{doc.get('name')}' sin actuator_type. "
                    f"Usando 'DIGITAL' por defecto (migración automática)"
                )
            else:
                actuator_type = "PWM"
                _logger.warning(
                    f"Variable OUTPUT '{doc.get('name')}' sin actuator_type. "
                    f"Usando 'PWM' por defecto (migración automática)"
                )

        return FuzzyVariable(
            id=FuzzyVariableRepository._to_domain_var_id(doc.get("_id")),
            name=doc.get("name", ""),
            description=doc.get("description", ""),
            variable_type=variable_type,
            actuator_type=actuator_type,
            defuzzification_threshold=doc.get("defuzzification_threshold", 50.0),
            universe_min=doc.get("universe_min"),
            universe_max=doc.get("universe_max"),
            reference_id=doc.get("reference_id", ""),
            terms=[FuzzyTermId(str(t)) for t in (doc.get("terms") or [])],
            created_at=doc.get("created_at"),
            updated_at=doc.get("updated_at"),
        )

    # ---------------------------- CRUD ----------------------------
    async def create(self, fuzzy_variable: FuzzyVariable) -> FuzzyVariable:
        # Extra validation: variable_type must be in enum values
        vtype = fuzzy_variable.variable_type
        vtype_value = vtype.value if isinstance(vtype, FuzzyVariableType) else str(vtype)
        if vtype_value not in ("input", "output"):
            _logger.warning("Invalid variable_type on create: %s", vtype_value)
            raise ValidationError(f"variable_type inválido: {vtype_value}")
        doc = self._entity_to_doc(fuzzy_variable)
        try:
            _logger.debug("Inserting FuzzyVariable doc: %s", {k: doc[k] for k in doc if k != "_id"})
            res = await self._coll.insert_one(doc)
        except DuplicateKeyError:
            _logger.warning("Duplicate variable name on create: %s", fuzzy_variable.name)
            raise DuplicateEntityError(f"Ya existe una variable con el nombre '{fuzzy_variable.name}'")
        db_id = res.inserted_id if getattr(res, "inserted_id", None) else doc["_id"]
        _logger.info("FuzzyVariable created with id=%s", str(db_id))
        created = await self.get_by_id(self._to_domain_var_id(db_id))
        assert created is not None
        return created

    async def get_by_id(self, variable_id: FuzzyVariableId) -> Optional[FuzzyVariable]:
        key = self._to_object_id(variable_id)
        doc = await self._coll.find_one({"_id": key})
        return self._doc_to_entity(doc) if doc else None

    async def get_by_name(self, name: str) -> Optional[FuzzyVariable]:
        doc = await self._coll.find_one({"name": name}, projection={"_id": 1, "name": 1, "variable_type": 1, "reference_id": 1, "terms": 1, "description": 1, "created_at": 1, "updated_at": 1})
        return self._doc_to_entity(doc) if doc else None

    async def get_all(self, skip: int = 0, limit: int = 100) -> List[FuzzyVariable]:
        cursor = self._coll.find({}).skip(int(skip)).limit(int(limit)).sort("created_at", -1)
        return [self._doc_to_entity(d) async for d in cursor]

    async def update(self, fuzzy_variable: FuzzyVariable) -> FuzzyVariable:
        if fuzzy_variable.id is None:
            raise ValidationError("Variable sin ID no puede actualizarse")
        # Normalize and validate variable_type
        vtype = fuzzy_variable.variable_type
        vtype_value = vtype.value if isinstance(vtype, FuzzyVariableType) else str(vtype)
        if vtype_value not in ("input", "output"):
            _logger.warning("Invalid variable_type on update: %s", vtype_value)
            raise ValidationError(f"variable_type inválido: {vtype_value}")
        key = self._to_object_id(fuzzy_variable.id)

        # Confirm existencia y validar cambio de tipo vs referencias en sistemas
        existing = await self._coll.find_one({"_id": key}, projection={"variable_type": 1})
        if not existing:
            _logger.warning("FuzzyVariable not found for update: id=%s", str(fuzzy_variable.id))
            raise EntityNotFoundError("Variable no encontrada para actualizar")
        existing_type = str(existing.get("variable_type"))
        if existing_type != vtype_value:
            flags = await self._get_system_reference_flags(fuzzy_variable.id)
            if flags["in_inputs"] and vtype_value != "input":
                raise ValidationError("No se puede cambiar variable_type a 'output' porque la variable está referenciada como input en al menos un sistema")
            if flags["in_outputs"] and vtype_value != "output":
                raise ValidationError("No se puede cambiar variable_type a 'input' porque la variable está referenciada como output en al menos un sistema")

        # Validar que los términos referenciados existan y pertenezcan a la variable
        if fuzzy_variable.terms:
            t_ids = [self._to_object_id(t) for t in fuzzy_variable.terms]
            vid_str = str(fuzzy_variable.id)
            found_terms = await self._terms.count_documents({"_id": {"$in": t_ids}, "variable_id": vid_str})
            if int(found_terms) != len(t_ids):
                raise ValidationError("Algunos términos referenciados no existen o no pertenecen a esta variable")

        update_doc = {
            "$set": {
                "name": fuzzy_variable.name,
                "description": fuzzy_variable.description,
                "variable_type": vtype_value,
                "reference_id": fuzzy_variable.reference_id,
                "terms": [str(t) for t in fuzzy_variable.terms],
                "updated_at": datetime.now(timezone.utc),
            }
        }
        res = await self._coll.update_one({"_id": key}, update_doc)
        if res.matched_count == 0:
            _logger.warning("FuzzyVariable not found for update: id=%s", str(fuzzy_variable.id))
            raise EntityNotFoundError("Variable no encontrada para actualizar")
        updated = await self.get_by_id(FuzzyVariableId(str(fuzzy_variable.id)))
        assert updated is not None
        _logger.info("FuzzyVariable updated: id=%s", str(fuzzy_variable.id))
        return updated

    async def delete(self, variable_id: FuzzyVariableId) -> bool:
        # No permitir eliminar si la variable está referenciada por sistemas
        flags = await self._get_system_reference_flags(variable_id)
        if flags["in_inputs"] or flags["in_outputs"]:
            raise ValidationError("No se puede eliminar la variable porque está asociada a uno o más sistemas")
        # No permitir eliminar si aún existen términos asociados a la variable
        if (await self._terms.count_documents({"variable_id": str(variable_id)}, limit=1)) > 0:
            raise ValidationError("No se puede eliminar la variable porque tiene términos asociados")

        key = self._to_object_id(variable_id)
        res = await self._coll.delete_one({"_id": key})
        if res.deleted_count == 0:
            _logger.warning("FuzzyVariable not found for delete: id=%s", str(variable_id))
        else:
            _logger.info("FuzzyVariable deleted: id=%s", str(variable_id))
        return res.deleted_count > 0

    async def exists(self, variable_id: FuzzyVariableId) -> bool:
        key = self._to_object_id(variable_id)
        count = await self._coll.count_documents({"_id": key}, limit=1)
        return count > 0

    # ---------------------------- System-specific queries ----------------------------
    async def get_by_system_id(self, system_id: FuzzySystemId, skip: int = 0, limit: int = 100) -> List[FuzzyVariable]:
        sid = str(system_id)
        sys_doc = await self._systems.find_one({"_id": self._to_object_id(system_id)})
        if not sys_doc:
            raise EntityNotFoundError("Sistema no encontrado")
        var_ids = [self._to_object_id(v) for v in (sys_doc.get("input_variable_ids", []) + sys_doc.get("output_variable_ids", []))]
        cursor = self._coll.find({"_id": {"$in": var_ids}}).skip(int(skip)).limit(int(limit))
        return [self._doc_to_entity(d) async for d in cursor]

    async def get_by_type(self, variable_type: FuzzyVariableType, skip: int = 0, limit: int = 100) -> List[FuzzyVariable]:
        vtype_value = variable_type.value if isinstance(variable_type, FuzzyVariableType) else str(variable_type)
        cursor = self._coll.find({"variable_type": vtype_value}).skip(int(skip)).limit(int(limit))
        return [self._doc_to_entity(d) async for d in cursor]

    async def get_input_variables_by_system(self, system_id: FuzzySystemId) -> List[FuzzyVariable]:
        sys_doc = await self._systems.find_one({"_id": self._to_object_id(system_id)})
        if not sys_doc:
            raise EntityNotFoundError("Sistema no encontrado")
        ids = [self._to_object_id(v) for v in (sys_doc.get("input_variable_ids") or [])]
        if not ids:
            return []
        cursor = self._coll.find({"_id": {"$in": ids}})
        return [self._doc_to_entity(d) async for d in cursor]

    async def get_output_variables_by_system(self, system_id: FuzzySystemId) -> List[FuzzyVariable]:
        sys_doc = await self._systems.find_one({"_id": self._to_object_id(system_id)})
        if not sys_doc:
            raise EntityNotFoundError("Sistema no encontrado")
        ids = [self._to_object_id(v) for v in (sys_doc.get("output_variable_ids") or [])]
        if not ids:
            return []
        cursor = self._coll.find({"_id": {"$in": ids}})
        return [self._doc_to_entity(d) async for d in cursor]

    # ---------------------------- Term-related queries ----------------------------
    async def get_variables_with_term(self, term_id: FuzzyTermId) -> List[FuzzyVariable]:
        tid = str(term_id)
        cursor = self._coll.find({"terms": tid})
        return [self._doc_to_entity(d) async for d in cursor]

    async def get_variables_with_term_count(self, min_terms: int = 0, max_terms: Optional[int] = None) -> List[FuzzyVariable]:
        expr = {"$size": "$terms"}
        query: Dict[str, Any] = {"$expr": {"$gte": [expr, int(min_terms)]}}
        if max_terms is not None:
            query = {"$and": [query, {"$expr": {"$lte": [expr, int(max_terms)]}}]}
        _logger.warning("Query using $size/$expr on 'terms' may be expensive on large collections.")
        cursor = self._coll.find(query)
        return [self._doc_to_entity(d) async for d in cursor]

    # ---------------------------- Search and filtering ----------------------------
    async def search_by_name(self, name_pattern: str, skip: int = 0, limit: int = 100) -> List[FuzzyVariable]:
        cursor = self._coll.find({"$text": {"$search": name_pattern}}).skip(int(skip)).limit(int(limit))
        return [self._doc_to_entity(d) async for d in cursor]

    async def filter_variables(self, filters: Dict[str, Any], skip: int = 0, limit: int = 100) -> List[FuzzyVariable]:
        query: Dict[str, Any] = {}
        if (vtype := filters.get("variable_type")):
            query["variable_type"] = vtype.value if isinstance(vtype, FuzzyVariableType) else str(vtype)
        if (name_contains := filters.get("name_contains")):
            query["$text"] = {"$search": name_contains}
        if (term_id := filters.get("term_id")):
            query["terms"] = str(term_id)
        if (reference_id := filters.get("reference_id")):
            query["reference_id"] = reference_id
        cursor = self._coll.find(query).skip(int(skip)).limit(int(limit))
        return [self._doc_to_entity(d) async for d in cursor]

    async def get_by_date_range(self, start_date: datetime, end_date: datetime,
                               skip: int = 0, limit: int = 100) -> List[FuzzyVariable]:
        cursor = self._coll.find({
            "created_at": {"$gte": start_date, "$lte": end_date}
        }).skip(int(skip)).limit(int(limit)).sort("created_at", 1)
        return [self._doc_to_entity(d) async for d in cursor]

    # ---------------------------- Counting ----------------------------
    async def count_by_system(self, system_id: FuzzySystemId) -> int:
        sys_doc = await self._systems.find_one({"_id": self._to_object_id(system_id)})
        if not sys_doc:
            return 0
        var_ids = [self._to_object_id(v) for v in (sys_doc.get("input_variable_ids", []) + sys_doc.get("output_variable_ids", []))]
        if not var_ids:
            return 0
        return await self._coll.count_documents({"_id": {"$in": var_ids}})

    async def count_by_type(self, variable_type: FuzzyVariableType) -> int:
        vtype_value = variable_type.value if isinstance(variable_type, FuzzyVariableType) else str(variable_type)
        return await self._coll.count_documents({"variable_type": vtype_value})

    async def count_total(self) -> int:
        return await self._coll.count_documents({})
