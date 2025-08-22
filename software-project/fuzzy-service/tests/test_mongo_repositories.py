"""Pruebas para repositorios MongoDB.

Este archivo contiene pruebas básicas para los repositorios de MongoDB.
"""

import pytest
from datetime import datetime

import pytest
from unittest.mock import AsyncMock, MagicMock, patch
from datetime import datetime, timezone, timedelta
from typing import Dict, List

from domain.models import (
    Variable, FuzzySet, MembershipFunctionType, Routine, FuzzyRule, 
    FuzzyCondition, LogicalOperator, ThresholdRule, ActuatorMapping, 
    ControlParameters, ActuatorType, ActuatorState
)
from infrastructure.mongo_repositories import (
    MongoVariableRepository, MongoRoutineRepository, MongoActuatorStateRepository
)
from pymongo.errors import DuplicateKeyError


class TestMongoVariableRepository:
    """Pruebas para MongoVariableRepository."""
    
    @pytest.fixture
    def mock_collection(self):
        """Mock de la colección MongoDB."""
        return AsyncMock()
    
    @pytest.fixture
    def repository(self, mock_collection):
        """Repositorio con colección mockeada."""
        repo = MongoVariableRepository()
        repo._collection = mock_collection
        return repo
    
    @pytest.fixture
    def sample_variable(self):
        """Variable de ejemplo para pruebas."""
        fuzzy_set = FuzzySet(
            name="Low",
            membership_type=MembershipFunctionType.TRIANGULAR,
            parameters=[0, 10, 20],
            description="Valor bajo"
        )
        
        variable = Variable(
            id="temp_001",
            name="Temperature",
            unit="°C",
            min_value=0.0,
            max_value=100.0,
            description="Temperatura ambiente"
        )
        variable.add_fuzzy_set(fuzzy_set)
        return variable
    
    @pytest.fixture
    def sample_document(self):
        """Documento MongoDB de ejemplo."""
        return {
            "_id": "507f1f77bcf86cd799439011",
            "variableId": "temp_001",
            "name": "Temperature",
            "unit": "°C",
            "minValue": 0.0,
            "maxValue": 100.0,
            "description": "Temperatura ambiente",
            "fuzzySets": [{
                "name": "Low",
                "membershipType": "triangular",
                "parameters": [0, 10, 20],
                "description": "Valor bajo"
            }]
        }
    
    @pytest.mark.asyncio
    async def test_get_by_id_found(self, repository, mock_collection, sample_document):
        """Prueba obtener variable por ID cuando existe."""
        mock_collection.find_one.return_value = sample_document
        
        result = await repository.get_by_id("temp_001")
        
        assert result is not None
        assert result.id == "temp_001"
        assert result.name == "Temperature"
        assert result.unit == "°C"
        assert len(result.fuzzy_sets) == 1
        assert result.fuzzy_sets[0].name == "Low"
        mock_collection.find_one.assert_called_once_with({"variableId": "temp_001"})
    
    @pytest.mark.asyncio
    async def test_get_by_id_not_found(self, repository, mock_collection):
        """Prueba obtener variable por ID cuando no existe."""
        mock_collection.find_one.return_value = None
        
        result = await repository.get_by_id("nonexistent")
        
        assert result is None
        mock_collection.find_one.assert_called_once_with({"variableId": "nonexistent"})
    
    @pytest.mark.asyncio
    async def test_create_success(self, repository, mock_collection, sample_variable):
        """Prueba crear variable exitosamente."""
        mock_result = MagicMock()
        mock_result.inserted_id = "507f1f77bcf86cd799439011"
        mock_collection.insert_one.return_value = mock_result
        
        result = await repository.create(sample_variable)
        
        assert result.id == sample_variable.id
        assert result.name == sample_variable.name
        mock_collection.insert_one.assert_called_once()
        
        # Verificar que se llamó con el documento correcto
        call_args = mock_collection.insert_one.call_args[0][0]
        assert call_args["variableId"] == "temp_001"
        assert call_args["name"] == "Temperature"
        assert "createdAt" in call_args
        assert "updatedAt" in call_args
    
    @pytest.mark.asyncio
    async def test_create_duplicate_key_error(self, repository, mock_collection, sample_variable):
        """Prueba crear variable con ID duplicado."""
        mock_collection.insert_one.side_effect = DuplicateKeyError("Duplicate key")
        
        with pytest.raises(ValueError, match="Variable con ID temp_001 ya existe"):
            await repository.create(sample_variable)
    
    @pytest.mark.asyncio
    async def test_update_success(self, repository, mock_collection, sample_variable):
        """Prueba actualizar variable exitosamente."""
        mock_result = MagicMock()
        mock_result.matched_count = 1
        mock_collection.update_one.return_value = mock_result
        
        result = await repository.update(sample_variable)
        
        assert result == sample_variable
        mock_collection.update_one.assert_called_once()
        
        # Verificar argumentos de la llamada
        call_args = mock_collection.update_one.call_args
        assert call_args[0][0] == {"variableId": "temp_001"}
        assert "$set" in call_args[0][1]
    
    @pytest.mark.asyncio
    async def test_update_not_found(self, repository, mock_collection, sample_variable):
        """Prueba actualizar variable que no existe."""
        mock_result = MagicMock()
        mock_result.matched_count = 0
        mock_collection.update_one.return_value = mock_result
        
        with pytest.raises(ValueError, match="Variable con ID temp_001 no encontrada"):
            await repository.update(sample_variable)
    
    @pytest.mark.asyncio
    async def test_delete_success(self, repository, mock_collection):
        """Prueba eliminar variable exitosamente."""
        mock_result = MagicMock()
        mock_result.deleted_count = 1
        mock_collection.delete_one.return_value = mock_result
        
        result = await repository.delete("temp_001")
        
        assert result is True
        mock_collection.delete_one.assert_called_once_with({"variableId": "temp_001"})
    
    @pytest.mark.asyncio
    async def test_delete_not_found(self, repository, mock_collection):
        """Prueba eliminar variable que no existe."""
        mock_result = MagicMock()
        mock_result.deleted_count = 0
        mock_collection.delete_one.return_value = mock_result
        
        result = await repository.delete("nonexistent")
        
        assert result is False
    
    @pytest.mark.asyncio
    async def test_exists_true(self, repository, mock_collection):
        """Prueba verificar existencia cuando la variable existe."""
        mock_collection.count_documents.return_value = 1
        
        result = await repository.exists("temp_001")
        
        assert result is True
        mock_collection.count_documents.assert_called_once_with({"variableId": "temp_001"})
    
    @pytest.mark.asyncio
    async def test_exists_false(self, repository, mock_collection):
        """Prueba verificar existencia cuando la variable no existe."""
        mock_collection.count_documents.return_value = 0
        
        result = await repository.exists("nonexistent")
        
        assert result is False
    
    # TODO: Implementar test_list_all cuando se resuelvan problemas con mocks de cursor
    # @pytest.mark.asyncio
    # async def test_list_all(self, repository, sample_document):
    #     """Prueba listar todas las variables."""
    #     pass
    
    def test_variable_to_document_conversion(self, repository, sample_variable):
        """Prueba conversión de Variable a documento MongoDB."""
        doc = repository._variable_to_document(sample_variable)
        
        assert doc["variableId"] == "temp_001"
        assert doc["name"] == "Temperature"
        assert doc["unit"] == "°C"
        assert doc["minValue"] == 0.0
        assert doc["maxValue"] == 100.0
        assert doc["description"] == "Temperatura ambiente"
        assert len(doc["fuzzySets"]) == 1
        assert doc["fuzzySets"][0]["name"] == "Low"
        assert doc["fuzzySets"][0]["membershipType"] == "triangular"
    
    def test_document_to_variable_conversion(self, repository, sample_document):
        """Prueba conversión de documento MongoDB a Variable."""
        variable = repository._document_to_variable(sample_document)
        
        assert variable.id == "temp_001"
        assert variable.name == "Temperature"
        assert variable.unit == "°C"
        assert variable.min_value == 0.0
        assert variable.max_value == 100.0
        assert variable.description == "Temperatura ambiente"
        assert len(variable.fuzzy_sets) == 1
        assert variable.fuzzy_sets[0].name == "Low"
        assert variable.fuzzy_sets[0].membership_type == MembershipFunctionType.TRIANGULAR


