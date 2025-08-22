# Guía de Uso del Servicio Fuzzy

## Introducción

Esta guía explica cómo utilizar el servicio fuzzy optimizado, incluyendo las nuevas funcionalidades de monitoreo, caché y optimizaciones de rendimiento.

## Configuración Inicial

### 1. Variables de Entorno

Crea un archivo `.env` con la configuración necesaria:

```bash
# Configuración de base de datos
MONGO_CONNECTION_STRING=mongodb://localhost:27017
MONGO_DATABASE_NAME=fuzzy_service

# Configuración MQTT
MQTT_BROKER_HOST=localhost
MQTT_BROKER_PORT=1883
MQTT_USERNAME=fuzzy_service
MQTT_PASSWORD=your_password

# Servicios externos
ACTUATOR_SERVICE_URL=http://localhost:8002

# Configuración de optimizaciones
CACHE_TTL_SECONDS=300
CACHE_MAX_SIZE=1000
RATE_LIMIT_WINDOW_SECONDS=60
RATE_LIMIT_MAX_COMMANDS=10

# Configuración de autenticación
JWT_SECRET_KEY=your_secret_key
JWT_ALGORITHM=HS256
```

### 2. Instalación de Dependencias

```bash
cd fuzzy-service
pip install -r requirements.txt
```

### 3. Ejecución del Servicio

```bash
# Desarrollo
uvicorn main:app --reload --host 0.0.0.0 --port 8001

# Producción
uvicorn main:app --host 0.0.0.0 --port 8001 --workers 4
```

## Uso de APIs

### Autenticación

Todos los endpoints (excepto health checks) requieren autenticación JWT:

```bash
# Obtener token (desde auth-service)
curl -X POST "http://localhost:8000/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "password"}'

# Usar token en requests
curl -X GET "http://localhost:8001/api/actuators/status" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Endpoints Principales

#### 1. Simulación de Evaluación

```bash
curl -X POST "http://localhost:8001/api/simulate" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "readings": [
      {
        "sensor_id": "temp_01",
        "value": 25.5,
        "timestamp": "2024-01-15T10:30:00Z"
      },
      {
        "sensor_id": "humidity_01",
        "value": 65.0,
        "timestamp": "2024-01-15T10:30:00Z"
      }
    ]
  }'
```

#### 2. Estado de Actuadores

```bash
curl -X GET "http://localhost:8001/api/actuators/status" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

Respuesta:
```json
[
  {
    "actuator_id": "pump_01",
    "last_target": 75.0,
    "last_emitted_at": "2024-01-15T10:25:00Z",
    "is_in_cooldown": false,
    "cooldown_remaining": 0
  }
]
```

#### 3. Gestión de Variables Fuzzy

```bash
# Listar variables
curl -X GET "http://localhost:8001/api/fuzzy/variables" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Crear variable
curl -X POST "http://localhost:8001/api/fuzzy/variables" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "temperature",
    "min_value": 0.0,
    "max_value": 50.0,
    "membership_function": {
      "type": "triangular",
      "parameters": [15.0, 25.0, 35.0]
    }
  }'
```

#### 4. Gestión de Rutinas

```bash
# Listar rutinas
curl -X GET "http://localhost:8001/api/routines" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Activar/desactivar rutina
curl -X PUT "http://localhost:8001/api/routines/routine_id/state" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"active": true}'
```

## Monitoreo y Optimizaciones

### Estadísticas de Rendimiento

