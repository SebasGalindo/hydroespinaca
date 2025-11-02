# Sensor Service - Gestión de Sensores IoT

> Microservicio para gestión de sensores, lecturas, alertas y agregaciones de datos ambientales

## 📋 Descripción

El **Sensor Service** gestiona sensores IoT conectados a nodos ESP32, procesando lecturas y generando alertas basadas en umbrales configurables. Es el componente central para la telemetría del sistema HydroEspinaca.

**Responsabilidades principales:**
- Gestionar sensores físicos y variables ambientales (PH, temperatura, humedad, EC, etc.)
- Procesar lecturas desde nodos ESP32 vía MQTT
- Generar alertas críticas basadas en umbrales físicos y óptimos
- Calcular agregaciones estadísticas (promedios, tendencias, variabilidad)
- Monitorear salud de nodos ESP32 (heartbeat, detección offline)

## 🏗️ Arquitectura

```
SensorService/
├── SensorService.Api/           # REST API, Controllers, Middleware
├── SensorService.Application/   # Use Cases, Services, DTOs, Validators
├── SensorService.Domain/        # Entities, Value Objects, Interfaces
└── SensorService.Infrastructure/ # Repositories, MQTT Workers, HTTP Clients
```

**Arquitectura:** Clean Architecture + Event-Driven (MQTT)

## 🔌 Dependencias

### Bases de Datos
- **MongoDB**:
  - `sensors` - Configuración de sensores físicos
  - `variables` - Variables ambientales y nutricionales
  - `readings` - Lecturas de sensores (series temporales)
  - `aggregates` - Datos agregados (promedios, tendencias)
  - `sensor_alerts` - Alertas por umbral de sensor
  - `esp32_nodes` - Nodos ESP32 registrados
  - `esp32_alerts` - Alertas de nodos offline

### Mensajería
- **MQTT**:
  - **Suscribe**: `sensor/readings` - Lecturas de sensores
  - **Suscribe**: `esp32/+/status` - Heartbeat de nodos ESP32

### Microservicios
- **Notification Service**: Envío de emails de alertas críticas (M2M authentication)

## 📡 Endpoints

### Sensores
```
GET    /api/sensors                 # Listar todos los sensores
GET    /api/sensors/{id}            # Obtener sensor por ID
POST   /api/sensors                 # Crear nuevo sensor
PUT    /api/sensors/{id}            # Actualizar sensor
DELETE /api/sensors/{id}            # Eliminar sensor
```

**Ejemplo de sensor:**
```json
{
  "code": "dht22-01",
  "physicalId": "DHT22-A1",
  "location": "greenhouse",
  "esp32Id": "507f191e810c19729de860ea",
  "status": "Active",
  "samplingFrequency": 60,
  "variables": ["T_AMB", "HUM"]
}
```

### Variables
```
GET    /api/variables               # Listar todas las variables
GET    /api/variables/{id}          # Obtener variable por ID
POST   /api/variables               # Crear nueva variable
PUT    /api/variables/{id}          # Actualizar variable
DELETE /api/variables/{id}          # Eliminar variable
```

### Lecturas
```
GET    /api/readings/{sensorId}/{variableId}?from=&to=  # Lecturas por rango temporal
GET    /api/readings/latest         # Últimas lecturas enriquecidas
```

### Agregados
```
GET    /api/aggregates/{sensorId}/{variableId}?from=&to=  # Agregados por sensor
POST   /api/aggregates/environmental  # Agregados ambientales
```

### Alertas
```
GET    /api/alerts/sensor/{sensorId}      # Alertas por sensor
PATCH  /api/alerts/{alertId}/acknowledge  # Reconocer alerta
```

### ESP32 Nodes
```
GET    /api/esp32nodes              # Listar todos los nodos
GET    /api/esp32nodes/{id}         # Obtener nodo por ID
POST   /api/esp32nodes              # Registrar nuevo nodo
PUT    /api/esp32nodes/{id}/status  # Actualizar estado
GET    /api/esp32nodes/{id}/exists  # Verificar existencia
```