class TestMongoRoutineRepository:
    """Pruebas para MongoRoutineRepository."""
    
    @pytest.fixture
    def mock_collection(self):
        """Mock de la colección MongoDB."""
        return AsyncMock()
    
    @pytest.fixture
    def repository(self, mock_collection):
        """Repositorio con colección mockeada."""
        repo = MongoRoutineRepository()
        repo._collection = mock_collection
        return repo
    
    @pytest.fixture
    def sample_routine(self):
        """Rutina de ejemplo para pruebas."""
        # Crear condición difusa
        condition = FuzzyCondition(
            variable_id="temp_001",
            fuzzy_set_name="High",
            weight=1.0
        )
        
        # Crear regla difusa
        fuzzy_rule = FuzzyRule(
            id="rule_001",
            name="High Temperature Rule",
            description="Activar ventilador cuando temperatura alta",
            conditions=[condition],
            operator=LogicalOperator.AND,
            output_variable_id="fan_speed",
            output_fuzzy_set_name="High",
            priority=1,
            active=True
        )
        
        # Crear regla de umbral
        threshold_rule = ThresholdRule(
            variableId="temp_001",
            greater_than=45.0,
            output_name="emergency_stop",
            target=0,
            hold_seconds=30
        )
        
        # Crear mapeo de actuador
        actuator_mapping = ActuatorMapping(
            output_name="fan_control",
            actuatorId="fan_001",
            esp32Id="esp32_001",
            actuator_type=ActuatorType.VARIABLE,
            on_threshold=50.0
        )
        
        # Crear parámetros de control
        control_params = ControlParameters(
            hysteresis_delta=2.0,
            cooldown_seconds=10,
            defuzzification_method="centroid",
            evaluation_interval_seconds=5,
            min_confidence_threshold=0.1
        )
        
        return Routine(
            id="routine_001",
            name="Temperature Control",
            description="Control de temperatura con ventilador",
            active=True,
            fuzzy_rules=[fuzzy_rule],
            threshold_rules=[threshold_rule],
            outputs=[actuator_mapping],
            control_parameters=control_params,
            input_variables=["temp_001"],
            output_variables=["fan_speed"]
        )
    
    @pytest.fixture
    def sample_document(self):
        """Documento MongoDB de ejemplo para rutina."""
        return {
            "_id": "507f1f77bcf86cd799439012",
            "routineId": "routine_001",
            "name": "Temperature Control",
            "description": "Control de temperatura con ventilador",
            "active": True,
            "fuzzyRules": [{
                "id": "rule_001",
                "name": "High Temperature Rule",
                "description": "Activar ventilador cuando temperatura alta",
                "conditions": [{
                    "variableId": "temp_001",
                    "fuzzySetName": "High",
                    "weight": 1.0
                }],
                "operator": "and",
                "outputVariableId": "fan_speed",
                "outputFuzzySetName": "High",
                "priority": 1,
                "active": True
            }],
            "thresholdRules": [{
                "variableId": "temp_001",
                "greater_than": 45.0,
                "output_name": "emergency_stop",
                "target": 0,
                "hold_seconds": 30
            }],
            "outputs": [{
                "output_name": "fan_control",
                "actuatorId": "fan_001",
                "esp32Id": "esp32_001",
                "actuator_type": "variable",
                "on_threshold": 50.0,
                "created_at": None,
                "updated_at": None
            }],
            "controlParameters": {
                "hysteresis_delta": 2.0,
                "cooldown_seconds": 10,
                "defuzzification_method": "centroid",
                "evaluation_interval_seconds": 5,
                "min_confidence_threshold": 0.1
            },
            "inputVariables": ["temp_001"],
            "outputVariables": ["fan_speed"]
        }
    
    @pytest.mark.asyncio
    async def test_get_by_id_found(self, repository, mock_collection, sample_document):
        """Prueba obtener rutina por ID cuando existe."""
        mock_collection.find_one.return_value = sample_document
        
        result = await repository.get_by_id("routine_001")
        
        assert result is not None
        assert result.id == "routine_001"
        assert result.name == "Temperature Control"
        assert result.active is True
        assert len(result.fuzzy_rules) == 1
        assert len(result.threshold_rules) == 1
        assert len(result.outputs) == 1
        mock_collection.find_one.assert_called_once_with({"routineId": "routine_001"})
    
    @pytest.mark.asyncio
    async def test_get_by_id_not_found(self, repository, mock_collection):
        """Prueba obtener rutina por ID cuando no existe."""
        mock_collection.find_one.return_value = None
        
        result = await repository.get_by_id("nonexistent")
        
        assert result is None
        mock_collection.find_one.assert_called_once_with({"routineId": "nonexistent"})
    
    @pytest.mark.asyncio
    async def test_create_success(self, repository, mock_collection, sample_routine):
        """Prueba crear rutina exitosamente."""
        mock_result = MagicMock()
        mock_result.inserted_id = "507f1f77bcf86cd799439012"
        mock_collection.insert_one.return_value = mock_result
        
        result = await repository.create(sample_routine)
        
        assert result.id == sample_routine.id
        assert result.name == sample_routine.name
        mock_collection.insert_one.assert_called_once()
        
        # Verificar que se llamó con el documento correcto
        call_args = mock_collection.insert_one.call_args[0][0]
        assert call_args["routineId"] == "routine_001"
        assert call_args["name"] == "Temperature Control"
        assert "createdAt" in call_args
        assert "updatedAt" in call_args
    
    @pytest.mark.asyncio
    async def test_create_duplicate_key_error(self, repository, mock_collection, sample_routine):
        """Prueba crear rutina con ID duplicado."""
        mock_collection.insert_one.side_effect = DuplicateKeyError("Duplicate key")
        
        with pytest.raises(ValueError, match="Rutina con ID routine_001 ya existe"):
            await repository.create(sample_routine)
    
    @pytest.mark.asyncio
    async def test_update_success(self, repository, mock_collection, sample_routine):
        """Prueba actualizar rutina exitosamente."""
        mock_result = MagicMock()
        mock_result.matched_count = 1
        mock_collection.update_one.return_value = mock_result
        
        result = await repository.update(sample_routine)
        
        assert result == sample_routine
        mock_collection.update_one.assert_called_once()
        
        # Verificar argumentos de la llamada
        call_args = mock_collection.update_one.call_args
        assert call_args[0][0] == {"routineId": "routine_001"}
        assert "$set" in call_args[0][1]
    
    @pytest.mark.asyncio
    async def test_update_not_found(self, repository, mock_collection, sample_routine):
        """Prueba actualizar rutina que no existe."""
        mock_result = MagicMock()
        mock_result.matched_count = 0
        mock_collection.update_one.return_value = mock_result
        
        with pytest.raises(ValueError, match="Rutina con ID routine_001 no encontrada"):
            await repository.update(sample_routine)
    
    @pytest.mark.asyncio
    async def test_delete_success(self, repository, mock_collection):
        """Prueba eliminar rutina exitosamente."""
        mock_result = MagicMock()
        mock_result.deleted_count = 1
        mock_collection.delete_one.return_value = mock_result
        
        result = await repository.delete("routine_001")
        
        assert result is True
        mock_collection.delete_one.assert_called_once_with({"routineId": "routine_001"})
    
    @pytest.mark.asyncio
    async def test_delete_not_found(self, repository, mock_collection):
        """Prueba eliminar rutina que no existe."""
        mock_result = MagicMock()
        mock_result.deleted_count = 0
        mock_collection.delete_one.return_value = mock_result
        
        result = await repository.delete("nonexistent")
        
        assert result is False
    
    @pytest.mark.asyncio
    async def test_exists_true(self, repository, mock_collection):
        """Prueba verificar existencia cuando la rutina existe."""
        mock_collection.count_documents.return_value = 1
        
        result = await repository.exists("routine_001")
        
        assert result is True
        mock_collection.count_documents.assert_called_once_with({"routineId": "routine_001"})
    
    @pytest.mark.asyncio
    async def test_exists_false(self, repository, mock_collection):
        """Prueba verificar existencia cuando la rutina no existe."""
        mock_collection.count_documents.return_value = 0
        
        result = await repository.exists("nonexistent")
        
        assert result is False
    
    def test_routine_to_document_conversion(self, repository, sample_routine):
        """Prueba conversión de Routine a documento MongoDB."""
        doc = repository._routine_to_document(sample_routine)
        
        assert doc["routineId"] == "routine_001"
        assert doc["name"] == "Temperature Control"
        assert doc["description"] == "Control de temperatura con ventilador"
        assert doc["active"] is True
        assert len(doc["fuzzyRules"]) == 1
        assert len(doc["thresholdRules"]) == 1
        assert len(doc["outputs"]) == 1
        assert "controlParameters" in doc
        assert doc["inputVariables"] == ["temp_001"]
        assert doc["outputVariables"] == ["fan_speed"]
    
    def test_document_to_routine_conversion(self, repository, sample_document):
        """Prueba conversión de documento MongoDB a Routine."""
        routine = repository._document_to_routine(sample_document)
        
        assert routine.id == "routine_001"
        assert routine.name == "Temperature Control"
        assert routine.description == "Control de temperatura con ventilador"
        assert routine.active is True
        assert len(routine.fuzzy_rules) == 1
        assert len(routine.threshold_rules) == 1
        assert len(routine.outputs) == 1
        assert routine.input_variables == ["temp_001"]
        assert routine.output_variables == ["fan_speed"]
        
        # Verificar regla difusa
        fuzzy_rule = routine.fuzzy_rules[0]
        assert fuzzy_rule.id == "rule_001"
        assert fuzzy_rule.name == "High Temperature Rule"
        assert len(fuzzy_rule.conditions) == 1
        assert fuzzy_rule.operator == LogicalOperator.AND
        
        # Verificar regla de umbral
        threshold_rule = routine.threshold_rules[0]
        assert threshold_rule.variableId == "temp_001"
        assert threshold_rule.greater_than == 45.0
        assert threshold_rule.output_name == "emergency_stop"
        
        # Verificar mapeo de actuador
        output = routine.outputs[0]
        assert output.output_name == "fan_control"
        assert output.actuatorId == "fan_001"


