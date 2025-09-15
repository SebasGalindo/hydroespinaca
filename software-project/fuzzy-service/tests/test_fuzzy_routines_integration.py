import os
import importlib
import sys
from uuid import uuid4
from pymongo import MongoClient

from fastapi.testclient import TestClient
from dotenv import load_dotenv

# Cargar variables desde .env.test para tests de integración
load_dotenv(dotenv_path=".env.test")

def cleanup_test_data(conn_str: str, db_name: str):
    """Limpia los datos de prueba de la base de datos."""
    client = MongoClient(conn_str)
    db = client[db_name]
    
    # Drop all indexes first to avoid conflicts
    try:
        db.fuzzy_routines.drop_indexes()
    except Exception:
        pass  # Ignore if collection doesn't exist
    
    # Delete all documents from collections instead of dropping database
    try:
        collections = db.list_collection_names()
        for collection_name in collections:
            db[collection_name].delete_many({})
    except Exception as e:
        print(f"Warning: Could not clean collection {collection_name}: {e}")
    
    client.close()

def setup_test_environment():
    """Configura el entorno de prueba usando la configuración de .env.test."""
    # La configuración de MongoDB ya está cargada desde .env.test
    # Verificar que estamos usando la base de datos de test
    db_name = os.getenv("MONGO_DATABASE_NAME") or os.getenv("FUZZY_MONGO_DATABASE")
    assert db_name == "HydroEspinacaTest", f"Expected test database 'HydroEspinacaTest', got '{db_name}'"
    
    print(f"Using test database: {db_name}")
    
    conn_str = os.getenv("MONGO_CONNECTION_STRING") or os.getenv("FUZZY_MONGO_CONNECTION_STRING") or "mongodb://localhost:27017"
    
    # Cleanup any existing data first
    cleanup_test_data(conn_str, db_name)
    
    # Clear the entire kink DI container to force fresh instances
    import kink
    kink.di.clear_cache()
    
    # Force reload of all relevant modules
    modules_to_reload = [
        'FuzzyService.Infrastructure.Configuration.DatabaseConfiguration',
        'FuzzyService.Infrastructure.Configuration.DependencyInjection',
        'FuzzyService.Application.Configuration.DependencyInjection',
        'FuzzyService.Api.main'
    ]
    
    for module_name in modules_to_reload:
        if module_name in sys.modules:
            importlib.reload(sys.modules[module_name])
    
    # Import and reload main app
    import FuzzyService.Api.main as main
    importlib.reload(main)
    
    return main.app


def test_fuzzy_routines_basic_crud():
    """Test de integración API-MongoDB para FuzzyRoutines CRUD."""
    # Setup test environment using .env.test configuration
    app = setup_test_environment()
    
    with TestClient(app) as client:
        # Create routine
        routine_resp = client.post("/api/fuzzy-routines", json={
            "routine_name": f"TestRoutine_{uuid4().hex[:8]}",
            "steps": [
                {
                    "step_id": 1,
                    "condition": "temperature > 25",
                    "power_term_id": "507f1f77bcf86cd799439011",
                    "duration_term_id": "507f1f77bcf86cd799439012"
                },
                {
                    "step_id": 2,
                    "condition": "humidity < 60",
                    "power_term_id": "507f1f77bcf86cd799439013",
                    "duration_term_id": "507f1f77bcf86cd799439014"
                }
            ]
        })
        assert routine_resp.status_code == 201
        routine_data = routine_resp.json()
        routine_id = routine_data["id"]
        assert routine_data["routine_name"].startswith("TestRoutine")
        assert len(routine_data["steps"]) == 2
        assert routine_data["steps"][0]["condition"] == "temperature > 25"
        assert routine_data["steps"][1]["condition"] == "humidity < 60"

        # Get routine by ID
        get_resp = client.get(f"/api/fuzzy-routines/{routine_id}")
        assert get_resp.status_code == 200
        get_data = get_resp.json()
        assert get_data["id"] == routine_id
        assert get_data["routine_name"] == routine_data["routine_name"]
        assert len(get_data["steps"]) == 2

        # Update routine
        update_resp = client.put(f"/api/fuzzy-routines/{routine_id}", json={
            "routine_name": f"UpdatedTestRoutine_{uuid4().hex[:8]}",
            "steps": [
                {
                    "step_id": 1,
                    "condition": "temperature > 30",
                    "power_term_id": "507f1f77bcf86cd799439015",
                    "duration_term_id": "507f1f77bcf86cd799439016"
                }
            ]
        })
        if update_resp.status_code != 200:
            print(f"Update error: {update_resp.status_code} - {update_resp.text}")
            assert False, f"Update failed with {update_resp.status_code}: {update_resp.text}"
        assert update_resp.status_code == 200
        update_data = update_resp.json()
        assert update_data["routine_name"].startswith("UpdatedTestRoutine")
        assert len(update_data["steps"]) == 1
        assert update_data["steps"][0]["condition"] == "temperature > 30"

        # Get all routines
        all_resp = client.get("/api/fuzzy-routines")
        assert all_resp.status_code == 200
        all_data = all_resp.json()
        assert len(all_data) >= 1
        assert any(routine["id"] == routine_id for routine in all_data)

        # Test pagination
        paginated_resp = client.get("/api/fuzzy-routines?skip=0&limit=1")
        assert paginated_resp.status_code == 200
        paginated_data = paginated_resp.json()
        assert len(paginated_data) <= 1

        # Delete routine
        delete_resp = client.delete(f"/api/fuzzy-routines/{routine_id}")
        assert delete_resp.status_code == 200

        # Verify deletion
        get_deleted_resp = client.get(f"/api/fuzzy-routines/{routine_id}")
        assert get_deleted_resp.status_code == 404

        print("âœ… FuzzyRoutines basic CRUD test completed successfully")