### ESP32 Alerts
```
GET    /api/esp32-alerts/esp32/{id}       # Alertas por nodo
PATCH  /api/esp32-alerts/{id}/acknowledge # Reconocer alerta
```

### Health
```
GET    /health                      # Health check
```

## � Background Workers

1. **SensorMqttWorker**: Procesa lecturas de sensores (topic: `sensor/readings`)
2. **Esp32StatusMqttWorker**: Actualiza telemetría de nodos (topic: `esp32/+/status`)
3. **AggregateWorker**: Calcula agregaciones estadísticas cada 5 minutos
4. **Esp32OfflineWorker**: Detecta nodos sin heartbeat en 2+ minutos y genera alertas
5. **AlertCleanupWorker**: Limpia alertas antiguas cada 60 minutos

## ⚙️ Configuración

### appsettings.json
```json
{
  "MongoSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "hydroespinaca_sensors"
  },
  "MqttSettings": {
    "Broker": "mqtt://localhost",
    "Port": 1883,
    "Username": "user",
    "Password": "pass"
  },
  "M2M": {
    "ClientId": "sensor-service",
    "ClientSecret": "secret",
    "TokenUrl": "http://auth-service/token",
    "NotificationServiceUrl": "http://notification-service"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key",
    "ExpirationMinutes": 60
  }
}
```

### Variables de Entorno (Producción)
```bash
MONGO_CONNECTION_STRING=mongodb://user:pass@mongo:27017
MONGO_DATABASE_NAME=hydroespinaca_sensors
MQTT_BROKER=mqtt://mqtt-broker
MQTT_PORT=1883
M2M__ClientSecret=production-secret
JWT_SECRET_KEY=your-production-secret
```

## 🚀 Ejecución

### Desarrollo Local
```bash
# Restaurar y ejecutar
dotnet restore
dotnet run --project SensorService.Api

# Con hot reload
dotnet watch run --project SensorService.Api
```

API disponible en `http://localhost:8080`

### Docker
```bash
# Build
docker build -t sensor-service:latest .

# Run
docker run -p 8080:80 \
  -e MONGO_CONNECTION_STRING=mongodb://mongo:27017 \
  -e MQTT_BROKER=mqtt://mqtt-broker \
  sensor-service:latest
```

## 🔐 Autenticación

**JWT Bearer tokens** con políticas basadas en roles:

| Policy | Descripción |
|--------|-------------|
| `SensorRead` | Lectura de sensores |
| `SensorWrite` | Escritura de sensores |
| `VariableRead` | Lectura de variables |
| `VariableWrite` | Escritura de variables |
| `ReadingRead` | Lectura de datos |
| `AggregateRead` | Lectura de agregados |
| `AlertRead` | Lectura de alertas |
| `AlertWrite` | Gestión de alertas |
| `Esp32Read` | Lectura de nodos ESP32 |
| `Esp32Write` | Gestión de nodos ESP32 |

## 📝 Notas Importantes

### Alertas y Notificaciones
- **SensorAlert**: Generadas cuando lecturas exceden umbrales físicos u óptimos
- **Esp32Alert**: Generadas cuando nodos no reportan heartbeat en 2+ minutos
- **⚠️ Notification Services**: Usan Singleton con estado en memoria (migrar a Redis para múltiples réplicas)

### Índices MongoDB
El servicio crea índices automáticamente en startup para optimizar queries:
- `readings`: (sensorId, variableCode, timestamp)
- `aggregates`: (sensorId, variableCode, windowStart)
- `sensor_alerts`: (variableCode, resolvedAt)
- `esp32_alerts`: (esp32Id, resolvedAt)

### Producción
- Configurar caching para variables (rara vez cambian)
- Implementar retry policies para MQTT reconnection
- Migrar servicios de notificación a Redis para horizontal scaling
- Configurar rate limiting en endpoints públicos

---

**Stack:** .NET 9, MongoDB, MQTT  
**Última actualización:** 2025-10-20
