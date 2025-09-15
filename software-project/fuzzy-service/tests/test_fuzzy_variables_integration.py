import os
import importlib
from uuid import uuid4

from fastapi.testclient import TestClient
from dotenv import load_dotenv

# Cargar variables desde .env.test para tests de integración
load_dotenv(dotenv_path=".env.test")


def test_fuzzy_variables_basic_crud():
    """Test de integración API-MongoDB para FuzzyVariables CRUD."""
    # La configuración de MongoDB ya está cargada desde .env.test
    # Verificar que estamos usando la base de datos de test
    db_name = os.getenv("MONGO_DATABASE_NAME") or os.getenv("FUZZY_MONGO_DATABASE")
    assert db_name == "HydroEspinacaTest", f"Expected test database 'HydroEspinacaTest', got '{db_name}'"
    
    print(f"Using test database: {db_name}")

    # Reload configuration
    import FuzzyService.Infrastructure.Configuration.DatabaseConfiguration as db
    importlib.reload(db)

    import FuzzyService.Api.main as main
    importlib.reload(main)

    app = main.app

    with TestClient(app) as client:
        # Create variable
        var_resp = client.post("/api/fuzzy-variables", json={
            "name": f"TestVariable{uuid4().hex[:8]}",
            "variable_type": "input",
            "description": "Test variable for integration testing"
        })
        assert var_resp.status_code == 201
        var_data = var_resp.json()
        var_id = var_data["id"]
        assert var_data["name"].startswith("TestVariable")
        assert var_data["variable_type"] == "input"
        assert var_data["description"] == "Test variable for integration testing"

        # Get variable by ID
        get_resp = client.get(f"/api/fuzzy-variables/{var_id}")
        assert get_resp.status_code == 200
        get_data = get_resp.json()
        assert get_data["id"] == var_id
        assert get_data["name"] == var_data["name"]

        # Update variable
        update_resp = client.put(f"/api/fuzzy-variables/{var_id}", json={
            "id": var_id,
            "name": f"UpdatedVariable{uuid4().hex[:8]}",
            "description": "Updated description"
        })
        assert update_resp.status_code == 200
        update_data = update_resp.json()
        assert update_data["id"] == var_id
        assert update_data["name"].startswith("UpdatedVariable")
        assert update_data["description"] == "Updated description"

        # List variables
        list_resp = client.get("/api/fuzzy-variables")
        assert list_resp.status_code == 200
        list_data = list_resp.json()
        assert isinstance(list_data, list)
        # Should contain our created variable
        var_ids = [v["id"] for v in list_data]
        assert var_id in var_ids

        # Test duplicate name handling
        duplicate_resp = client.post("/api/fuzzy-variables", json={
            "name": update_data["name"],  # Same name as updated variable
            "variable_type": "output"
        })
        assert duplicate_resp.status_code == 409  # Conflict

        # Delete variable
        delete_resp = client.delete(f"/api/fuzzy-variables/{var_id}")
        assert delete_resp.status_code == 200
        assert delete_resp.json() is True

        # Verify deletion
        get_deleted_resp = client.get(f"/api/fuzzy-variables/{var_id}")
        assert get_deleted_resp.status_code == 404
