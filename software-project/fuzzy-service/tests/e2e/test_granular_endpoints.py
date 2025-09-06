import os
import importlib
from uuid import uuid4
from fastapi.testclient import TestClient
from dotenv import load_dotenv

# Cargar variables desde .env antes de leer FUZZY_* o MONGO_*
load_dotenv()


def test_granular_endpoints_workflow():
    """
    Test E2E para endpoints granulares:
    1. Gestión granular de términos en variables
    2. Gestión granular de condiciones en reglas
    3. Gestión granular de conectores en reglas
    4. Gestión granular de consecuentes en reglas
    """
    # Configuración de conexión a MongoDB
    conn_str = os.getenv("MONGO_CONNECTION_STRING") or os.getenv("FUZZY_MONGO_CONNECTION_STRING")
    if not conn_str:
        conn_str = "mongodb://localhost:27017"
    db_name = os.getenv("MONGO_DATABASE_NAME") or os.getenv("FUZZY_MONGO_DATABASE") or "fuzzy_test"

    # Configurar variables de entorno
    os.environ["MONGO_CONNECTION_STRING"] = conn_str
    os.environ["MONGO_DATABASE_NAME"] = db_name
    os.environ["MONGO_PING_ON_STARTUP"] = os.getenv("MONGO_PING_ON_STARTUP", "true")
    os.environ["FUZZY_ENSURE_INDEXES_ON_STARTUP"] = "true"

    # Recargar configuración de base de datos
    import FuzzyService.Infrastructure.Configuration.DatabaseConfiguration as db
    importlib.reload(db)

    # Importar la aplicación FastAPI
    import FuzzyService.Api.main as main
    importlib.reload(main)
    app = main.app

    # Nombres únicos para evitar colisiones
    unique_suffix = uuid4().hex[:8]
    system_name = f"Sistema Granular E2E {unique_suffix}"
    
    with TestClient(app) as client:
        # === SETUP: CREAR ENTIDADES BASE ===
        print("\n=== SETUP: CREANDO ENTIDADES BASE ===")
        
        # Crear sistema
        system_response = client.post(
            "/api/fuzzy-systems",
            json={"name": system_name, "isActive": False}
        )
        assert system_response.status_code == 201
        system_id = system_response.json()["id"]
        print(f"Sistema creado: {system_id}")
        
        # Crear variable de entrada
        var_response = client.post(
            "/api/fuzzy-variables",
            json={
                "name": f"Variable Granular {unique_suffix}",
                "variable_type": "input",
                "description": "Variable de entrada para test granular"
            }
        )
        assert var_response.status_code == 201
        variable_id = var_response.json()["id"]
        print(f"Variable creada: {variable_id}")
        
        # Crear segunda variable para reglas
        var2_response = client.post(
            "/api/fuzzy-variables",
            json={
                "name": f"Variable2 Granular {unique_suffix}",
                "variable_type": "input",
                "description": "Segunda variable de entrada para test granular"
            }
        )
        assert var2_response.status_code == 201
        variable2_id = var2_response.json()["id"]
        print(f"Variable2 creada: {variable2_id}")
        
        # Agregar variables al sistema
        system_update_response = client.put(
            f"/api/fuzzy-systems/{system_id}",
            json={
                "input_variable_ids": [variable_id, variable2_id]
            }
        )
        assert system_update_response.status_code == 200
        print(f"Variables agregadas al sistema: {variable_id}, {variable2_id}")
        
        # Crear rutina para reglas
        routine_response = client.post(
            "/api/fuzzy-routines",
            json={
                "routine_name": f"Rutina Granular {unique_suffix}",
                "steps": []
            }
        )
        assert routine_response.status_code == 201
        routine_id = routine_response.json()["id"]
        print(f"Rutina creada: {routine_id}")
        
        # Crear segunda rutina
        routine2_response = client.post(
            "/api/fuzzy-routines",
            json={
                "routine_name": f"Rutina2 Granular {unique_suffix}",
                "steps": []
            }
        )
        assert routine2_response.status_code == 201
        routine2_id = routine2_response.json()["id"]
        print(f"Rutina2 creada: {routine2_id}")
        
        # === 1. TEST GESTIÓN GRANULAR DE TÉRMINOS ===
        print("\n=== 1. TESTING GESTIÓN GRANULAR DE TÉRMINOS ===")
        
        # Primero crear un término
        term_response = client.post(
            "/api/fuzzy-terms",
            json={
                "variable_id": variable_id,
                "label": "Bajo",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 15.0, 30.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            }
        )
        assert term_response.status_code == 201
        term_id = term_response.json()["id"]
        print(f"Término creado: {term_id}")
        
        # Agregar término usando endpoint granular
        add_term_response = client.post(
            f"/api/fuzzy-variables/{variable_id}/terms",
            json={
                "variable_id": variable_id,
                "term_id": term_id
            }
        )
        assert add_term_response.status_code == 200, f"Error agregando término: {add_term_response.text}"
        var_with_term = add_term_response.json()
        assert len(var_with_term["terms"]) == 1
        added_term_id = var_with_term["terms"][0]  # terms contiene IDs como strings
        print(f"Término 'Bajo' agregado: {added_term_id}")
        
        # Crear segundo término
        term2_response = client.post(
            "/api/fuzzy-terms",
            json={
                "variable_id": variable_id,
                "label": "Alto",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [70.0, 85.0, 100.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            }
        )
        assert term2_response.status_code == 201
        term2_id = term2_response.json()["id"]
        
        # Agregar segundo término usando endpoint granular
        add_term2_response = client.post(
            f"/api/fuzzy-variables/{variable_id}/terms",
            json={
                "variable_id": variable_id,
                "term_id": term2_id
            }
        )
        assert add_term2_response.status_code == 200
        var_with_terms = add_term2_response.json()
        assert len(var_with_terms["terms"]) == 2
        added_term2_id = var_with_terms["terms"][1]  # terms contiene IDs como strings
        print(f"Término 'Alto' agregado: {added_term2_id}")
        
        # Verificar comportamiento al agregar término duplicado
        duplicate_term_response = client.post(
            f"/api/fuzzy-variables/{variable_id}/terms",
            json={
                "variable_id": variable_id,
                "term_id": added_term_id  # Intentar agregar el mismo término
            }
        )
        assert duplicate_term_response.status_code == 200  # El endpoint permite duplicados
        print("Término duplicado agregado (comportamiento actual)")
        
        # Remover término usando endpoint granular
        remove_term_response = client.delete(f"/api/fuzzy-variables/{variable_id}/terms/{added_term2_id}")
        assert remove_term_response.status_code == 200
        var_after_removal = remove_term_response.json()
        assert len(var_after_removal["terms"]) == 1
        assert var_after_removal["terms"][0] == added_term_id  # Solo queda el primer término
        print("Término 'Alto' removido exitosamente")
        
        # Agregar términos a la segunda variable para las reglas
        client.post(
            f"/api/fuzzy-variables/{variable2_id}/terms",
            params={"name": "Frio", "membershipFunction": "TRIANGULAR", "parameters": "0,0,20"}
        )
        client.post(
            f"/api/fuzzy-variables/{variable2_id}/terms",
            params={"name": "Caliente", "membershipFunction": "TRIANGULAR", "parameters": "30,50,50"}
        )
        
        # === 2. TEST GESTIÓN GRANULAR DE REGLAS ===
        print("\n=== 2. TESTING GESTIÓN GRANULAR DE REGLAS ===")
        
        # Crear regla base con una condición
        rule_response = client.post(
            "/api/fuzzy-rules",
            json={
                "name": f"Regla Granular {unique_suffix}",
                "system_id": system_id,
                "conditions": [
                    {"variable_id": variable_id, "operator": "IS", "value": "Bajo"}
                ],
                "connectors": [],
                "consequent": routine_id
            }
        )
        assert rule_response.status_code == 201
        rule_id = rule_response.json()["id"]
        print(f"Regla base creada: {rule_id}")
        
        # === 2.1 AGREGAR CONDICIÓN GRANULARMENTE ===
        add_condition_response = client.post(
            f"/api/fuzzy-rules/{rule_id}/conditions",
            params={
                "variable_id": variable2_id,
                "operator": "IS",
                "value": "Frio",
                "connector": "AND"
            }
        )
        assert add_condition_response.status_code == 201, f"Error agregando condición: {add_condition_response.text}"
        rule_with_condition = add_condition_response.json()
        assert len(rule_with_condition["conditions"]) == 2
        assert len(rule_with_condition["connectors"]) == 1
        assert rule_with_condition["connectors"][0] == "AND"
        print("Condición agregada exitosamente")
        
        # === 2.2 ACTUALIZAR CONECTORES GRANULARMENTE ===
        update_connectors_response = client.put(
            f"/api/fuzzy-rules/{rule_id}/connectors",
            json=["OR"]
        )
        assert update_connectors_response.status_code == 200, f"Error actualizando conectores: {update_connectors_response.text}"
        rule_with_new_connectors = update_connectors_response.json()
        assert rule_with_new_connectors["connectors"][0] == "OR"
        print("Conectores actualizados exitosamente")
        
        # === 2.3 ACTUALIZAR CONSECUENTE GRANULARMENTE ===
        update_consequent_response = client.put(
            f"/api/fuzzy-rules/{rule_id}/consequent",
            params={"consequent": routine2_id}
        )
        assert update_consequent_response.status_code == 200, f"Error actualizando consecuente: {update_consequent_response.text}"
        rule_with_new_consequent = update_consequent_response.json()
        assert rule_with_new_consequent["consequent"] == routine2_id
        print("Consecuente actualizado exitosamente")
        
        # === 2.4 REMOVER CONDICIÓN GRANULARMENTE ===
        remove_condition_response = client.delete(f"/api/fuzzy-rules/{rule_id}/conditions/{variable2_id}")
        assert remove_condition_response.status_code == 200, f"Error removiendo condición: {remove_condition_response.text}"
        rule_after_removal = remove_condition_response.json()
        assert len(rule_after_removal["conditions"]) == 1
        assert len(rule_after_removal["connectors"]) == 0  # Sin conectores cuando hay una sola condición
        print("Condición removida exitosamente")
        
        # === 3. TEST VALIDACIONES DE ERRORES ===
        print("\n=== 3. TESTING VALIDACIONES DE ERRORES ===")
        
        # Intentar agregar condición con variable inexistente
        invalid_var_response = client.post(
            f"/api/fuzzy-rules/{rule_id}/conditions",
            params={
                "variable_id": "507f1f77bcf86cd799439011",  # ObjectId válido pero inexistente
                "operator": "IS",
                "value": "Test",
                "connector": "AND"
            }
        )
        assert invalid_var_response.status_code == 404
        print("Validación de variable inexistente funcionando")
        
        # Intentar agregar condición duplicada
        duplicate_condition_response = client.post(
            f"/api/fuzzy-rules/{rule_id}/conditions",
            params={
                "variable_id": variable_id,  # Ya existe una condición para esta variable
                "operator": "IS",
                "value": "Bajo"
            }
        )
        assert duplicate_condition_response.status_code == 400
        print("Validación de condición duplicada funcionando")
        
        # Intentar actualizar conectores con cantidad incorrecta
        invalid_connectors_response = client.put(
            f"/api/fuzzy-rules/{rule_id}/connectors",
            json=["AND", "OR"]  # Demasiados conectores para una sola condición
        )
        assert invalid_connectors_response.status_code == 400
        print("Validación de conectores incorrectos funcionando")
        
        # Intentar actualizar consecuente con rutina inexistente
        invalid_consequent_response = client.put(
            f"/api/fuzzy-rules/{rule_id}/consequent",
            params={"consequent": "507f1f77bcf86cd799439012"}  # ObjectId válido pero inexistente
        )
        assert invalid_consequent_response.status_code == 404
        print("Validación de rutina inexistente funcionando")
        
        # Intentar remover término que está siendo usado en reglas
        remove_used_term_response = client.delete(f"/api/fuzzy-variables/{variable_id}/terms/{term_id}")
        assert remove_used_term_response.status_code == 400
        print("Validación de término en uso funcionando")
        
        # === 4. LIMPIEZA ===
        print("\n=== 4. LIMPIEZA ===")
        
        # Eliminar sistema (esto debería eliminar en cascada)
        delete_response = client.delete(f"/api/fuzzy-systems/{system_id}")
        assert delete_response.status_code == 200
        print("Sistema eliminado exitosamente")
        
        print("\n=== TEST ENDPOINTS GRANULARES EXITOSO ===")


if __name__ == "__main__":
    test_granular_endpoints_workflow()