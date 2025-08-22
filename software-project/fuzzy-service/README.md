# Fuzzy Service - Sistema de Lógica Difusa para HydroEspinaca

## 📋 Descripción

El **Fuzzy Service** es un microservicio especializado en lógica difusa que forma parte del ecosistema HydroEspinaca. Su función principal es evaluar condiciones ambientales (temperatura, humedad, luminosidad) y generar comandos inteligentes para actuadores del sistema hidropónico.

### ✨ Características Optimizadas

- 🚀 **Alto Rendimiento**: Caché multinivel y vectorización con NumPy
- 📊 **Monitoreo Integrado**: Métricas detalladas de rendimiento y uso
- 🛡️ **Rate Limiting**: Protección contra spam de comandos
- 🔄 **Idempotencia**: Prevención de comandos duplicados
- 📈 **Escalabilidad**: Arquitectura stateless optimizada
- 🔍 **Observabilidad**: Logs estructurados y métricas en tiempo real

## 🏗️ Arquitectura

### Arquitectura Hexagonal (Clean Architecture)

```
┌─────────────────────────────────────────────────────────────┐
│                        API Layer                           │
│  ┌─────────────────┐  ┌─────────────────┐                 │
│  │   REST API      │  │   Health Checks │                 │
│  │   (FastAPI)     │  │                 │                 │
│  └─────────────────┘  └─────────────────┘                 │
├─────────────────────────────────────────────────────────────┤
│                    Application Layer                       │
│  ┌─────────────────┐  ┌─────────────────┐                 │
│  │   Use Cases     │  │      DTOs       │                 │
│  │                 │  │                 │                 │
│  └─────────────────┘  └─────────────────┘                 │
├─────────────────────────────────────────────────────────────┤
│                     Domain Layer                           │
│  ┌─────────────────┐  ┌─────────────────┐                 │
│  │   Entities      │  │   Fuzzy Engine  │                 │
│  │                 │  │                 │                 │
│  └─────────────────┘  └─────────────────┘                 │
├─────────────────────────────────────────────────────────────┤
│                  Infrastructure Layer                      │
│  ┌─────────────────┐  ┌─────────────────┐                 │
│  │   MongoDB       │  │   MQTT Worker   │                 │
│  │   Repositories  │  │                 │                 │
│  └─────────────────┘  └─────────────────┘                 │
└─────────────────────────────────────────────────────────────┘
```

### Componentes Principales

#### 1. **Motor de Lógica Difusa**
- Evaluación de funciones de membresía
- Inferencia basada en reglas
- Defuzzificación por centroide
- Soporte para múltiples variables de entrada y salida

#### 2. **Gestión de Configuración**
- Variables fuzzy con términos lingüísticos
- Reglas de inferencia
- Rutinas de evaluación
- Mapeos de actuadores

#### 3. **Worker MQTT**
- Procesamiento asíncrono de lecturas de sensores
- Filtro de histéresis para evitar oscilaciones
- Envío de comandos al actuator-service

#### 4. **API REST**
- CRUD completo para entidades del sistema
- Endpoints de simulación y evaluación
- Autenticación JWT con scopes
- Health checks y métricas

## 🚀 Inicio Rápido

### Prerrequisitos

- Python 3.11+
- MongoDB 5.0+
- MQTT Broker (Mosquitto)

### Instalación

```bash
# Clonar el repositorio
git clone <repository-url>
cd fuzzy-service

# Crear entorno virtual
python -m venv venv
source venv/bin/activate  # Linux/Mac
# o
venv\Scripts\activate     # Windows

# Instalar dependencias
pip install -r requirements.txt
```

### Configuración

Crear archivo `.env`:

```env
# Base de datos
FUZZY_MONGO_URI=mongodb://localhost:27017
FUZZY_MONGO_DB_NAME=hydroespinaca_fuzzy

# MQTT
FUZZY_MQTT_BROKER_HOST=localhost
FUZZY_MQTT_BROKER_PORT=1883
FUZZY_MQTT_USERNAME=fuzzy_user
FUZZY_MQTT_PASSWORD=fuzzy_pass

# Servicios externos
FUZZY_ACTUATOR_SERVICE_URL=http://localhost:8002
FUZZY_AUTH_SERVICE_URL=http://localhost:8001

# Configuración del servidor
FUZZY_HOST=0.0.0.0
FUZZY_PORT=8003
FUZZY_LOG_LEVEL=INFO
```

