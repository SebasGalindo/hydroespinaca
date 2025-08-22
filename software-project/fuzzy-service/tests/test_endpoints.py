"""Pruebas para endpoints HTTP del API.

Este archivo contiene pruebas para los endpoints REST de la aplicación.
"""

import pytest
from datetime import datetime, timezone
from unittest.mock import Mock, AsyncMock, patch
from fastapi.testclient import TestClient
from fastapi import FastAPI, HTTPException

from api.endpoints import router
from application.dtos import (
    VariableCreateDto,
    RoutineCreateDto,
    RoutineStateUpdateDto,
    ReadingBatch,
    SimulateResponseDto
)
from domain.models import Variable, Routine, ActuatorType


@pytest.fixture
def app():
    """Fixture que crea una aplicación FastAPI para pruebas."""
    from application.use_cases import (
        VariableManagementUseCase,
        RoutineManagementUseCase,
        SimulationUseCase,
        FuzzyEvaluationUseCase
    )
    from application.interfaces.fuzzy_service import IFuzzyService
    from api.security import ScopeChecker
    
    test_app = FastAPI()
    test_app.include_router(router)
    
    # Override dependencies with mocks
    test_app.dependency_overrides[VariableManagementUseCase] = lambda: Mock()
    test_app.dependency_overrides[RoutineManagementUseCase] = lambda: Mock()
    test_app.dependency_overrides[SimulationUseCase] = lambda: Mock()
    test_app.dependency_overrides[FuzzyEvaluationUseCase] = lambda: Mock()
    test_app.dependency_overrides[IFuzzyService] = lambda: Mock()
    test_app.dependency_overrides[ScopeChecker] = lambda scopes: lambda: {"user_id": "test"}
    
    return test_app


@pytest.fixture
def client(app):
    """Fixture que crea un cliente de prueba."""
    return TestClient(app)


@pytest.fixture
def mock_auth_token():
    """Mock token de autenticación para pruebas."""
    return "Bearer test-token"


class TestHealthEndpoints:
    """Pruebas para endpoints de health check."""
    
    def test_health_check(self, client):
        """Test del endpoint de health check básico."""
        response = client.get("/healthz")
        
        assert response.status_code == 200
        data = response.json()
        assert data["status"] == "healthy"
        assert data["service"] == "fuzzy-service"
    
    @patch('api.endpoints.mongo_db')
    def test_readiness_check_healthy(self, mock_mongo_db, client):
        """Test del endpoint de readiness check cuando MongoDB está disponible."""
        # Mock de MongoDB saludable
        mock_mongo_db.client.admin.command = AsyncMock(return_value={})
        
        response = client.get("/readyz")
        
        assert response.status_code == 200
        data = response.json()
        assert data["status"] == "ready"
        assert data["service"] == "fuzzy-service"
        assert "checks" in data
        assert data["checks"]["mongodb"]["status"] == "healthy"
    
    @patch('api.endpoints.mongo_db')
    def test_readiness_check_unhealthy(self, mock_mongo_db, client):
        """Test del endpoint de readiness check cuando MongoDB no está disponible."""
        # Mock de MongoDB con error
        mock_mongo_db.client.admin.command = AsyncMock(side_effect=Exception("Connection failed"))
        
        response = client.get("/readyz")
        
        assert response.status_code == 503
        data = response.json()
        assert "not_ready" in data["detail"]["status"]


class TestVariableEndpoints:
    """Pruebas para endpoints de gestión de variables."""
    
    def test_create_variable_success(self, client, app):
        """Test de creación exitosa de variable."""
        # Configurar mock para el caso de uso
        from application.use_cases import VariableManagementUseCase
        mock_use_case = Mock()
        mock_use_case.create_variable = AsyncMock(return_value="var_123")
        app.dependency_overrides[VariableManagementUseCase] = lambda: mock_use_case
        
        variable_data = {
            "id": "temperature",
            "name": "Temperature",
            "unit": "°C",
            "min_value": 0.0,
            "max_value": 50.0,
            "fuzzy_sets": []
        }
        
        response = client.post("/api/variables", json=variable_data)
        
        assert response.status_code == 201
        data = response.json()
        assert data["id"] == "var_123"
        assert "created successfully" in data["message"]
    
    def test_list_variables_success(self, client, app):
        """Test de listado exitoso de variables."""
        # Configurar mock para el caso de uso
        from application.use_cases import VariableManagementUseCase
        mock_variables = [
            {
                "id": "temp",
                "name": "Temperature",
                "unit": "°C",
                "min_value": 0.0,
                "max_value": 50.0,
                "fuzzy_sets": []
            }
        ]
        mock_use_case = Mock()
        mock_use_case.list_variables = AsyncMock(return_value=mock_variables)
        app.dependency_overrides[VariableManagementUseCase] = lambda: mock_use_case
        
        response = client.get("/api/variables")
        
        assert response.status_code == 200
        data = response.json()
        assert len(data) == 1
        assert data[0]["id"] == "temp"
        assert data[0]["name"] == "Temperature"


