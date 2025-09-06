import os
import importlib
from uuid import uuid4

from fastapi.testclient import TestClient
from dotenv import load_dotenv

# Cargar variables desde .env antes de leer FUZZY_* o MONGO_*
load_dotenv()


def test_api_mongo_e2e_create_patch_get_delete():
    """
    End-to-end API→Mongo integration using a real MongoDB.
    Uses MONGO_CONNECTION_STRING or FUZZY_MONGO_CONNECTION_STRING if present;
    otherwise falls back to mongodb://localhost:27017 with database 'fuzzy_test'.
    Verifies create, patch status, get by id, duplicate handling and delete.
    """
    conn_str = os.getenv("MONGO_CONNECTION_STRING") or os.getenv("FUZZY_MONGO_CONNECTION_STRING")
    if not conn_str:
        conn_str = "mongodb://localhost:27017"
    db_name = os.getenv("MONGO_DATABASE_NAME") or os.getenv("FUZZY_MONGO_DATABASE") or "fuzzy_test"

    # Ensure env BEFORE importing the app so settings are loaded
    os.environ["MONGO_CONNECTION_STRING"] = conn_str
    os.environ["MONGO_DATABASE_NAME"] = db_name
    os.environ["MONGO_PING_ON_STARTUP"] = os.getenv("MONGO_PING_ON_STARTUP", "true")
    # Forzar ensure_indexes para garantizar unicidad por nombre durante la prueba
    os.environ["FUZZY_ENSURE_INDEXES_ON_STARTUP"] = "true"

    # Reload DatabaseConfiguration to clear cached settings/client in case other tests imported it
    import FuzzyService.Infrastructure.Configuration.DatabaseConfiguration as db
    importlib.reload(db)

    # Import (or reload) the FastAPI app after env is set to ensure DI reads new settings
    import FuzzyService.Api.main as main
    importlib.reload(main)

    app = main.app

    # Nombre único por ejecución para evitar colisiones con datos preexistentes
    unique_name = f"Sys IT Mongo {uuid4().hex[:8]}"

    # Exercise the API with the configured Mongo backend
    with TestClient(app) as client:
        # Create system (isActive=False → DRAFT)
        resp = client.post(
            "/api/fuzzy-systems",
            json={"name": unique_name, "isActive": False},
        )
        assert resp.status_code == 201, resp.text
        print("response 1 = ",resp.text)
        created = resp.json()
        sys_id = created["id"]
        assert created["name"] == unique_name
        assert created["status"] == "DRAFT"

        # Duplicate name should yield 409
        resp_dup = client.post(
            "/api/fuzzy-systems",
            json={"name": unique_name, "isActive": False},
        )
        assert resp_dup.status_code == 409, resp_dup.text
        print("response 2 = ",resp_dup.text)

        # Patch status to ACTIVE
        resp_patch = client.patch(
            f"/api/fuzzy-systems/{sys_id}/status",
            json={"status": "ACTIVE"},
        )
        assert resp_patch.status_code == 200, resp_patch.text
        print("response 3 = ",resp_patch.text)
        patched = resp_patch.json()
        assert patched["id"] == sys_id
        assert patched["status"] == "ACTIVE"

        # Get by ID should reflect ACTIVE
        resp_get = client.get(f"/api/fuzzy-systems/{sys_id}")
        assert resp_get.status_code == 200, resp_get.text
        print("response 4 = ",resp_get.text)
        fetched = resp_get.json()
        assert fetched["id"] == sys_id
        assert fetched["status"] == "ACTIVE"

        # Delete should return True (200)
        resp_del = client.delete(f"/api/fuzzy-systems/{sys_id}")
        assert resp_del.status_code == 200, resp_del.text
        print("response 5 = ",resp_del.text)
        assert resp_del.json() is True

        # Get by ID after delete should be 404
        resp_get2 = client.get(f"/api/fuzzy-systems/{sys_id}")
        assert resp_get2.status_code == 404, resp_get2.text
        print("response 6 = ",resp_get2.text)