def test_fuzzy_routines_step_management():
    """Test step management operations for FuzzyRoutines."""
    # Setup test environment
    app = setup_test_environment()
    
    with TestClient(app) as client:
        # Create routine with initial steps
        routine_resp = client.post("/api/fuzzy-routines", json={
            "routine_name": f"StepTestRoutine{uuid4().hex[:8]}",
            "steps": [
                {
                    "step_id": 1,
                    "condition": "initial_condition",
                    "power_term_id": "507f1f77bcf86cd799439017",
                    "duration_term_id": "507f1f77bcf86cd799439018"
                }
            ]
        })
        assert routine_resp.status_code == 201
        routine_id = routine_resp.json()["id"]

        # Add step to routine
        add_step_resp = client.post(f"/api/fuzzy-routines/{routine_id}/steps", json={
            "step_id": 2,
            "condition": "new_step_condition",
            "power_term_id": "507f1f77bcf86cd799439019",
            "duration_term_id": "507f1f77bcf86cd799439020"
        })
        assert add_step_resp.status_code == 200
        add_step_data = add_step_resp.json()
        assert len(add_step_data["steps"]) == 2
        assert add_step_data["steps"][1]["condition"] == "new_step_condition"

        # Update step in routine (step_id = 2, the second step)
        update_step_resp = client.put(f"/api/fuzzy-routines/{routine_id}/steps/2", json={
            "step_id": 2,
            "condition": "updated_step_condition",
            "power_term_id": "507f1f77bcf86cd799439021"
        })
        assert update_step_resp.status_code == 200
        update_step_data = update_step_resp.json()
        assert update_step_data["steps"][1]["condition"] == "updated_step_condition"
        assert update_step_data["steps"][1]["power_term_id"] == "507f1f77bcf86cd799439021"
        # duration_term_id should remain unchanged
        assert update_step_data["steps"][1]["duration_term_id"] == "507f1f77bcf86cd799439020"

        # Delete step from routine (step_id = 1, the first step)
        delete_step_resp = client.delete(f"/api/fuzzy-routines/{routine_id}/steps/1")
        assert delete_step_resp.status_code == 200
        delete_step_data = delete_step_resp.json()
        assert len(delete_step_data["steps"]) == 1
        assert delete_step_data["steps"][0]["condition"] == "updated_step_condition"

        # Clean up
        client.delete(f"/api/fuzzy-routines/{routine_id}")

        print("âœ… FuzzyRoutines step management test completed successfully")


def test_fuzzy_routines_validation_errors():
    """Test validation errors for FuzzyRoutines."""
    # Setup test environment
    app = setup_test_environment()
    
    with TestClient(app) as client:
        # Test missing required fields
        missing_fields_resp = client.post("/api/fuzzy-routines", json={})
        assert missing_fields_resp.status_code == 422

        # Test invalid routine_id for step operations
        invalid_id = "invalid_id_format"
        add_step_invalid_resp = client.post(f"/api/fuzzy-routines/{invalid_id}/steps", json={
            "step_id": 1,
            "condition": "test",
            "power_term_id": "507f1f77bcf86cd799439022",
            "duration_term_id": "507f1f77bcf86cd799439023"
        })
        assert add_step_invalid_resp.status_code == 422  # Validation error

        # Test non-existent routine for step operations
        non_existent_id = "507f1f77bcf86cd799439011"  # Valid ObjectId format but non-existent
        add_step_not_found_resp = client.post(f"/api/fuzzy-routines/{non_existent_id}/steps", json={
            "step_id": 1,
            "condition": "test",
            "power_term_id": "507f1f77bcf86cd799439024",
            "duration_term_id": "507f1f77bcf86cd799439025"
        })
        assert add_step_not_found_resp.status_code == 404

        print("âœ… FuzzyRoutines validation test completed successfully")


def test_fuzzy_routines_duplicate_name():
    """Test duplicate name handling for FuzzyRoutines."""
    # Setup test environment
    app = setup_test_environment()
    
    with TestClient(app) as client:
        routine_name = f"DuplicateTestRoutine{uuid4().hex[:8]}"
        
        # Create first routine
        first_resp = client.post("/api/fuzzy-routines", json={
                "routine_name": routine_name,
            "steps": [
                {
                    "step_id": 1,
                    "condition": "test_condition",
                    "power_term_id": "507f1f77bcf86cd799439026",
                    "duration_term_id": "507f1f77bcf86cd799439027"
                }
            ]
        })
        if first_resp.status_code != 201:
            print(f"Create routine failed with status {first_resp.status_code}")
            print(f"Response: {first_resp.text}")
        assert first_resp.status_code == 201
        first_id = first_resp.json()["id"]

        # Try to create second routine with same name
        duplicate_resp = client.post("/api/fuzzy-routines", json={
            "routine_name": routine_name,
            "steps": [
                {
                    "step_id": 1,
                    "condition": "another_condition",
                    "power_term_id": "507f1f77bcf86cd799439028",
                    "duration_term_id": "507f1f77bcf86cd799439029"
                }
            ]
        })
        assert duplicate_resp.status_code == 409  # Conflict - duplicate name

        # Clean up
        client.delete(f"/api/fuzzy-routines/{first_id}")

        print("âœ… FuzzyRoutines duplicate name test completed successfully")

