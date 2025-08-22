# API Documentation - Fuzzy Service

## 🔗 Base URL

```
http://localhost:8003
```

## 🔐 Autenticación

Todos los endpoints (excepto health checks) requieren autenticación JWT:

```http
Authorization: Bearer <jwt_token>
```

### Scopes Requeridos

- `fuzzy.read`: Lectura de configuración
- `fuzzy.write`: Modificación de configuración  
- `fuzzy.execute`: Ejecución de simulaciones
- `admin`: Acceso completo (suple todos los scopes)

## 📋 Health Checks

### GET /healthz

Verificación básica de salud del servicio.

**Respuesta:**
```json
{
  "status": "healthy",
  "service": "fuzzy-service"
}
```

### GET /readyz

Verificación de preparación con dependencias.

**Respuesta (Exitosa):**
```json
{
  "status": "ready",
  "service": "fuzzy-service",
  "checks": {
    "mongodb": {
      "status": "healthy",
      "message": "Connected"
    }
  }
}
```

**Respuesta (Error - 503):**
```json
{
  "status": "not_ready",
  "service": "fuzzy-service",
  "checks": {
    "mongodb": {
      "status": "unhealthy",
      "message": "Connection timeout"
    }
  }
}
```

## 🏗️ Sistemas Fuzzy

### POST /api/fuzzy/systems

**Scope:** `fuzzy.write`

Crea un nuevo sistema fuzzy.

**Request:**
```json
{
  "name": "Sistema Hidropónico Principal",
  "description": "Control de temperatura y humedad",
  "version": "1.0.0",
  "status": "draft"
}
```

**Response (201):**
```json
{
  "id": "507f1f77bcf86cd799439011",
  "message": "System created successfully"
}
```

### GET /api/fuzzy/systems

**Scope:** `fuzzy.read`

Lista todos los sistemas fuzzy.

**Query Parameters:**
- `name` (opcional): Filtrar por nombre
- `status` (opcional): Filtrar por estado (`draft`, `published`)
- `version` (opcional): Filtrar por versión

**Response:**
```json
[
  {
    "id": "507f1f77bcf86cd799439011",
    "name": "Sistema Hidropónico Principal",
    "description": "Control de temperatura y humedad",
    "version": "1.0.0",
    "status": "published",
    "created_at": "2024-01-15T10:30:00Z",
    "updated_at": "2024-01-15T11:00:00Z"
  }
]
```

### GET /api/fuzzy/systems/{system_id}

**Scope:** `fuzzy.read`

Obtiene detalles de un sistema específico.

**Response:**
```json
{
  "id": "507f1f77bcf86cd799439011",
  "name": "Sistema Hidropónico Principal",
  "description": "Control de temperatura y humedad",
  "version": "1.0.0",
  "status": "published",
  "variables": [
    {
      "id": "507f1f77bcf86cd799439012",
      "name": "temperatura_aire",
      "type": "input"
    }
  ],
  "rules": [
    {
      "id": "507f1f77bcf86cd799439013",
      "name": "enfriar_cuando_calor"
    }
  ],
  "created_at": "2024-01-15T10:30:00Z",
  "updated_at": "2024-01-15T11:00:00Z"
}
```

### PUT /api/fuzzy/systems/{system_id}

**Scope:** `fuzzy.write`

Actualiza un sistema (solo si está en estado `draft`).

**Request:**
```json
{
  "name": "Sistema Hidropónico Actualizado",
  "description": "Control mejorado de temperatura y humedad",
  "version": "1.1.0"
}
```

### POST /api/fuzzy/systems/{system_id}/publish

**Scope:** `fuzzy.write`

Publica una versión del sistema.

**Response:**
```json
{
  "message": "System published successfully",
  "version": "1.0.0",
  "published_at": "2024-01-15T12:00:00Z"
}
```

### POST /api/fuzzy/systems/{system_id}/export

**Scope:** `fuzzy.read`

Exporta el sistema como paquete JSON.