class TestMongoActuatorStateRepository:
    """Pruebas para MongoActuatorStateRepository."""
    
    @pytest.fixture
    def mock_collection(self):
        """Mock de la colección MongoDB."""
        return AsyncMock()
    
    @pytest.fixture
    def repository(self, mock_collection):
        """Repositorio con colección mockeada."""
        repo = MongoActuatorStateRepository()
        repo._collection = mock_collection
        return repo
    
    @pytest.fixture
    def sample_actuator_state(self):
        """Estado de actuador de ejemplo para pruebas."""
        return ActuatorState(
            actuatorId="fan_001",
            last_target=75,
            last_emitted_at=datetime(2024, 1, 15, 10, 30, 0, tzinfo=timezone.utc),
            last_hold_seconds=30
        )
    
    @pytest.fixture
    def sample_document(self):
        """Documento MongoDB de ejemplo para estado de actuador."""
        return {
            "_id": "507f1f77bcf86cd799439012",
            "actuatorId": "fan_001",
            "lastTarget": 75,
            "lastEmittedAt": datetime(2024, 1, 15, 10, 30, 0, tzinfo=timezone.utc),
            "lastHoldSeconds": 30
        }
    
    @pytest.mark.asyncio
    async def test_get_state_found(self, repository, mock_collection, sample_document):
        """Prueba obtener estado por ID de actuador cuando existe."""
        mock_collection.find_one.return_value = sample_document
        
        result = await repository.get_state("fan_001")
        
        assert result is not None
        assert result.actuatorId == "fan_001"
        assert result.last_target == 75
        assert result.last_hold_seconds == 30
        mock_collection.find_one.assert_called_once_with({"actuatorId": "fan_001"})
    
    @pytest.mark.asyncio
    async def test_get_state_not_found(self, repository, mock_collection):
        """Prueba obtener estado por ID de actuador cuando no existe."""
        mock_collection.find_one.return_value = None
        
        result = await repository.get_state("nonexistent")
        
        assert result is None
        mock_collection.find_one.assert_called_once_with({"actuatorId": "nonexistent"})
    
    @pytest.mark.asyncio
    async def test_save_state(self, repository, mock_collection, sample_actuator_state):
        """Prueba guardar estado de actuador."""
        mock_result = MagicMock()
        mock_result.matched_count = 1
        mock_collection.update_one.return_value = mock_result
        
        await repository.save_state("fan_001", sample_actuator_state)
        
        # Verificar que se llamó update_one con upsert=True
        mock_collection.update_one.assert_called_once()
        call_args = mock_collection.update_one.call_args
        assert call_args[0][0] == {"actuatorId": "fan_001"}
        assert "$set" in call_args[0][1]
        assert call_args[1]["upsert"] is True
    
    def test_actuator_state_to_document_conversion(self, repository, sample_actuator_state):
        """Prueba conversión de ActuatorState a documento MongoDB."""
        doc = repository._actuator_state_to_document(sample_actuator_state)
        
        assert doc["actuatorId"] == "fan_001"
        assert doc["lastTarget"] == 75
        assert doc["lastHoldSeconds"] == 30
        assert "lastEmittedAt" in doc
    
    def test_document_to_actuator_state_conversion(self, repository, sample_document):
        """Prueba conversión de documento MongoDB a ActuatorState."""
        state = repository._document_to_actuator_state(sample_document)
        
        assert state.actuatorId == "fan_001"
        assert state.last_target == 75
        assert state.last_hold_seconds == 30
        assert state.last_emitted_at == datetime(2024, 1, 15, 10, 30, 0, tzinfo=timezone.utc)
    
    def test_should_skip_hysteresis(self, sample_actuator_state):
        """Prueba lógica de histeresis."""
        # Cambio pequeño - debe saltarse
        assert sample_actuator_state.should_skip_hysteresis(77, 5.0) is True
        
        # Cambio grande - no debe saltarse
        assert sample_actuator_state.should_skip_hysteresis(85, 5.0) is False
    
    def test_should_skip_cooldown(self, sample_actuator_state):
        """Prueba lógica de cooldown."""
        # Actualizar timestamp a ahora para simular comando reciente
        sample_actuator_state.last_emitted_at = datetime.now(timezone.utc)
        
        # Debe saltarse por cooldown
        assert sample_actuator_state.should_skip_cooldown(60) is True
        
        # Simular comando antiguo
        sample_actuator_state.last_emitted_at = datetime.now(timezone.utc) - timedelta(seconds=120)
        
        # No debe saltarse
        assert sample_actuator_state.should_skip_cooldown(60) is False
    
    def test_update_state(self, sample_actuator_state):
        """Prueba actualización de estado."""
        old_time = sample_actuator_state.last_emitted_at
        
        sample_actuator_state.update_state(90)
        
        assert sample_actuator_state.last_target == 90
        assert sample_actuator_state.last_emitted_at > old_time