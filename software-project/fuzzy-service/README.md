# Fuzzy Service - Motor de Lógica Difusa

> Evalúa datos de sensores mediante lógica difusa Mamdani y genera comandos consolidados para actuadores

## 📋 Descripción

El **fuzzy-service** centraliza toda la lógica difusa (fuzzy logic) del sistema HydroEspinaca. Procesa lecturas de sensores, evalúa reglas mediante inferencia Mamdani y genera comandos normalizados para actuadores:

- **Entradas**: Variables de sensores (luminosidad, temperatura ambiente, temperatura del agua, humedad, nivel de agua)
- **Reglas**: Condiciones con conectores (AND/OR) y consecuentes que mapean a variables de salida y términos lingüísticos
- **Salidas**: Comandos por actuador con duty cycle (PWM) o power (DIGITAL) más duración en segundos

Expone APIs REST completas para sistemas, variables, términos, reglas e historial de evaluaciones. Se comunica con otros servicios mediante M2M JWT.

## 🏗️ Arquitectura

```
FuzzyService/
├── Api/                        # FastAPI app, routers, middleware, health endpoint
│   ├── Controllers/           # REST endpoints por recurso
│   ├── Configuration/         # CORS, OpenAPI metadata
│   └── Middleware/            # ErrorHandlingMiddleware
├── Application/               # CQRS Commands/Queries/Handlers
│   ├── Features/             # Organizados por agregado (Systems, Variables, Rules, etc.)
│   ├── Services/             # CommandAggregator (consolidación de outputs)
│   └── Configuration/        # DI de handlers con Medyator
├── Domain/                    # Entidades, Value Objects, Interfaces
│   ├── Entities/             # FuzzySystem, FuzzyVariable, FuzzyRule, FuzzyEvaluation
│   ├── Interfaces/           # Repositorios, IFuzzyEngine, servicios externos
│   ├── Enums/                # Status, tipos de actuador, métodos de defuzzificación
│   └── ValueObjects/         # DomainId, MembershipFunction, OperatorsConfig
└── Infrastructure/            # Implementaciones concretas
    ├── Persistence/          # Repositorios MongoDB (PyMongo async)
    ├── ExternalServices/     # FuzzyEngine (scikit-fuzzy), ActuatorService, SensorService, MQTT
    ├── Authentication/       # JWT/JWKS validation, M2M token management
    └── Configuration/        # Database, DI, FeatureFlags, SeedData
```

**Arquitectura**: Clean Architecture + CQRS (Medyator) + Dependency Injection (kink)

**Flujo principal**:
1. `ProcessSensorReadingsHandler` recibe lecturas de sensores
2. `IFuzzyEngine.complete_fuzzy_evaluation()`:
   - Fuzzificación (grados de pertenencia con scikit-fuzzy)
   - Evaluación de reglas (firing strength por regla)
   - Defuzzificación Mamdani desde consecuentes (agregación multi-regla)
3. `CommandAggregator` consolida salidas por actuador (max/sum/OR)
4. POST a `actuator-service` `/api/commands/execute` con payload de comandos

## 🔌 Dependencias

### Base de Datos
- **MongoDB**: Colecciones principales
  - `fuzzy_systems` - Sistemas difusos (ACTIVE/INACTIVE)
  - `fuzzy_variables` - Variables de entrada/salida con reference_code
  - `fuzzy_terms` - Términos lingüísticos con funciones de membresía
  - `fuzzy_rules` - Reglas con condiciones/conectores/consecuentes
  - `fuzzy_evaluations` - Historial de evaluaciones

### Microservicios
- **auth-service**: 
  - JWKS para validación JWT (GET `/api/auth/keys/public`)
  - Emisión de tokens M2M (POST `/api/auth/token`)
- **actuator-service**: 
  - Ejecución de comandos (POST `/api/commands/execute`)
- **sensor-service** (opcional): 
  - Validaciones/metadata de sensores

### Mensajería
- **MQTT** (opcional): Subscriber para trigger automático de `ProcessSensorReadings`

## 📡 Endpoints

### Sistemas Fuzzy
```
GET    /api/fuzzy-systems                    # Listar sistemas
GET    /api/fuzzy-systems/{id}               # Obtener por ID
POST   /api/fuzzy-systems                    # Crear sistema
PUT    /api/fuzzy-systems/{id}               # Actualizar sistema
DELETE /api/fuzzy-systems/{id}               # Eliminar sistema
```

### Variables
```
GET    /api/fuzzy-variables                  # Listar variables
GET    /api/fuzzy-variables/{id}             # Obtener por ID
POST   /api/fuzzy-variables                  # Crear variable
PUT    /api/fuzzy-variables/{id}             # Actualizar variable
DELETE /api/fuzzy-variables/{id}             # Eliminar variable
GET    /api/fuzzy-variables/system/{id}      # Variables de un sistema
```

