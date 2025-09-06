import os
import importlib
from uuid import uuid4

from fastapi.testclient import TestClient
from dotenv import load_dotenv

load_dotenv()


def test_fuzzy_rules_basic_crud():
    """Test de integración API-MongoDB para FuzzyRules CRUD."""
    # Setup MongoDB test environment
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
        # Create system first
        sys_resp = client.post("/api/fuzzy-systems", json={
            "name": f"TestSystem{uuid4().hex[:8]}",
            "isActive": False
        })
        assert sys_resp.status_code == 201
        sys_id = sys_resp.json()["id"]

        # Create variables
        var1_resp = client.post("/api/fuzzy-variables", json={
            "name": f"TestVar1{uuid4().hex[:8]}",
            "variable_type": "input"
        })
        assert var1_resp.status_code == 201
        var1_id = var1_resp.json()["id"]

        var2_resp = client.post("/api/fuzzy-variables", json={
            "name": f"TestVar2{uuid4().hex[:8]}",
            "variable_type": "output"
        })
        assert var2_resp.status_code == 201
        var2_id = var2_resp.json()["id"]
        
        # Add variables to system
        update_sys_resp = client.put(f"/api/fuzzy-systems/{sys_id}", json={
            "input_variable_ids": [var1_id],
            "output_variable_ids": [var2_id]
        })
        if update_sys_resp.status_code != 200:
            print(f"Update system error: {update_sys_resp.status_code} - {update_sys_resp.text}")
        assert update_sys_resp.status_code == 200

        # Create terms
        term1_resp = client.post("/api/fuzzy-terms", json={
            "variable_id": var1_id,
            "label": f"TestTerm1{uuid4().hex[:8]}",
            "membership_function": {
                "function_type": "triangular",
                "parameters": [0.0, 0.5, 1.0],
                "universe_min": 0.0,
                "universe_max": 1.0
            }
        })
        assert term1_resp.status_code == 201
        term1_id = term1_resp.json()["id"]

        term2_resp = client.post("/api/fuzzy-terms", json={
            "variable_id": var2_id,
            "label": f"TestTerm2{uuid4().hex[:8]}",
            "membership_function": {
                "function_type": "triangular",
                "parameters": [0.0, 0.5, 1.0],
                "universe_min": 0.0,
                "universe_max": 1.0
            }
        })
        assert term2_resp.status_code == 201
        term2_id = term2_resp.json()["id"]

        # Create routine for consequent
        routine_resp = client.post("/api/fuzzy-routines", json={
            "routine_name": f"TestRoutine{uuid4().hex[:8]}",
            "steps": []
        })
        assert routine_resp.status_code == 201
        routine_id = routine_resp.json()["id"]

        # Create rule
        rule_resp = client.post("/api/fuzzy-rules", json={
            "system_id": sys_id,
            "name": f"TestRule{uuid4().hex[:8]}",
            "description": "Test rule for integration testing",
            "conditions": [
                {
                    "variable_id": var1_id,
                    "operator": "IS",
                    "value": "TestTerm1"
                }
            ],
            "connectors": [],
            "consequent": routine_id
        })
        assert rule_resp.status_code == 201
        rule_data = rule_resp.json()
        rule_id = rule_data["id"]
        assert rule_data["name"].startswith("TestRule")
        assert rule_data["system_id"] == sys_id
        assert len(rule_data["conditions"]) == 1
        assert rule_data["description"] == "Test rule for integration testing"

        # Get rule by ID
        get_resp = client.get(f"/api/fuzzy-rules/{rule_id}")
        assert get_resp.status_code == 200
        get_data = get_resp.json()
        assert get_data["id"] == rule_id
        assert get_data["name"] == rule_data["name"]

        # Update rule
        update_resp = client.put(f"/api/fuzzy-rules/{rule_id}", json={
            "name": f"UpdatedRule{uuid4().hex[:8]}",
            "description": "Updated test rule",
            "conditions": [
                {
                    "variable_id": var1_id,
                    "operator": "IS",
                    "value": "TestTerm1"
                }
            ],
            "connectors": [],
            "consequent": routine_id
        })
        if update_resp.status_code != 200:
            print(f"Update failed with status {update_resp.status_code}")
            print(f"Response: {update_resp.text}")
        assert update_resp.status_code == 200
        update_data = update_resp.json()
        assert update_data["name"].startswith("UpdatedRule")
        assert update_data["consequent"] == routine_id

        # Note: Status update endpoint not implemented for rules

        # Get rules by system
        system_rules_resp = client.get(f"/api/fuzzy-rules/system/{sys_id}")
        assert system_rules_resp.status_code == 200
        system_rules = system_rules_resp.json()
        assert len(system_rules) >= 1
        assert any(rule["id"] == rule_id for rule in system_rules)

        # Delete rule
        delete_resp = client.delete(f"/api/fuzzy-rules/{rule_id}")
        assert delete_resp.status_code == 200

        # Verify deletion
        get_deleted_resp = client.get(f"/api/fuzzy-rules/{rule_id}")
        assert get_deleted_resp.status_code == 404

        print("✅ FuzzyRules integration test completed successfully")


def test_fuzzy_rules_validation_errors():
    """Test validation errors for FuzzyRules."""
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
        # Test invalid system_id
        invalid_rule_resp = client.post("/api/fuzzy-rules", json={
            "system_id": "invalid_id",
            "name": "TestRule",
            "conditions": [],
            "logical_connectors": [],
            "consequents": []
        })
        assert invalid_rule_resp.status_code == 422  # Validation error

        # Test missing required fields
        missing_fields_resp = client.post("/api/fuzzy-rules", json={})
        assert missing_fields_resp.status_code == 422

        print("✅ FuzzyRules validation test completed successfully")
