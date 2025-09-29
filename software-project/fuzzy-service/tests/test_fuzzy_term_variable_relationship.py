import os
import importlib
from uuid import uuid4

from fastapi.testclient import TestClient
from dotenv import load_dotenv

# Cargar variables desde .env.test para tests de integración
load_dotenv(dotenv_path=".env.test")


def test_create_term_adds_to_variable_terms_list():
    """Prueba que al crear un término, se agrega automáticamente a la lista de términos de la variable."""
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
        # 1. Crear una variable difusa
        variable_data = {
            "name": "Temperatura Test",
            "description": "Variable de prueba para términos",
            "variable_type": "input",
            "device_id": "sensor_001"
        }
        
        variable_response = client.post("/api/fuzzy-variables", json=variable_data)
        assert variable_response.status_code == 201
        variable = variable_response.json()
        variable_id = variable["id"]
        
        # Verificar que la variable inicialmente no tiene términos
        assert variable["terms"] == []
        
        # 2. Crear un término asociado a la variable
        term_data = {
            "variable_id": variable_id,
            "label": "Bajo",
            "membership_function": {
                "function_type": "triangular",
                "parameters": [0, 10, 20],
                "universe_min": 0,
                "universe_max": 100
            }
        }
        
        term_response = client.post("/api/fuzzy-terms", json=term_data)
        assert term_response.status_code == 201
        term = term_response.json()
        term_id = term["id"]
        
        # 3. Verificar que el término se creó correctamente
        assert term["variable_id"] == variable_id
        assert term["label"] == "Bajo"
        
        # 4. Obtener la variable actualizada y verificar que contiene el término
        updated_variable_response = client.get(f"/api/fuzzy-variables/{variable_id}")
        assert updated_variable_response.status_code == 200
        updated_variable = updated_variable_response.json()
        
        # Verificar que el término se agregó a la lista de términos de la variable
        assert term_id in updated_variable["terms"]
        assert len(updated_variable["terms"]) == 1
        
        # 5. Crear un segundo término para verificar que se agrega correctamente
        term_data_2 = {
            "variable_id": variable_id,
            "label": "Alto",
            "membership_function": {
                "function_type": "triangular",
                "parameters": [60, 80, 100],
                "universe_min": 0,
                "universe_max": 100
            }
        }
        
        term_response_2 = client.post("/api/fuzzy-terms", json=term_data_2)
        assert term_response_2.status_code == 201
        term_2 = term_response_2.json()
        term_id_2 = term_2["id"]
        
        # 6. Verificar que ambos términos están en la variable
        final_variable_response = client.get(f"/api/fuzzy-variables/{variable_id}")
        assert final_variable_response.status_code == 200
        final_variable = final_variable_response.json()
        
        assert term_id in final_variable["terms"]
        assert term_id_2 in final_variable["terms"]
        assert len(final_variable["terms"]) == 2
        
        # Cleanup: eliminar los términos y la variable
        client.delete(f"/api/fuzzy-terms/{term_id}")
        client.delete(f"/api/fuzzy-terms/{term_id_2}")
        client.delete(f"/api/fuzzy-variables/{variable_id}")


def test_delete_term_removes_from_variable_terms_list():
    """Prueba que al eliminar un término, se remueve automáticamente de la lista de términos de la variable."""
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
        # 1. Crear una variable difusa
        variable_data = {
            "name": "Humedad Test",
            "description": "Variable de prueba para eliminación de términos",
            "variable_type": "input",
            "device_id": "sensor_002"
        }
        
        variable_response = client.post("/api/fuzzy-variables", json=variable_data)
        assert variable_response.status_code == 201
        variable = variable_response.json()
        variable_id = variable["id"]
        
        # 2. Crear dos términos asociados a la variable
        term_data_1 = {
            "variable_id": variable_id,
            "label": "Seco",
            "membership_function": {
                "function_type": "triangular",
                "parameters": [0, 20, 40],
                "universe_min": 0,
                "universe_max": 100
            }
        }
        
        term_data_2 = {
            "variable_id": variable_id,
            "label": "Húmedo",
            "membership_function": {
                "function_type": "triangular",
                "parameters": [60, 80, 100],
                "universe_min": 0,
                "universe_max": 100
            }
        }
        
        term_response_1 = client.post("/api/fuzzy-terms", json=term_data_1)
        term_response_2 = client.post("/api/fuzzy-terms", json=term_data_2)
        
        assert term_response_1.status_code == 201
        assert term_response_2.status_code == 201
        
        term_1 = term_response_1.json()
        term_2 = term_response_2.json()
        term_id_1 = term_1["id"]
        term_id_2 = term_2["id"]
        
        # 3. Verificar que ambos términos están en la variable
        variable_with_terms_response = client.get(f"/api/fuzzy-variables/{variable_id}")
        assert variable_with_terms_response.status_code == 200
        variable_with_terms = variable_with_terms_response.json()
        
        assert term_id_1 in variable_with_terms["terms"]
        assert term_id_2 in variable_with_terms["terms"]
        assert len(variable_with_terms["terms"]) == 2
        
        # 4. Eliminar el primer término
        delete_response = client.delete(f"/api/fuzzy-terms/{term_id_1}")
        assert delete_response.status_code == 200
        
        # 5. Verificar que el término se removió de la lista de la variable
        updated_variable_response = client.get(f"/api/fuzzy-variables/{variable_id}")
        assert updated_variable_response.status_code == 200
        updated_variable = updated_variable_response.json()
        
        assert term_id_1 not in updated_variable["terms"]
        assert term_id_2 in updated_variable["terms"]
        assert len(updated_variable["terms"]) == 1
        
        # Cleanup: eliminar el término restante y la variable
        client.delete(f"/api/fuzzy-terms/{term_id_2}")
        client.delete(f"/api/fuzzy-variables/{variable_id}")