"""Implementaciones de repositorios MongoDB.

Sigue el patrón de auth-service y sensor-service para mantener
consistencia arquitectónica.
"""

from __future__ import annotations

import logging
from datetime import datetime, timezone
from typing import Dict, List, Optional

from bson import ObjectId
from pymongo.asynchronous.collection import AsyncCollection
from pymongo.errors import DuplicateKeyError

from domain.models import ActuatorState, Routine, Variable
from infrastructure.database import mongo_db
from infrastructure.repositories import (
    ActuatorStateRepository,
    RoutineRepository,
    VariableRepository,
)


class MongoVariableRepository(VariableRepository):
    """Repositorio MongoDB para Variables."""
    
    def __init__(self):
        self._collection: Optional[AsyncCollection] = None

    async def _get_collection(self) -> AsyncCollection:
        """Obtiene la colección de variables."""
        if self._collection is None:
            self._collection = mongo_db.database["variables"]
        return self._collection
        
    async def get_all(self) -> List[Variable]:
        """Obtiene todas las variables."""
        collection = await self._get_collection()
        cursor = collection.find({})
        
        variables = []
        async for doc in cursor:
            variables.append(self._document_to_variable(doc))
            
        return variables
        
    async def get_by_id(self, variable_id: str) -> Optional[Variable]:
        """Obtiene una variable por ID."""
        collection = await self._get_collection()
        doc = await collection.find_one({"variableId": variable_id})
        
        if doc:
            return self._document_to_variable(doc)
        return None
        
    async def save(self, variable: Variable) -> Variable:
        """Guarda o actualiza una variable."""
        existing = await self.get_by_id(variable.id)
        if existing:
            return await self.update(variable)
        else:
            return await self.create(variable)
        
    async def create(self, variable: Variable) -> Variable:
        """Crea una nueva variable."""
        collection = await self._get_collection()
        
        doc = self._variable_to_document(variable)
        doc["createdAt"] = datetime.now(timezone.utc)
        doc["updatedAt"] = datetime.now(timezone.utc)
        
        try:
            result = await collection.insert_one(doc)
            doc["_id"] = result.inserted_id
            return self._document_to_variable(doc)
            
        except DuplicateKeyError:
            raise ValueError(f"Variable con ID {variable.id} ya existe")
            
    async def update(self, variable: Variable) -> Variable:
        """Actualiza una variable existente."""
        collection = await self._get_collection()
        
        doc = self._variable_to_document(variable)
        doc["updatedAt"] = datetime.now(timezone.utc)
        
        result = await collection.update_one(
            {"variableId": variable.id},
            {"$set": doc}
        )
        
        if result.matched_count == 0:
            raise ValueError(f"Variable con ID {variable.id} no encontrada")
            
        return variable
        
    async def delete(self, variable_id: str) -> bool:
        """Elimina una variable."""
        collection = await self._get_collection()
        result = await collection.delete_one({"variableId": variable_id})
        return result.deleted_count > 0
    
    async def exists(self, variable_id: str) -> bool:
        """Verifica si existe una variable."""
        collection = await self._get_collection()
        count = await collection.count_documents({"variableId": variable_id})
        return count > 0
    
    async def list_all(self) -> List[Variable]:
        """Lista todas las variables."""
        collection = await self._get_collection()
        cursor = collection.find({})
        documents = await cursor.to_list(length=None)
        return [self._document_to_variable(doc) for doc in documents]
        
    def _variable_to_document(self, variable: Variable) -> Dict:
        """Convierte Variable a documento MongoDB."""
        return {
            "variableId": variable.id,
            "name": variable.name,
            "unit": variable.unit,
            "minValue": variable.min_value,
            "maxValue": variable.max_value,
            "description": variable.description,
            "fuzzySets": [{
                "name": fs.name,
                "membershipType": fs.membership_type.value,
                "parameters": fs.parameters,
                "description": fs.description
            } for fs in variable.fuzzy_sets]
        }
        
    def _document_to_variable(self, doc: Dict) -> Variable:
        """Convierte documento MongoDB a Variable."""
        from domain.models import FuzzySet, MembershipFunctionType
        
        fuzzy_sets = []
        for fs_doc in doc.get("fuzzySets", []):
            fuzzy_set = FuzzySet(
                name=fs_doc["name"],
                membership_type=MembershipFunctionType(fs_doc["membershipType"]),
                parameters=fs_doc["parameters"],
                description=fs_doc.get("description")
            )
            fuzzy_sets.append(fuzzy_set)
        
        return Variable(
            id=doc["variableId"],
            name=doc["name"],
            unit=doc.get("unit"),
            min_value=doc.get("minValue"),
            max_value=doc.get("maxValue"),
            description=doc.get("description"),
            fuzzy_sets=fuzzy_sets
        )