class TestRoutineEndpoints:
    """Pruebas para endpoints de gestión de rutinas."""
    
    def test_create_routine_success(self, client, app):
        """Test de creación exitosa de rutina."""
        # Configurar mock para el caso de uso
        from application.use_cases import RoutineManagementUseCase
        mock_use_case = Mock()
        mock_use_case.create_routine = AsyncMock(return_value="routine_123")
        app.dependency_overrides[RoutineManagementUseCase] = lambda: mock_use_case
        
        routine_data = {
            "id": "test_routine",
            "name": "Test Routine",
            "active": True,
            "fuzzy_rules": [],
            "threshold_rules": [],
            "outputs": []
        }
        
        response = client.post("/api/routines", json=routine_data)
        
        assert response.status_code == 201
        data = response.json()
        assert data["id"] == "routine_123"
        assert "created successfully" in data["message"]
    
    def test_list_routines_success(self, client, app):
        """Test de listado exitoso de rutinas."""
        # Configurar mock para el caso de uso
        from application.use_cases import RoutineManagementUseCase
        mock_routines = [
            {
                "id": "routine1",
                "name": "Test Routine",
                "active": True,
                "fuzzy_rules": [],
                "threshold_rules": [],
                "outputs": []
            }
        ]
        mock_use_case = Mock()
        mock_use_case.list_routines = AsyncMock(return_value=mock_routines)
        app.dependency_overrides[RoutineManagementUseCase] = lambda: mock_use_case
        
        response = client.get("/api/routines")
        
        assert response.status_code == 200
        data = response.json()
        assert len(data) == 1
        assert data[0]["id"] == "routine1"
        assert data[0]["name"] == "Test Routine"
    
    def test_update_routine_state_success(self, client, app):
        """Test de actualización exitosa del estado de rutina."""
        # Configurar mock para el caso de uso
        from application.use_cases import RoutineManagementUseCase
        mock_use_case = Mock()
        mock_use_case.update_routine_state = AsyncMock(return_value=None)
        app.dependency_overrides[RoutineManagementUseCase] = lambda: mock_use_case
        
        state_data = {"active": False}
        
        response = client.patch("/api/routines/routine_123/state", json=state_data)
        
        assert response.status_code == 200
        data = response.json()
        assert "updated successfully" in data["message"]


class TestSimulationEndpoints:
    """Pruebas para endpoints de simulación."""
    
    def test_simulate_success(self, client, app):
        """Test de simulación exitosa."""
        # Configurar mocks para los casos de uso
        from application.use_cases import SimulationUseCase, RoutineManagementUseCase, FuzzyEvaluationUseCase
        
        mock_result = {
            "plans": [
                {"actuator_id": "pump_001", "target_value": 75.0},
                {"actuator_id": "valve_002", "target_value": 50.0}
            ]
        }
        mock_simulation_use_case = Mock()
        mock_simulation_use_case.simulate = AsyncMock(return_value=mock_result)
        app.dependency_overrides[SimulationUseCase] = lambda: mock_simulation_use_case
        
        mock_routine_use_case = Mock()
        mock_routine_use_case.list_routines = AsyncMock(return_value=[])
        app.dependency_overrides[RoutineManagementUseCase] = lambda: mock_routine_use_case
        
        mock_evaluation_use_case = Mock()
        app.dependency_overrides[FuzzyEvaluationUseCase] = lambda: mock_evaluation_use_case
        
        readings_data = {
            "esp32Id": "test_esp32",
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "readings": [
                {
                    "variableId": "temperature",
                    "value": 25.5
                }
            ]
        }
        
        response = client.post("/api/simulate", json=readings_data)
        
        assert response.status_code == 200
        data = response.json()
        assert "plans" in data
        assert len(data["plans"]) == 2
        assert data["plans"][0]["actuator_id"] == "pump_001"
        assert data["plans"][1]["actuator_id"] == "valve_002"


class TestErrorHandling:
    """Pruebas para manejo de errores."""
    
    def test_invalid_json_payload(self, client):
        """Test de manejo de payload JSON inválido."""
        response = client.post(
            "/api/variables",
            data="invalid json",
            headers={"Content-Type": "application/json"}
        )
        
        assert response.status_code == 422
    
    def test_missing_required_fields(self, client):
        """Test de manejo de campos requeridos faltantes."""
        incomplete_data = {
            "name": "Test Variable"
            # Faltan campos requeridos como 'id', 'unit', etc.
        }
        
        response = client.post("/api/variables", json=incomplete_data)
        
        assert response.status_code == 422
        data = response.json()
        assert "detail" in data