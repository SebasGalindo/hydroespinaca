import os
import importlib
from uuid import uuid4
from fastapi.testclient import TestClient
from dotenv import load_dotenv

# Cargar variables desde .env.test para tests E2E
load_dotenv(dotenv_path=".env.test")


def test_complete_fuzzy_system_workflow():
    """
    Test E2E completo que verifica todo el flujo del sistema fuzzy:
    1. Crear sistema fuzzy
    2. Crear variables de entrada y salida
    3. Agregar términos a las variables
    4. Crear reglas fuzzy con condiciones
    5. Crear rutinas fuzzy con pasos
    6. Realizar evaluación fuzzy
    7. Verificar resultados y limpieza
    """
    # La configuración de MongoDB ya está cargada desde .env.test
    # Verificar que estamos usando la base de datos de test
    db_name = os.getenv("MONGO_DATABASE_NAME") or os.getenv("FUZZY_MONGO_DATABASE")
    assert db_name == "HydroEspinacaTest", f"Expected test database 'HydroEspinacaTest', got '{db_name}'"
    
    print(f"Using test database: {db_name}")

    # Recargar configuración de base de datos
    import FuzzyService.Infrastructure.Configuration.DatabaseConfiguration as db
    importlib.reload(db)

    # Importar la aplicación FastAPI
    import FuzzyService.Api.main as main
    importlib.reload(main)
    app = main.app

    # Nombres únicos para evitar colisiones en tests
    unique_suffix = uuid4().hex[:8]
    system_name = f"Sistema Hidroponico E2E {unique_suffix}"
    
    with TestClient(app) as client:
        # === 1. CREAR SISTEMA FUZZY ===
        print("\n=== 1. CREANDO SISTEMA FUZZY ===")
        system_response = client.post(
            "/api/fuzzy-systems",
            json={"name": system_name, "isActive": False}
        )
        assert system_response.status_code == 201, f"Error creando sistema: {system_response.text}"
        system_data = system_response.json()
        system_id = system_data["id"]
        print(f"Sistema creado: {system_id}")
        
        # === 2. CREAR VARIABLES DE ENTRADA Y SALIDA ===
        print("\n=== 2. CREANDO VARIABLES ===")
        
        # Variable de entrada: Temperatura
        temp_response = client.post(
            "/api/fuzzy-variables",
            json={
                "name": f"Temperatura {unique_suffix}",
                "variable_type": "input",
                "description": "Variable de temperatura del aire"
            }
        )
        assert temp_response.status_code == 201, f"Error creando variable temperatura: {temp_response.text}"
        temp_var = temp_response.json()
        temp_var_id = temp_var["id"]
        print(f"Variable Temperatura creada: {temp_var_id}")
        
        # Variable de entrada: Humedad
        humidity_response = client.post(
            "/api/fuzzy-variables",
            json={
                "name": f"Humedad {unique_suffix}",
                "variable_type": "input",
                "description": "Variable de humedad del aire"
            }
        )
        assert humidity_response.status_code == 201, f"Error creando variable humedad: {humidity_response.text}"
        humidity_var = humidity_response.json()
        humidity_var_id = humidity_var["id"]
        print(f"Variable Humedad creada: {humidity_var_id}")
        
        # Variable de salida: Riego
        irrigation_response = client.post(
            "/api/fuzzy-variables",
            json={
                "name": f"Riego {unique_suffix}",
                "variable_type": "output",
                "description": "Variable de control de riego"
            }
        )
        assert irrigation_response.status_code == 201, f"Error creando variable riego: {irrigation_response.text}"
        irrigation_var = irrigation_response.json()
        irrigation_var_id = irrigation_var["id"]
        print(f"Variable Riego creada: {irrigation_var_id}")
        
        # Actualizar el sistema con las variables creadas
        update_system_response = client.put(
            f"/api/fuzzy-systems/{system_id}",
            json={
                "input_variable_ids": [temp_var_id, humidity_var_id],
                "output_variable_ids": [irrigation_var_id]
            }
        )
        assert update_system_response.status_code == 200, f"Error actualizando sistema con variables: {update_system_response.text}"
        print("Sistema actualizado con variables")
        
        # === 3. AGREGAR TÉRMINOS A LAS VARIABLES ===
        print("\n=== 3. AGREGANDO TÉRMINOS ===")
        
        # Términos para Temperatura: Fría, Normal, Caliente
        temp_terms = [
            {"name": "Fria", "membershipFunction": "TRIANGULAR", "parameters": [0, 0, 20]},
            {"name": "Normal", "membershipFunction": "TRIANGULAR", "parameters": [15, 25, 35]},
            {"name": "Caliente", "membershipFunction": "TRIANGULAR", "parameters": [30, 50, 50]}
        ]
        
        temp_term_ids = []
        for term in temp_terms:
            # Crear el término primero
            create_term_response = client.post(
                "/api/fuzzy-terms",
                json={
                    "variable_id": temp_var_id,
                    "label": term["name"],
                    "membership_function": {
                        "function_type": term["membershipFunction"].lower(),
                        "parameters": term["parameters"],
                        "universe_min": 0.0,
                        "universe_max": 50.0
                    }
                }
            )
            assert create_term_response.status_code == 201, f"Error creando término {term['name']}: {create_term_response.text}"
            term_data = create_term_response.json()
            temp_term_ids.append(term_data["id"])
            print(f"Término {term['name']} creado para Temperatura")
        
        # Términos para Humedad: Baja, Media, Alta
        humidity_terms = [
            {"name": "Baja", "membershipFunction": "TRIANGULAR", "parameters": [0, 0, 40]},
            {"name": "Media", "membershipFunction": "TRIANGULAR", "parameters": [30, 50, 70]},
            {"name": "Alta", "membershipFunction": "TRIANGULAR", "parameters": [60, 100, 100]}
        ]
        
        humidity_term_ids = []
        for term in humidity_terms:
            # Crear el término primero
            create_term_response = client.post(
                "/api/fuzzy-terms",
                json={
                    "variable_id": humidity_var_id,
                    "label": term["name"],
                    "membership_function": {
                        "function_type": term["membershipFunction"].lower(),
                        "parameters": term["parameters"],
                        "universe_min": 0.0,
                        "universe_max": 100.0
                    }
                }
            )
            assert create_term_response.status_code == 201, f"Error creando término {term['name']}: {create_term_response.text}"
            term_data = create_term_response.json()
            humidity_term_ids.append(term_data["id"])
            print(f"Término {term['name']} creado para Humedad")
        
        # Términos para Riego: Poco, Moderado, Mucho
        irrigation_terms = [
            {"name": "Poco", "membershipFunction": "TRIANGULAR", "parameters": [0, 0, 30]},
            {"name": "Moderado", "membershipFunction": "TRIANGULAR", "parameters": [20, 50, 80]},
            {"name": "Mucho", "membershipFunction": "TRIANGULAR", "parameters": [70, 100, 100]}
        ]
        
        irrigation_term_ids = []
        for term in irrigation_terms:
            # Crear el término primero
            create_term_response = client.post(
                "/api/fuzzy-terms",
                json={
                    "variable_id": irrigation_var_id,
                    "label": term["name"],
                    "membership_function": {
                        "function_type": term["membershipFunction"].lower(),
                        "parameters": term["parameters"],
                        "universe_min": 0.0,
                        "universe_max": 100.0
                    }
                }
            )
            assert create_term_response.status_code == 201, f"Error creando término {term['name']}: {create_term_response.text}"
            term_data = create_term_response.json()
            irrigation_term_ids.append(term_data["id"])
            print(f"Término {term['name']} creado para Riego")
        
        # === 4. CREAR RUTINAS FUZZY ===
        print("\n=== 4. CREANDO RUTINAS FUZZY ===")
        
        # Rutina para riego poco
        routine_poco_response = client.post(
            "/api/fuzzy-routines",
            json={
                "routine_name": f"Riego Poco {unique_suffix}",
                "steps": []
            }
        )
        assert routine_poco_response.status_code == 201, f"Error creando rutina poco: {routine_poco_response.text}"
        routine_poco = routine_poco_response.json()
        routine_poco_id = routine_poco["id"]
        print(f"Rutina Riego Poco creada: {routine_poco_id}")
        
        # Rutina para riego moderado
        routine_moderado_response = client.post(
            "/api/fuzzy-routines",
            json={
                "routine_name": f"Riego Moderado {unique_suffix}",
                "steps": []
            }
        )
        assert routine_moderado_response.status_code == 201, f"Error creando rutina moderado: {routine_moderado_response.text}"
        routine_moderado = routine_moderado_response.json()
        routine_moderado_id = routine_moderado["id"]
        print(f"Rutina Riego Moderado creada: {routine_moderado_id}")
        
        # Rutina para riego mucho
        routine_mucho_response = client.post(
            "/api/fuzzy-routines",
            json={
                "routine_name": f"Riego Mucho {unique_suffix}",
                "steps": []
            }
        )
        assert routine_mucho_response.status_code == 201, f"Error creando rutina mucho: {routine_mucho_response.text}"
        routine_mucho = routine_mucho_response.json()
        routine_mucho_id = routine_mucho["id"]
        print(f"Rutina Riego Mucho creada: {routine_mucho_id}")
        
        # === 5. CREAR REGLAS FUZZY ===
        print("\n=== 5. CREANDO REGLAS FUZZY ===")
        
        # Regla 1: Si Temperatura es Caliente Y Humedad es Baja → Riego Mucho
        rule1_response = client.post(
            "/api/fuzzy-rules",
            json={
                "name": f"Regla Caliente-Baja {unique_suffix}",
                "system_id": system_id,
                "conditions": [
                    {"variable_id": temp_var_id, "operator": "IS", "value": "Caliente"},
                    {"variable_id": humidity_var_id, "operator": "IS", "value": "Baja"}
                ],
                "connectors": ["AND"],
                "consequent": routine_mucho_id
            }
        )
        assert rule1_response.status_code == 201, f"Error creando regla 1: {rule1_response.text}"
        rule1 = rule1_response.json()
        print(f"Regla 1 creada: {rule1['id']}")
        
        # Regla 2: Si Temperatura es Normal Y Humedad es Media → Riego Moderado
        rule2_response = client.post(
            "/api/fuzzy-rules",
            json={
                "name": f"Regla Normal-Media {unique_suffix}",
                "system_id": system_id,
                "conditions": [
                    {"variable_id": temp_var_id, "operator": "IS", "value": "Normal"},
                    {"variable_id": humidity_var_id, "operator": "IS", "value": "Media"}
                ],
                "connectors": ["AND"],
                "consequent": routine_moderado_id
            }
        )
        assert rule2_response.status_code == 201, f"Error creando regla 2: {rule2_response.text}"
        rule2 = rule2_response.json()
        print(f"Regla 2 creada: {rule2['id']}")
        
        # Regla 3: Si Temperatura es Fría O Humedad es Alta → Riego Poco
        rule3_response = client.post(
            "/api/fuzzy-rules",
            json={
                "name": f"Regla Fria-Alta {unique_suffix}",
                "system_id": system_id,
                "conditions": [
                    {"variable_id": temp_var_id, "operator": "IS", "value": "Fria"},
                    {"variable_id": humidity_var_id, "operator": "IS", "value": "Alta"}
                ],
                "connectors": ["OR"],
                "consequent": routine_poco_id
            }
        )
        assert rule3_response.status_code == 201, f"Error creando regla 3: {rule3_response.text}"
        rule3 = rule3_response.json()
        print(f"Regla 3 creada: {rule3['id']}")
        
        # === 6. ACTIVAR SISTEMA ===
        print("\n=== 6. ACTIVANDO SISTEMA ===")
        activate_response = client.patch(
            f"/api/fuzzy-systems/{system_id}/status",
            json={"status": "ACTIVE"}
        )
        assert activate_response.status_code == 200, f"Error activando sistema: {activate_response.text}"
        print("Sistema activado exitosamente")
        
        # === 7. VERIFICAR ENDPOINTS DE EVALUACIÓN ===
        print("\n=== 7. VERIFICANDO ENDPOINTS DE EVALUACIÓN ===")
        
        # Nota: Las evaluaciones fuzzy se ejecutan automáticamente vía MQTT,
        # no mediante endpoint POST. Aquí verificamos que los endpoints de consulta funcionen.
        
        # Verificar endpoint de historial de evaluaciones
        all_evals_response = client.get("/api/fuzzy-evaluations")
        assert all_evals_response.status_code == 200, f"Error obteniendo historial: {all_evals_response.text}"
        all_evals = all_evals_response.json()
        print(f"Endpoint de historial funcional - Total evaluaciones: {all_evals['total_count']}")
        
        # Verificar endpoint de evaluaciones por sistema
        system_evals_response = client.get(f"/api/fuzzy-evaluations/system/{system_id}")
        assert system_evals_response.status_code == 200, f"Error obteniendo evaluaciones del sistema: {system_evals_response.text}"
        system_evals = system_evals_response.json()
        print(f"Endpoint de evaluaciones por sistema funcional - Evaluaciones del sistema: {len(system_evals['evaluations'])}")
        
        # Verificar endpoint de evaluaciones recientes
        recent_evals_response = client.get("/api/fuzzy-evaluations/recent")
        assert recent_evals_response.status_code == 200, f"Error obteniendo evaluaciones recientes: {recent_evals_response.text}"
        recent_evals = recent_evals_response.json()
        print(f"Endpoint de evaluaciones recientes funcional - Evaluaciones recientes: {len(recent_evals['evaluations'])}")
        
        # Verificar que el sistema tiene las variables correctas
        system_get_response = client.get(f"/api/fuzzy-systems/{system_id}")
        assert system_get_response.status_code == 200
        system_details = system_get_response.json()
        assert len(system_details["input_variable_ids"]) == 2  # Temperatura y Humedad
        assert len(system_details["output_variable_ids"]) == 1  # Riego
        print("Variables del sistema verificadas")
        
        print("\n=== ✅ WORKFLOW COMPLETO EXITOSO ===")
        print(f"Sistema fuzzy '{system_name}' creado y configurado completamente")
        print(f"- Sistema ID: {system_id}")
        print(f"- Variables: {len([temp_var_id, humidity_var_id, irrigation_var_id])}")
        print(f"- Términos: 9 (3 por variable)")
        print(f"- Rutinas: 3")
        print(f"- Reglas: 3")
        print(f"- Endpoints de evaluación verificados y funcionales")
        print(f"\nNota: Las evaluaciones fuzzy se ejecutan automáticamente cuando se reciben datos vía MQTT.")
        
        # === 8. LIMPIEZA ===
        print("\n=== 8. LIMPIEZA ===")
        
        # Eliminar sistema (esto debería eliminar en cascada)
        delete_response = client.delete(f"/api/fuzzy-systems/{system_id}")
        assert delete_response.status_code == 200, f"Error eliminando sistema: {delete_response.text}"
        print("Sistema eliminado exitosamente")
        
        # Verificar que el sistema ya no existe
        get_deleted_response = client.get(f"/api/fuzzy-systems/{system_id}")
        assert get_deleted_response.status_code == 404
        print("Eliminación verificada")
        
        print("\n=== TEST E2E COMPLETO EXITOSO ===")


if __name__ == "__main__":
    test_complete_fuzzy_system_workflow()