### Ejecución

```bash
# Desarrollo
uvicorn main:app --host 0.0.0.0 --port 8003 --reload

# Producción
uvicorn main:app --host 0.0.0.0 --port 8003

# Con Docker
docker build -t fuzzy-service .
docker run -p 8003:8003 --env-file .env fuzzy-service
```

## 📚 Guía de Uso

### 1. Configuración Inicial

#### Crear Variables Fuzzy

```bash
curl -X POST "http://localhost:8003/api/fuzzy/variables" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{
    "name": "temperatura_aire",
    "description": "Temperatura del aire en °C",
    "min_value": 0,
    "max_value": 50,
    "unit": "°C"
  }'
```

#### Crear Términos Lingüísticos

```bash
curl -X POST "http://localhost:8003/api/fuzzy/terms" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{
    "variable_id": "<variable_id>",
    "name": "alta",
    "membership_function": {
      "type": "trapezoidal",
      "parameters": [25, 30, 50, 50]
    }
  }'
```

#### Crear Reglas

```bash
curl -X POST "http://localhost:8003/api/fuzzy/rules" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{
    "name": "enfriar_cuando_calor",
    "conditions": [
      {
        "variable_id": "<temp_var_id>",
        "term_id": "<alta_term_id>",
        "operator": "is"
      }
    ],
    "conclusions": [
      {
        "variable_id": "<fan_var_id>",
        "term_id": "<alto_term_id>",
        "weight": 1.0
      }
    ]
  }'
```

### 2. Simulación y Evaluación

#### Simular Condiciones

```bash
curl -X POST "http://localhost:8003/api/simulate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{
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
      }
    ]
  }'
```

### 3. Monitoreo

#### Health Checks

```bash
# Health básico
curl http://localhost:8003/healthz

# Readiness con verificaciones
curl http://localhost:8003/readyz
```

#### Métricas del Sistema

```bash
curl -H "Authorization: Bearer <token>" \
  http://localhost:8003/api/metrics
```

## 🔧 Desarrollo

### Estructura del Proyecto

```
fuzzy-service/
├── api/                    # Capa de API
│   ├── endpoints.py        # Definición de endpoints
│   └── security.py         # Autenticación y autorización
├── application/            # Capa de aplicación
│   ├── dtos.py            # Data Transfer Objects
│   ├── interfaces/        # Interfaces de servicios
│   └── use_cases/         # Casos de uso
├── domain/                # Capa de dominio
│   ├── models/            # Entidades del dominio
│   └── services/          # Servicios de dominio
├── infrastructure/        # Capa de infraestructura
│   ├── config.py          # Configuración
│   ├── database.py        # Conexión a MongoDB
│   └── mongo_repositories.py # Implementación de repositorios
├── workers/               # Workers asíncronos
│   └── fuzzy_worker.py    # Worker principal MQTT
├── tests/                 # Pruebas
├── main.py               # Punto de entrada
├── requirements.txt      # Dependencias
└── Dockerfile           # Imagen Docker
```

### Ejecutar Pruebas

```bash
# Todas las pruebas
pytest

# Con cobertura
pytest --cov=. --cov-report=html

# Pruebas específicas
pytest tests/test_domain.py -v
```

### Estilo de Código

```bash
# Formatear código
black .

# Verificar estilo
flake8 .

# Type checking
mypy .
```

## 🔐 Seguridad

### Autenticación

- **JWT Tokens**: Validación local con JWKS del auth-service
- **Scopes**: Control granular de permisos
  - `fuzzy.read`: Lectura de configuración
  - `fuzzy.write`: Modificación de configuración
  - `fuzzy.execute`: Ejecución de simulaciones

### Autorización

```python
# Ejemplo de endpoint protegido
@router.post("/api/fuzzy/systems")
async def create_system(
    system_dto: FuzzySystemCreateDto,
    _: dict = Depends(ScopeChecker(["fuzzy.write"]))
):
    # Lógica del endpoint
```

### Variables de Entorno Sensibles

- `FUZZY_MQTT_PASSWORD`: Contraseña MQTT
- `FUZZY_JWT_SECRET`: Secreto para validación JWT
- `FUZZY_MONGO_URI`: URI de conexión MongoDB

## 📊 Monitoreo y Observabilidad

### Logs Estructurados