**Response:**
```json
{
  "system": {
    "name": "Sistema Hidropónico Principal",
    "version": "1.0.0",
    "variables": [...],
    "rules": [...],
    "routines": [...]
  },
  "exported_at": "2024-01-15T12:00:00Z",
  "format_version": "1.0"
}
```

### POST /api/fuzzy/systems/import

**Scope:** `fuzzy.write`

Importa un paquete de sistema.

**Request:**
```json
{
  "package": {
    "system": {
      "name": "Sistema Importado",
      "version": "1.0.0",
      "variables": [...],
      "rules": [...]
    }
  }
}
```

## 📊 Variables Fuzzy

### POST /api/fuzzy/variables

**Scope:** `fuzzy.write`

Crea una nueva variable fuzzy.

**Request:**
```json
{
  "name": "temperatura_aire",
  "description": "Temperatura del aire en grados Celsius",
  "min_value": 0.0,
  "max_value": 50.0,
  "unit": "°C",
  "type": "input",
  "system_id": "507f1f77bcf86cd799439011"
}
```

**Response (201):**
```json
{
  "id": "507f1f77bcf86cd799439012",
  "message": "Variable created successfully"
}
```

### GET /api/fuzzy/variables

**Scope:** `fuzzy.read`

Lista todas las variables fuzzy.

**Query Parameters:**
- `system_id` (opcional): Filtrar por sistema
- `type` (opcional): Filtrar por tipo (`input`, `output`)

**Response:**
```json
[
  {
    "id": "507f1f77bcf86cd799439012",
    "name": "temperatura_aire",
    "description": "Temperatura del aire en grados Celsius",
    "min_value": 0.0,
    "max_value": 50.0,
    "unit": "°C",
    "type": "input",
    "system_id": "507f1f77bcf86cd799439011",
    "terms": [
      {
        "id": "507f1f77bcf86cd799439014",
        "name": "baja",
        "membership_function": {
          "type": "trapezoidal",
          "parameters": [0, 0, 15, 20]
        }
      }
    ]
  }
]
```

### GET /api/fuzzy/variables/{variable_id}

**Scope:** `fuzzy.read`

Obtiene detalles de una variable específica.

### PUT /api/fuzzy/variables/{variable_id}

**Scope:** `fuzzy.write`

Actualiza una variable fuzzy.

### DELETE /api/fuzzy/variables/{variable_id}

**Scope:** `fuzzy.write`

Elimina una variable fuzzy.

## 🏷️ Términos Lingüísticos

### POST /api/fuzzy/terms

**Scope:** `fuzzy.write`

Crea un nuevo término lingüístico.

**Request:**
```json
{
  "variable_id": "507f1f77bcf86cd799439012",
  "name": "alta",
  "description": "Temperatura alta",
  "membership_function": {
    "type": "trapezoidal",
    "parameters": [25, 30, 50, 50]
  }
}
```

**Tipos de Funciones de Membresía:**
- `triangular`: `[a, b, c]` donde b es el pico
- `trapezoidal`: `[a, b, c, d]` donde b-c es la meseta
- `gaussian`: `[center, sigma]`
- `sigmoid`: `[center, slope]`

**Response (201):**
```json
{
  "id": "507f1f77bcf86cd799439014",
  "message": "Term created successfully"
}
```

### GET /api/fuzzy/terms

**Scope:** `fuzzy.read`

Lista todos los términos.

**Query Parameters:**
- `variable_id` (opcional): Filtrar por variable

### GET /api/fuzzy/terms/{term_id}

**Scope:** `fuzzy.read`

Obtiene detalles de un término específico.

### PUT /api/fuzzy/terms/{term_id}

**Scope:** `fuzzy.write`

Actualiza un término.

### DELETE /api/fuzzy/terms/{term_id}

**Scope:** `fuzzy.write`

Elimina un término.

## 📏 Reglas Fuzzy

### POST /api/fuzzy/rules

**Scope:** `fuzzy.write`

Crea una nueva regla fuzzy.