class MongoRoutineRepository(RoutineRepository):
    """Repositorio MongoDB para Routines."""
    
    def __init__(self):
        self._collection: Optional[AsyncCollection] = None

    async def _get_collection(self) -> AsyncCollection:
        """Obtiene la colección de rutinas."""
        if self._collection is None:
            self._collection = mongo_db.database["routines"]
        return self._collection
        
    async def get_all(self) -> List[Routine]:
        """Obtiene todas las rutinas."""
        collection = await self._get_collection()
        cursor = collection.find({})
        
        routines = []
        async for doc in cursor:
            routines.append(self._document_to_routine(doc))
            
        return routines
        
    async def get_active(self) -> List[Routine]:
        """Obtiene rutinas activas."""
        collection = await self._get_collection()
        cursor = collection.find({"active": True})
        
        routines = []
        async for doc in cursor:
            routines.append(self._document_to_routine(doc))
            
        return routines
        
    async def get_by_id(self, routine_id: str) -> Optional[Routine]:
        """Obtiene una rutina por ID."""
        collection = await self._get_collection()
        doc = await collection.find_one({"routineId": routine_id})
        
        if doc:
            return self._document_to_routine(doc)
        return None
        
    async def save(self, routine: Routine) -> Routine:
        """Guarda o actualiza una rutina."""
        existing = await self.get_by_id(routine.id)
        if existing:
            return await self.update(routine)
        else:
            return await self.create(routine)
        
    async def create(self, routine: Routine) -> Routine:
        """Crea una nueva rutina."""
        collection = await self._get_collection()
        
        doc = self._routine_to_document(routine)
        doc["createdAt"] = datetime.now(timezone.utc)
        doc["updatedAt"] = datetime.now(timezone.utc)
        
        try:
            result = await collection.insert_one(doc)
            doc["_id"] = result.inserted_id
            return self._document_to_routine(doc)
            
        except DuplicateKeyError:
            raise ValueError(f"Rutina con ID {routine.id} ya existe")
            
    async def update(self, routine: Routine) -> Routine:
        """Actualiza una rutina existente."""
        collection = await self._get_collection()
        
        doc = self._routine_to_document(routine)
        doc["updatedAt"] = datetime.now(timezone.utc)
        
        result = await collection.update_one(
            {"routineId": routine.id},
            {"$set": doc}
        )
        
        if result.matched_count == 0:
            raise ValueError(f"Rutina con ID {routine.id} no encontrada")
            
        return routine
        
    async def delete(self, routine_id: str) -> bool:
        """Elimina una rutina."""
        collection = await self._get_collection()
        result = await collection.delete_one({"routineId": routine_id})
        return result.deleted_count > 0
    
    async def exists(self, routine_id: str) -> bool:
        """Verifica si existe una rutina."""
        collection = await self._get_collection()
        count = await collection.count_documents({"routineId": routine_id})
        return count > 0
    
    async def list_all(self) -> List[Routine]:
        """Lista todas las rutinas."""
        collection = await self._get_collection()
        cursor = collection.find({})
        documents = await cursor.to_list(length=None)
        return [self._document_to_routine(doc) for doc in documents]
    
    async def list_active(self) -> List[Routine]:
        """Lista todas las rutinas activas."""
        collection = await self._get_collection()
        cursor = collection.find({"active": True})
        documents = await cursor.to_list(length=None)
        return [self._document_to_routine(doc) for doc in documents]
        
    def _routine_to_document(self, routine: Routine) -> Dict:
        """Convierte Routine a documento MongoDB."""
        return {
            "routineId": routine.id,
            "name": routine.name,
            "description": routine.description,
            "active": routine.active,
            "fuzzyRules": [{
                "id": rule.id,
                "name": rule.name,
                "description": rule.description,
                "conditions": [{
                    "variableId": cond.variable_id,
                    "fuzzySetName": cond.fuzzy_set_name,
                    "weight": cond.weight
                } for cond in rule.conditions],
                "operator": rule.operator.value,
                "outputVariableId": rule.output_variable_id,
                "outputFuzzySetName": rule.output_fuzzy_set_name,
                "priority": rule.priority,
                "active": rule.active
            } for rule in routine.fuzzy_rules],
            "thresholdRules": [rule.model_dump() for rule in routine.threshold_rules],
            "outputs": [{
                "output_name": output.output_name,
                "actuatorId": output.actuatorId,
                "esp32Id": output.esp32Id,
                "actuator_type": output.actuator_type,
                "on_threshold": output.on_threshold,
                "created_at": output.created_at,
                "updated_at": output.updated_at
            } for output in routine.outputs],
            "controlParameters": routine.control_parameters.model_dump(),
            "inputVariables": routine.input_variables,
            "outputVariables": routine.output_variables
        }
        
    def _document_to_routine(self, doc: Dict) -> Routine:
        """Convierte documento MongoDB a Routine."""
        from domain.models import (
            FuzzyRule, FuzzyCondition, LogicalOperator, 
            ThresholdRule, ActuatorMapping, ControlParameters
        )
        
        # Deserializar reglas difusas
        fuzzy_rules = []
        for rule_doc in doc.get("fuzzyRules", []):
            conditions = [
                FuzzyCondition(
                    variable_id=cond["variableId"],
                    fuzzy_set_name=cond["fuzzySetName"],
                    weight=cond.get("weight", 1.0)
                ) for cond in rule_doc["conditions"]
            ]
            
            fuzzy_rule = FuzzyRule(
                id=rule_doc["id"],
                name=rule_doc["name"],
                description=rule_doc.get("description"),
                conditions=conditions,
                operator=LogicalOperator(rule_doc["operator"]),
                output_variable_id=rule_doc["outputVariableId"],
                output_fuzzy_set_name=rule_doc["outputFuzzySetName"],
                priority=rule_doc.get("priority", 1),
                active=rule_doc.get("active", True)
            )
            fuzzy_rules.append(fuzzy_rule)
        
        # Deserializar reglas de umbral
        threshold_rules = [
            ThresholdRule(**rule_data) 
            for rule_data in doc.get("thresholdRules", [])
        ]
        
        # Deserializar mapeos de salida
        outputs = []
        for output_data in doc.get("outputs", []):
            # Asegurar compatibilidad con documentos existentes
            output_dict = {
                "output_name": output_data.get("output_name", ""),
                "actuatorId": output_data.get("actuatorId", ""),
                "esp32Id": output_data.get("esp32Id", ""),
                "actuator_type": output_data.get("actuator_type", "variable"),
                "on_threshold": output_data.get("on_threshold", 50.0),
                "created_at": output_data.get("created_at"),
                "updated_at": output_data.get("updated_at")
            }
            outputs.append(ActuatorMapping(**output_dict))
        
        # Deserializar parámetros de control
        control_params_data = doc.get("controlParameters", {})
        control_parameters = ControlParameters(**control_params_data)
        
        return Routine(
            id=doc["routineId"],
            name=doc["name"],
            description=doc.get("description"),
            active=doc["active"],
            fuzzy_rules=fuzzy_rules,
            threshold_rules=threshold_rules,
            outputs=outputs,
            control_parameters=control_parameters,
            input_variables=doc.get("inputVariables", []),
            output_variables=doc.get("outputVariables", [])
        )