### Términos
```
GET    /api/fuzzy-terms                      # Listar términos
GET    /api/fuzzy-terms/{id}                 # Obtener por ID
POST   /api/fuzzy-terms                      # Crear término
PUT    /api/fuzzy-terms/{id}                 # Actualizar término
DELETE /api/fuzzy-terms/{id}                 # Eliminar término
```

### Reglas
```
GET    /api/fuzzy-rules[?system_id=...]      # Listar reglas (filtrado opcional)
GET    /api/fuzzy-rules/{id}                 # Obtener por ID
POST   /api/fuzzy-rules                      # Crear regla
PUT    /api/fuzzy-rules/{id}                 # Actualizar regla completa
DELETE /api/fuzzy-rules/{id}                 # Eliminar regla

# Endpoints granulares
POST   /api/fuzzy-rules/{id}/conditions      # Agregar condición
DELETE /api/fuzzy-rules/{id}/conditions/{variable_id}  # Remover condición
PUT    /api/fuzzy-rules/{id}/connectors      # Actualizar conectores
PUT    /api/fuzzy-rules/{id}/consequent      # Actualizar consecuente (legacy)
```

**Ejemplo de payload para crear regla**:
```json
{
  "name": "Regla de Ventilación",
  "system_id": "67354a1b2c3d4e5f6a7b8c9d",
  "description": "Si temperatura alta, activar ventilador",
  "conditions": [
    {
      "variable_id": "67354a1b2c3d4e5f6a7b8c9e",
      "operator": "IS",
      "value": "calorAmbiental"
    }
  ],
  "connectors": [],
  "consequents": [
    {
      "variable_id": "67354a1b2c3d4e5f6a7b8c9f",
      "terms": ["67354a1b2c3d4e5f6a7b8ca0", "67354a1b2c3d4e5f6a7b8ca1"],
      "aggregation_method": "max"
    }
  ]
}
```

### Evaluaciones
```
POST   /api/fuzzy-evaluations                # Crear evaluación manual
GET    /api/fuzzy-evaluations                # Listar con paginación y filtros
GET    /api/fuzzy-evaluations/recent         # Evaluaciones recientes (últimas 24h)
GET    /api/fuzzy-evaluations/system/{id}    # Por sistema
GET    /api/fuzzy-evaluations/{id}           # Obtener por ID
GET    /api/fuzzy-evaluations/stats/summary  # Estadísticas agregadas
```

### Health
```
GET    /health                               # Health check (sin autenticación)
```

## ⚙️ Configuración

### Variables de Entorno Principales

**MongoDB**:
```bash
MONGO_CONNECTION_STRING=mongodb://localhost:27017
MONGO_DATABASE=fuzzy_dev
MONGO_PING_ON_STARTUP=true
```

**JWT/JWKS**:
```bash
JWT_ISSUER=http://auth-service:8080
JWT_AUDIENCE=hydroespinaca-services
# JWKS se construye automáticamente: {M2M_AUTH_SERVICE_URL}/api/auth/keys/public
```

**M2M (Machine-to-Machine)**:
```bash
M2M_CLIENT_ID=fuzzy-service
M2M_CLIENT_SECRET=your-secret-here
M2M_AUTH_SERVICE_URL=http://auth-service:8080
M2M_TOKEN_ENDPOINT=/api/auth/token
```

**Actuadores**:
```bash
ACTUATOR_SERVICE_URL=http://actuator-service:8080
```

**Logging**:
```bash
ASPNETCORE_ENVIRONMENT=Development|Production
# Development: logging DEBUG, Production: logging INFO
```

## 🚀 Ejecución

### Desarrollo Local

```bash
# Instalar dependencias
pip install -r software-project/fuzzy-service/requirements.txt

# Ejecutar API con hot-reload
uvicorn FuzzyService.Api.main:app --reload --host 0.0.0.0 --port 8000

# Swagger UI disponible en:
# http://localhost:8000/docs
```

### Docker

```bash
# Build desde raíz del proyecto
docker build -t fuzzy-service:latest -f software-project/fuzzy-service/Dockerfile .

# Run con variables de entorno
docker run -p 8000:8000 \
  -e MONGO_CONNECTION_STRING=mongodb://mongo:27017 \
  -e M2M_CLIENT_ID=fuzzy-service \
  -e M2M_CLIENT_SECRET=your-secret \
  -e M2M_AUTH_SERVICE_URL=http://auth-service:8080 \
  -e ACTUATOR_SERVICE_URL=http://actuator-service:8080 \
  fuzzy-service:latest
```