**Request:**
```json
{
  "name": "enfriar_cuando_calor",
  "description": "Activar ventilador cuando temperatura es alta",
  "system_id": "507f1f77bcf86cd799439011",
  "conditions": [
    {
      "variable_id": "507f1f77bcf86cd799439012",
      "term_id": "507f1f77bcf86cd799439014",
      "operator": "is"
    }
  ],
  "conclusions": [
    {
      "variable_id": "507f1f77bcf86cd799439015",
      "term_id": "507f1f77bcf86cd799439016",
      "weight": 1.0
    }
  ],
  "priority": 1
}
```

**Operadores Disponibles:**
- `is`: Variable es término
- `is_not`: Variable no es término
- `and`: Conjunción lógica
- `or`: Disyunción lógica

**Response (201):**
```json
{
  "id": "507f1f77bcf86cd799439017",
  "message": "Rule created successfully"
}
```

### GET /api/fuzzy/rules

**Scope:** `fuzzy.read`

Lista todas las reglas.

**Query Parameters:**
- `system_id` (opcional): Filtrar por sistema
- `priority` (opcional): Filtrar por prioridad

### GET /api/fuzzy/rules/{rule_id}

**Scope:** `fuzzy.read`

Obtiene detalles de una regla específica.

### PUT /api/fuzzy/rules/{rule_id}

**Scope:** `fuzzy.write`

Actualiza una regla.

### DELETE /api/fuzzy/rules/{rule_id}

**Scope:** `fuzzy.write`

Elimina una regla.

## 🔄 Rutinas

### POST /api/fuzzy/routines

**Scope:** `fuzzy.write`

Crea una nueva rutina de evaluación.

**Request:**
```json
{
  "name": "control_temperatura",
  "description": "Rutina de control de temperatura",
  "system_id": "507f1f77bcf86cd799439011",
  "rules": [
    "507f1f77bcf86cd799439017",
    "507f1f77bcf86cd799439018"
  ],
  "schedule": {
    "type": "interval",
    "interval_seconds": 300
  },
  "enabled": true
}
```

**Response (201):**
```json
{
  "id": "507f1f77bcf86cd799439019",
  "message": "Routine created successfully"
}
```

### GET /api/fuzzy/routines

**Scope:** `fuzzy.read`

Lista todas las rutinas.

### GET /api/fuzzy/routines/{routine_id}

**Scope:** `fuzzy.read`

Obtiene detalles de una rutina específica.

### PUT /api/fuzzy/routines/{routine_id}

**Scope:** `fuzzy.write`

Actualiza una rutina.

### DELETE /api/fuzzy/routines/{routine_id}

**Scope:** `fuzzy.write`

Elimina una rutina.

## 🎛️ Mapeos de Actuadores

### POST /api/fuzzy/actuator-mappings

**Scope:** `fuzzy.write`

Crea un mapeo entre variables de salida y actuadores.

**Request:**
```json
{
  "variable_id": "507f1f77bcf86cd799439015",
  "actuator_id": "FAN_001",
  "actuator_type": "fan",
  "mapping_function": {
    "type": "linear",
    "min_output": 0,
    "max_output": 100,
    "unit": "percent"
  },
  "enabled": true
}
```

**Response (201):**
```json
{
  "id": "507f1f77bcf86cd79943901a",
  "message": "Actuator mapping created successfully"
}
```

### GET /api/fuzzy/actuator-mappings

**Scope:** `fuzzy.read`

Lista todos los mapeos de actuadores.

### GET /api/fuzzy/actuator-mappings/{mapping_id}

**Scope:** `fuzzy.read`

Obtiene detalles de un mapeo específico.

### PUT /api/fuzzy/actuator-mappings/{mapping_id}

**Scope:** `fuzzy.write`

Actualiza un mapeo.

### DELETE /api/fuzzy/actuator-mappings/{mapping_id}

**Scope:** `fuzzy.write`

Elimina un mapeo.

## 🧪 Simulación y Evaluación

### POST /api/simulate

**Scope:** `fuzzy.execute`

Simula la evaluación del sistema fuzzy con datos de entrada.