class MongoActuatorStateRepository(ActuatorStateRepository):
    """Repositorio MongoDB para ActuatorState."""
    
    def __init__(self):
        self._collection: Optional[AsyncCollection] = None

    async def _get_collection(self) -> AsyncCollection:
        """Obtiene la colección de estados de actuadores."""
        if self._collection is None:
            self._collection = mongo_db.database["actuator_states"]
        return self._collection
        
    async def get_state(self, actuator_id: str) -> Optional[ActuatorState]:
        """Obtiene el estado de un actuador por ID."""
        collection = await self._get_collection()
        doc = await collection.find_one({"actuatorId": actuator_id})
        
        if doc:
            return self._document_to_actuator_state(doc)
        return None
        
    async def save_state(self, actuator_id: str, state: ActuatorState) -> None:
        """Guarda o actualiza el estado de un actuador."""
        collection = await self._get_collection()
        
        doc = self._actuator_state_to_document(state)
        doc["updatedAt"] = datetime.now(timezone.utc)
        
        await collection.update_one(
            {"actuatorId": actuator_id},
            {"$set": doc},
            upsert=True
        )
        
    async def get_all_states(self) -> Dict[str, ActuatorState]:
        """Obtiene todos los estados de actuadores."""
        collection = await self._get_collection()
        cursor = collection.find({})
        
        states = {}
        async for doc in cursor:
            state = self._document_to_actuator_state(doc)
            states[state.actuatorId] = state
        return states
        
    def _actuator_state_to_document(self, state: ActuatorState) -> Dict:
        """Convierte ActuatorState a documento MongoDB."""
        return {
            "actuatorId": state.actuatorId,
            "lastTarget": state.last_target,
            "lastEmittedAt": state.last_emitted_at,
            "lastHoldSeconds": state.last_hold_seconds
        }
        
    def _document_to_actuator_state(self, doc: Dict) -> ActuatorState:
        """Convierte documento MongoDB a ActuatorState."""
        return ActuatorState(
            actuatorId=doc["actuatorId"],
            last_target=doc["lastTarget"],
            last_emitted_at=doc["lastEmittedAt"],
            last_hold_seconds=doc["lastHoldSeconds"]
        )