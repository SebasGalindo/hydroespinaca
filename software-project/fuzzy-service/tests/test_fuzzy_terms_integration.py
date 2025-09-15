import os
import importlib
from uuid import uuid4

from fastapi.testclient import TestClient
from dotenv import load_dotenv

# Cargar variables desde .env.test para tests de integración
load_dotenv(dotenv_path=".env.test")


def test_fuzzy_terms_basic_crud():
    """Basic CRUD integration test for FuzzyTerms."""
    # Setup environment
    conn_str = os.getenv("MONGO_CONNECTION_STRING") or "mongodb://localhost:27017"
    db_name = os.getenv("MONGO_DATABASE_NAME") or "fuzzy_test"
    
    os.environ["MONGO_CONNECTION_STRING"] = conn_str
    os.environ["MONGO_DATABASE_NAME"] = db_name
    os.environ["MONGO_PING_ON_STARTUP"] = "false"
    os.environ["FUZZY_ENSURE_INDEXES_ON_STARTUP"] = "true"

    # Reload configuration
    import FuzzyService.Infrastructure.Configuration.DatabaseConfiguration as db
    importlib.reload(db)

    import FuzzyService.Api.main as main
    importlib.reload(main)

    app = main.app

    with TestClient(app) as client:
        # Create variable
        var_resp = client.post("/api/fuzzy-variables", json={
            "name": f"TestVar{uuid4().hex[:8]}",
            "variable_type": "input"
        })
        assert var_resp.status_code == 201
        var_id = var_resp.json()["id"]

        # Create term
        term_resp = client.post("/api/fuzzy-terms", json={
            "variable_id": var_id,
            "label": f"TestTerm{uuid4().hex[:8]}",
            "membership_function": {
                "function_type": "triangular",
                "parameters": [0.0, 0.5, 1.0],
                "universe_min": 0.0,
                "universe_max": 1.0
            }
        })
        assert term_resp.status_code == 201
        term_data = term_resp.json()
        term_id = term_data["id"]

        # Get term
        get_resp = client.get(f"/api/fuzzy-terms/{term_id}")
        assert get_resp.status_code == 200
        assert get_resp.json()["id"] == term_id

        # Delete term
        del_resp = client.delete(f"/api/fuzzy-terms/{term_id}")
        assert del_resp.status_code == 200
        assert del_resp.json() is True

        # Clean up
        client.delete(f"/api/fuzzy-variables/{var_id}")