**Request:**
```json
{
  "esp32Id": "ESP32_001",
  "timestamp": "2024-01-15T10:30:00Z",
  "readings": [
    {
      "variableId": "temperatura_aire",
      "value": 32.5
    },
    {
      "variableId": "humedad",
      "value": 65.0
    },
    {
      "variableId": "luminosidad",
      "value": 8000
    }
  ]
}
```

**Response:**
```json
{
  "plans": [
    {
      "actuator_id": "FAN_001",
      "target_value": 75.5,
      "action": "set_speed",
      "duration_ms": 300000,
      "metadata": {
        "rule_fired": "enfriar_cuando_calor",
        "confidence": 0.85,
        "fuzzy_output": 75.5
      }
    },
    {
      "actuator_id": "LED_001",
      "target_value": 0.0,
      "action": "turn_off",
      "duration_ms": 0,
      "metadata": {
        "rule_fired": "apagar_luz_suficiente",
        "confidence": 0.92,
        "fuzzy_output": 0.0
      }
    }
  ],
  "evaluation_details": {
    "memberships": {
      "temperatura_aire": {
        "baja": 0.0,
        "media": 0.15,
        "alta": 0.85
      },
      "humedad": {
        "baja": 0.0,
        "media": 0.7,
        "alta": 0.3
      }
    },
    "fired_rules": [
      {
        "rule_id": "507f1f77bcf86cd799439017",
        "rule_name": "enfriar_cuando_calor",
        "strength": 0.85
      }
    ],
    "execution_time_ms": 15.2
  }
}
```

## 📊 Monitoreo y Métricas

### GET /api/metrics

**Scope:** `fuzzy.read`

Obtiene métricas del sistema.

**Response:**
```json
{
  "system_metrics": {
    "uptime_seconds": 86400,
    "memory_usage_mb": 128.5,
    "cpu_usage_percent": 15.2
  },
  "fuzzy_metrics": {
    "total_evaluations": 1250,
    "evaluations_per_minute": 4.2,
    "average_evaluation_time_ms": 12.8,
    "active_systems": 3,
    "active_routines": 8
  },
  "database_metrics": {
    "connection_status": "healthy",
    "query_count": 5420,
    "average_query_time_ms": 8.5
  },
  "mqtt_metrics": {
    "connection_status": "connected",
    "messages_received": 2150,
    "messages_sent": 890,
    "last_message_at": "2024-01-15T10:29:45Z"
  }
}
```

### GET /api/actuators/status

**Scope:** `fuzzy.read`

Obtiene estado actual de todos los actuadores.

**Response:**
```json
[
  {
    "actuator_id": "FAN_001",
    "type": "fan",
    "current_value": 75.5,
    "target_value": 75.5,
    "status": "active",
    "last_updated": "2024-01-15T10:28:30Z",
    "last_command_id": "cmd_507f1f77bcf86cd79943901b"
  },
  {
    "actuator_id": "LED_001",
    "type": "led",
    "current_value": 0.0,
    "target_value": 0.0,
    "status": "inactive",
    "last_updated": "2024-01-15T10:25:15Z",
    "last_command_id": "cmd_507f1f77bcf86cd79943901c"
  }
]
```

## ⚙️ Configuración

### GET /api/config

**Scope:** `fuzzy.read`

Obtiene configuración actual del sistema.

**Response:**
```json
{
  "fuzzy_engine": {
    "defuzzification_method": "centroid",
    "inference_method": "mamdani",
    "aggregation_method": "max"
  },
  "evaluation": {
    "max_rules_per_evaluation": 100,
    "timeout_ms": 5000,
    "cache_results": true,
    "cache_ttl_seconds": 300
  },
  "actuators": {
    "command_timeout_ms": 10000,
    "retry_attempts": 3,
    "hysteresis_threshold": 0.1
  }
}
```

### PUT /api/config

**Scope:** `fuzzy.write`

Actualiza configuración del sistema.