```bash
curl -X GET "http://localhost:8001/api/performance/stats" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

Respuesta:
```json
{
  "endpoint_metrics": {
    "counters": {
      "simulate_evaluation_endpoint": 150,
      "get_actuator_status_endpoint": 75
    },
    "timers": {
      "simulate_evaluation_endpoint": {
        "count": 150,
        "total_time": 45.2,
        "avg_time": 0.301,
        "min_time": 0.125,
        "max_time": 1.205
      }
    }
  },
  "use_case_metrics": {
    "fuzzy_engine": {
      "cache_stats": {
        "hits": 1250,
        "misses": 180,
        "hit_ratio": 0.874
      }
    },
    "rate_limiter": {
      "commands_sent": 45,
      "commands_blocked": 3
    }
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Limpieza de Cachés

```bash
curl -X POST "http://localhost:8001/api/performance/clear-cache" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Health Checks

```bash
# Health check básico
curl -X GET "http://localhost:8001/healthz"

# Readiness check con dependencias
curl -X GET "http://localhost:8001/readyz"
```

## Configuración Avanzada

### Optimización de Caché

Puedes ajustar la configuración de caché según tus necesidades:

```python
# En tu configuración
CACHE_TTL_SECONDS = 600  # 10 minutos para datos estables
CACHE_MAX_SIZE = 2000    # Más memoria para mejor hit ratio
```

### Rate Limiting

Configura límites según la capacidad de tus actuadores:

```python
# Configuración por tipo de actuador
RATE_LIMITS = {
    "pump": {"max_commands": 5, "window_seconds": 60},
    "valve": {"max_commands": 10, "window_seconds": 30},
    "fan": {"max_commands": 20, "window_seconds": 60}
}
```

### Monitoreo Personalizado

Puedes agregar métricas personalizadas:

```python
from infrastructure.monitoring import PerformanceMonitor

monitor = PerformanceMonitor()

# Contador personalizado
monitor.increment_counter("custom_operation")

# Gauge personalizado
monitor.set_gauge("queue_size", 25)

# Timer personalizado
with monitor.timing_context("custom_operation"):
    # Tu código aquí
    pass
```

## Casos de Uso Comunes

### 1. Sistema de Riego Automático

```python
# Configurar variables fuzzy
variables = [
    {
        "name": "soil_moisture",
        "min_value": 0.0,
        "max_value": 100.0,
        "membership_function": {
            "type": "trapezoidal",
            "parameters": [0, 20, 40, 60]
        }
    },
    {
        "name": "pump_intensity",
        "min_value": 0.0,
        "max_value": 100.0,
        "membership_function": {
            "type": "triangular",
            "parameters": [0, 50, 100]
        }
    }
]

# Regla: Si humedad es baja, entonces bomba alta
rule = {
    "antecedents": [
        {"variable": "soil_moisture", "set": "low"}
    ],
    "consequent": {"variable": "pump_intensity", "set": "high"},
    "weight": 1.0
}
```

### 2. Control de Temperatura

```python
# Variables para control de temperatura
variables = [
    {
        "name": "temperature",
        "min_value": 10.0,
        "max_value": 40.0,
        "membership_function": {
            "type": "gaussian",
            "parameters": [25.0, 5.0]  # media, desviación
        }
    },
    {
        "name": "fan_speed",
        "min_value": 0.0,
        "max_value": 100.0,
        "membership_function": {
            "type": "linear",
            "parameters": [0, 100]
        }
    }
]
```

### 3. Monitoreo de Rendimiento

```bash
#!/bin/bash
# Script para monitoreo continuo

while true; do
    echo "=== $(date) ==="
    
    # Obtener estadísticas
    curl -s -X GET "http://localhost:8001/api/performance/stats" \
        -H "Authorization: Bearer $JWT_TOKEN" | jq '.endpoint_metrics.timers'
    
    # Verificar health
    curl -s -X GET "http://localhost:8001/readyz" | jq '.status'
    
    sleep 60
done
```

## Troubleshooting

### Problemas Comunes

#### 1. Alto Uso de Memoria

```bash
# Verificar estadísticas de caché
curl -X GET "http://localhost:8001/api/performance/stats" | jq '.use_case_metrics'

# Limpiar cachés si es necesario
curl -X POST "http://localhost:8001/api/performance/clear-cache"
```

#### 2. Comandos Bloqueados por Rate Limiting

```bash
# Verificar estadísticas de rate limiter
curl -X GET "http://localhost:8001/api/performance/stats" | jq '.use_case_metrics.rate_limiter'

# Ajustar configuración si es necesario
export RATE_LIMIT_MAX_COMMANDS=20
export RATE_LIMIT_WINDOW_SECONDS=30
```

#### 3. Baja Performance

```bash
# Verificar hit ratio de caché
curl -X GET "http://localhost:8001/api/performance/stats" | jq '.use_case_metrics.fuzzy_engine.cache_stats.hit_ratio'

# Si es bajo (<0.7), considerar:
# - Aumentar TTL del caché
# - Aumentar tamaño máximo del caché
# - Revisar patrones de uso
```

### Logs de Debug

```bash
# Habilitar logs de debug
export LOG_LEVEL=DEBUG

# Filtrar logs específicos
tail -f app.log | grep "fuzzy_engine" | jq .
```

## Mejores Prácticas

### 1. Configuración de Producción

- Usar múltiples workers: `--workers 4`
- Configurar límites de memoria apropiados
- Monitorear métricas regularmente
- Implementar alertas para errores

### 2. Optimización de Rendimiento

- Ajustar TTL de caché según estabilidad de datos
- Usar rate limiting apropiado para cada actuador
- Monitorear hit ratio de caché (objetivo: >80%)
- Limpiar cachés periódicamente en horarios de bajo tráfico

### 3. Seguridad

- Rotar JWT secrets regularmente
- Usar HTTPS en producción
- Implementar rate limiting a nivel de API Gateway
- Auditar logs de acceso regularmente

### 4. Monitoreo

- Configurar alertas para métricas críticas
- Revisar estadísticas de rendimiento semanalmente
- Implementar dashboards para visualización
- Mantener logs por al menos 30 días

## Integración con Otros Servicios

### Auth Service

```python
# Configurar validación de JWT
JWT_SECRET_KEY = "shared_secret_with_auth_service"
JWT_ALGORITHM = "HS256"
```

### Actuator Service

```python
# Configurar cliente HTTP
ACTUATOR_SERVICE_URL = "http://actuator-service:8002"
ACTUATOR_SERVICE_TIMEOUT = 5.0
```

### MQTT Broker

```python
# Configurar cliente MQTT
MQTT_BROKER_HOST = "mqtt-broker"
MQTT_BROKER_PORT = 1883
MQTT_QOS = 1
MQTT_RETAIN = False
```

## Conclusión

Este servicio fuzzy optimizado proporciona:

- ✅ **Alto rendimiento** con caché multinivel y vectorización
- ✅ **Monitoreo completo** con métricas detalladas
- ✅ **Protección contra abuso** con rate limiting
- ✅ **Prevención de duplicados** con procesamiento idempotente
- ✅ **Escalabilidad** con arquitectura stateless
- ✅ **Observabilidad** con logs estructurados y métricas

Para soporte adicional, consulta la documentación de arquitectura o contacta al equipo de desarrollo.