```python
import logging

logger = logging.getLogger(__name__)
logger.info("Evaluating fuzzy rules", extra={
    "system_id": system_id,
    "input_count": len(inputs),
    "execution_time_ms": execution_time
})
```

### Métricas Disponibles

- Tiempo de respuesta de endpoints
- Número de evaluaciones fuzzy
- Errores de conexión MQTT/MongoDB
- Estado de actuadores
- Uso de memoria y CPU

### Health Checks

- `/healthz`: Verificación básica de vida
- `/readyz`: Verificación de dependencias (MongoDB, MQTT)

## 🚀 Despliegue

### Docker

```dockerfile
# Dockerfile optimizado para producción
FROM python:3.11-slim

WORKDIR /app
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .
EXPOSE 8003

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8003"]
```

### Docker Compose

```yaml
version: '3.8'
services:
  fuzzy-service:
    build: .
    ports:
      - "8003:8003"
    environment:
      - FUZZY_MONGO_URI=mongodb://mongo:27017
      - FUZZY_MQTT_BROKER_HOST=mosquitto
    depends_on:
      - mongo
      - mosquitto
```

### Kubernetes

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: fuzzy-service
spec:
  replicas: 3
  selector:
    matchLabels:
      app: fuzzy-service
  template:
    metadata:
      labels:
        app: fuzzy-service
    spec:
      containers:
      - name: fuzzy-service
        image: fuzzy-service:latest
        ports:
        - containerPort: 8003
        env:
        - name: FUZZY_MONGO_URI
          valueFrom:
            secretKeyRef:
              name: fuzzy-secrets
              key: mongo-uri
```

## 📚 Documentación

### Guías Disponibles

- **[ARCHITECTURE.md](./ARCHITECTURE.md)**: Documentación detallada de la arquitectura y optimizaciones
- **[USAGE_GUIDE.md](./USAGE_GUIDE.md)**: Guía completa de uso y configuración
- **[API_DOCUMENTATION.md](./API_DOCUMENTATION.md)**: Documentación de endpoints y APIs
- **[OPTIMIZATION_GUIDE.md](./OPTIMIZATION_GUIDE.md)**: Guía de optimizaciones implementadas
- **[ENDPOINT_ANALYSIS.md](./ENDPOINT_ANALYSIS.md)**: Análisis de endpoints y especificaciones

### Monitoreo y Métricas

```bash
# Obtener estadísticas de rendimiento
curl -X GET "http://localhost:8001/api/performance/stats" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Limpiar cachés del sistema
curl -X POST "http://localhost:8001/api/performance/clear-cache" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Health checks
curl -X GET "http://localhost:8001/healthz"  # Básico
curl -X GET "http://localhost:8001/readyz"   # Con dependencias
```

### Optimizaciones Implementadas

#### 🚀 Rendimiento
- **Caché multinivel**: Membresías, evaluaciones y mapeos
- **Vectorización NumPy**: Operaciones matriciales optimizadas
- **Rate limiting**: Control de frecuencia de comandos
- **Procesamiento idempotente**: Prevención de duplicados

#### 📊 Monitoreo
- **Métricas en tiempo real**: Contadores, gauges, histogramas, timers
- **Logs estructurados**: Formato JSON para análisis
- **Estadísticas de caché**: Hit ratio y uso de memoria
- **Endpoints de monitoreo**: APIs para obtener métricas

## 🤝 Contribución

### Flujo de Desarrollo

1. Fork del repositorio
2. Crear rama feature: `git checkout -b feature/nueva-funcionalidad`
3. Commit cambios: `git commit -am 'Agregar nueva funcionalidad'`
4. Push a la rama: `git push origin feature/nueva-funcionalidad`
5. Crear Pull Request

### Estándares de Código

- **PEP 8**: Estilo de código Python
- **Type Hints**: Tipado estático obligatorio
- **Docstrings**: Documentación de funciones y clases
- **Tests**: Cobertura mínima del 80%

## 📄 Licencia

Este proyecto está bajo la Licencia MIT. Ver `LICENSE` para más detalles.

## 🆘 Soporte

- **Documentación**: [Wiki del proyecto]
- **Issues**: [GitHub Issues]
- **Discusiones**: [GitHub Discussions]
- **Email**: soporte@hydroespinaca.com

---

**Versión**: 1.0.0  
**Última actualización**: Enero 2024  
**Mantenedores**: Equipo HydroEspinaca