**Request:**
```json
{
  "fuzzy_engine": {
    "defuzzification_method": "centroid",
    "cache_ttl_seconds": 600
  }
}
```

## 🚨 Códigos de Error

### Códigos HTTP Estándar

- `200`: OK - Operación exitosa
- `201`: Created - Recurso creado exitosamente
- `400`: Bad Request - Datos de entrada inválidos
- `401`: Unauthorized - Token JWT inválido o faltante
- `403`: Forbidden - Permisos insuficientes (scope)
- `404`: Not Found - Recurso no encontrado
- `409`: Conflict - Conflicto de recursos (ej: nombre duplicado)
- `422`: Unprocessable Entity - Error de validación
- `500`: Internal Server Error - Error interno del servidor
- `503`: Service Unavailable - Servicio no disponible

### Formato de Errores

```json
{
  "detail": "Descripción del error",
  "error_code": "FUZZY_001",
  "timestamp": "2024-01-15T10:30:00Z",
  "path": "/api/fuzzy/systems",
  "validation_errors": [
    {
      "field": "name",
      "message": "Field is required"
    }
  ]
}
```

### Códigos de Error Específicos

- `FUZZY_001`: Variable no encontrada
- `FUZZY_002`: Regla inválida
- `FUZZY_003`: Sistema no publicado
- `FUZZY_004`: Función de membresía inválida
- `FUZZY_005`: Rutina no activa
- `FUZZY_006`: Actuador no disponible
- `FUZZY_007`: Evaluación timeout
- `FUZZY_008`: Configuración inválida

## 📝 Ejemplos de Uso Completo

### Configuración de Sistema Básico

```bash
#!/bin/bash

# 1. Crear sistema
SYSTEM_ID=$(curl -s -X POST "http://localhost:8003/api/fuzzy/systems" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "name": "Control Hidropónico",
    "description": "Sistema de control automático",
    "version": "1.0.0"
  }' | jq -r '.id')

# 2. Crear variable de entrada
TEMP_VAR_ID=$(curl -s -X POST "http://localhost:8003/api/fuzzy/variables" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "name": "temperatura",
    "description": "Temperatura ambiente",
    "min_value": 0,
    "max_value": 50,
    "unit": "°C",
    "type": "input",
    "system_id": "'$SYSTEM_ID'"
  }' | jq -r '.id')

# 3. Crear términos
TEMP_HIGH_ID=$(curl -s -X POST "http://localhost:8003/api/fuzzy/terms" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "variable_id": "'$TEMP_VAR_ID'",
    "name": "alta",
    "membership_function": {
      "type": "trapezoidal",
      "parameters": [25, 30, 50, 50]
    }
  }' | jq -r '.id')

# 4. Crear variable de salida
FAN_VAR_ID=$(curl -s -X POST "http://localhost:8003/api/fuzzy/variables" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "name": "ventilador",
    "description": "Velocidad del ventilador",
    "min_value": 0,
    "max_value": 100,
    "unit": "%",
    "type": "output",
    "system_id": "'$SYSTEM_ID'"
  }' | jq -r '.id')

# 5. Crear regla
curl -X POST "http://localhost:8003/api/fuzzy/rules" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "name": "enfriar_cuando_calor",
    "system_id": "'$SYSTEM_ID'",
    "conditions": [
      {
        "variable_id": "'$TEMP_VAR_ID'",
        "term_id": "'$TEMP_HIGH_ID'",
        "operator": "is"
      }
    ],
    "conclusions": [
      {
        "variable_id": "'$FAN_VAR_ID'",
        "term_id": "'$FAN_HIGH_ID'",
        "weight": 1.0
      }
    ]
  }'

# 6. Simular evaluación
curl -X POST "http://localhost:8003/api/simulate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "esp32Id": "ESP32_001",
    "timestamp": "2024-01-15T10:30:00Z",
    "readings": [
      {
        "variableId": "temperatura",
        "value": 32.5
      }
    ]
  }'
```

---

**Versión de API**: 1.0.0  
**Última actualización**: Enero 2024  
**Formato**: OpenAPI 3.0