### Docker Compose

Ver `/docker-compose.yml` en la raíz del proyecto.

## 🔐 Autenticación

**JWT Bearer tokens** con validación via JWKS y soporte M2M.

### Scopes por Recurso

| Scope | Descripción |
|-------|-------------|
| `fuzzy:system:read` | Lectura de sistemas |
| `fuzzy:system:create` | Creación de sistemas |
| `fuzzy:system:update` | Actualización de sistemas |
| `fuzzy:system:delete` | Eliminación de sistemas |
| `fuzzy:variable:read` | Lectura de variables |
| `fuzzy:variable:create` | Creación de variables |
| `fuzzy:variable:update` | Actualización de variables |
| `fuzzy:variable:delete` | Eliminación de variables |
| `fuzzy:rule:read` | Lectura de reglas |
| `fuzzy:rule:create` | Creación de reglas |
| `fuzzy:rule:update` | Actualización de reglas |
| `fuzzy:rule:delete` | Eliminación de reglas |
| `fuzzy:evaluation:read` | Lectura de evaluaciones |
| `fuzzy:evaluation:create` | Creación de evaluaciones |
| `sensor:process` | Procesamiento de sensores (M2M) |
| `system:admin` | Bypass de todos los scopes |

**Nota**: Los endpoints de `/health` no requieren autenticación.

## 🧪 Testing

```bash
# Unit tests (recomendado con pytest)
pytest software-project/fuzzy-service/tests/

# Tests de integración contra MongoDB de prueba
MONGO_CONNECTION_STRING=mongodb://localhost:27017 \
MONGO_DATABASE=fuzzy_test \
pytest software-project/fuzzy-service/tests/integration/
```

## 📝 Notas Importantes

### Modelo Mamdani y Consecuentes
- **Modelo oficial**: Mamdani con `consequents` (array de `RuleConsequent`)
- Cada regla puede tener múltiples consecuentes (una salida por variable)
- Agregación multi-regla: múltiples reglas pueden contribuir a la misma variable de salida
- Métodos de agregación: `max` (por defecto), `sum`, `probabilistic_or`

### reference_code vs reference_id
- **`reference_code`** es el identificador estable para vincular variables fuzzy con sensores/actuadores
- Evitar usar `reference_id` (campo legacy en proceso de migración)
- Los mapeos sensor→variable y variable→actuador usan `reference_code`

### Tipos de Actuadores
- **PWM**: Salida continua 0-100 (duty cycle) - ej: ventiladores con control de velocidad
- **DIGITAL**: Salida ON/OFF basada en `defuzzification_threshold` (default: 50.0)
  - `crisp_value >= threshold` → `power: "ON"`
  - `crisp_value < threshold` → `power: "OFF"`

### Duración de Comandos
- Las variables de duración (ej: "Duración de Ventilación") se defuzzifican independientemente
- El motor asocia automáticamente duraciones a comandos de control según convenciones de nombres
- Valor mínimo de seguridad: 0.5 segundos
- Valor máximo: 10,000 segundos (configurable por variable)

### Seed Data
- El servicio incluye seed data completo en `Infrastructure/Configuration/SeedData.py`
- Al iniciar con `FeatureFlags.ENABLE_SEEDING=True`, crea:
  - 1 sistema "Hydroponic Control System" (ACTIVE)
  - 5 variables de entrada (T_AMB, HUM, LUMINOSITY, T_WAT, WL)
  - 13 variables de salida (control + duración por actuador)
  - ~80 términos lingüísticos con funciones de membresía
  - 11 reglas de control (ventilación, calefacción, iluminación, etc.)

### Producción
- **M2M tokens se cachean**: Verificar sincronización de reloj (TZ) entre contenedores
- **Health endpoint**: En Development retorna detalles de DB; en Production solo estado
- **Indexes MongoDB**: Se crean automáticamente en startup si `FeatureFlags.ENSURE_INDEXES=True`
- **Idempotencia**: Seed data usa IDs predefinidos; re-run es seguro (no duplica)
- **Concurrencia**: AsyncMongoClient con pool configurable (default: max_pool_size=20)

### Logging
- Ajuste granular por entorno (Development=DEBUG, Production=INFO)
- Loggers ruidosos silenciados: pymongo, motor, httpx, httpcore (WARNING)
- Logs estructurados en ProcessSensorReadingsHandler para debugging de flujo

---

**Stack**: Python 3.12, FastAPI 0.116.1, MongoDB (PyMongo async), scikit-fuzzy, numpy, Pydantic v2, Medyator, kink  
**Última actualización**: 2025-10-21