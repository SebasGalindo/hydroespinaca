# Actuator Service - Control de Actuadores IoT

> Microservicio para gestión y ejecución de comandos sobre actuadores conectados a nodos ESP32

## 📋 Descripción

El **Actuator Service** gestiona el control de actuadores físicos (bombas, ventiladores, luces, humidificadores) en el sistema HydroEspinaca. Coordina la ejecución de comandos desde múltiples fuentes (fuzzy logic, rutinas internas, control manual), garantizando la seguridad del hardware mediante reglas de bloqueo de pines, timeouts y cooldowns.

**Responsabilidades principales:**
- Ejecutar comandos sobre actuadores conectados a ESP32 vía MQTT
- Gestionar rutinas internas programadas por tiempo (time-based scheduling)
- Coordinar máquina de estados de actuadores (ON/OFF/PWM)
- Aplicar reglas de seguridad (pin locking, max runtime, cooldowns)
- Proveer analytics de uso y estado de actuadores

## 🏗️ Arquitectura

```
ActuatorService/
├── ActuatorService.Api/           # REST API, Controllers, Middleware
├── ActuatorService.Application/   # Use Cases, Services, DTOs, Validators
├── ActuatorService.Domain/        # Entities, Interfaces, State Machine
└── ActuatorService.Infrastructure/ # Repositories, MQTT, HTTP Clients
```

**Arquitectura:** Clean Architecture + Event-Driven (MQTT)

## 🔌 Dependencias

### Bases de Datos
- **MongoDB**: 
  - `actuators` - Configuración de actuadores físicos
  - `routine_commands` - Historial de comandos ejecutados
  - `internal_routines` - Rutinas programadas por tiempo

### Mensajería
- **MQTT**:
  - **Publica**: `hydro/{esp32Id}/job-schedule` - Comandos a ejecutar en firmware
  - **Suscribe**: `hydro/{esp32Id}/routine/completed` - Confirmaciones de ejecución

### Microservicios
- **Sensor Service**: Validación de existencia de dispositivos ESP32

## 📡 Endpoints

### Actuators
```
GET    /api/actuators                # Listar todos los actuadores
GET    /api/actuators/{id}           # Obtener por ID
GET    /api/actuators/esp32/{id}     # Filtrar por ESP32
POST   /api/actuators                # Crear actuador
PUT    /api/actuators/{id}           # Actualizar actuador
DELETE /api/actuators/{id}           # Eliminar actuador
```

**Ejemplo de actuador:**
```json
{
  "esp32Id": "507f191e810c19729de860ea",
  "code": "BombaRiego",
  "type": "PUMP",
  "mode": "DIGITAL",
  "physicalId": "PUMP-001",
  "pin": "GPIO12",
  "location": "Zona A",
  "status": "Active"
}
```

### Commands
```
POST   /api/commands/execute         # Ejecutar comandos sobre actuadores
GET    /api/commands/jobs/status     # Estado de jobs activos/pendientes
DELETE /api/commands/jobs/clear      # Limpiar schedule y resetear actuadores
POST   /api/commands/analytics       # Analytics de uso de actuadores
```

**Ejemplo de ejecución:**
```json
{
  "controls": [
    {
      "actuatorCode": "BombaRiego",
      "power": "ON",
      "duration": 300
    }
  ]
}
```

### Internal Routines
```
GET    /api/internal-routines        # Listar rutinas internas
GET    /api/internal-routines/{id}   # Obtener rutina por ID
POST   /api/internal-routines        # Crear rutina interna
PUT    /api/internal-routines/{id}   # Actualizar rutina
DELETE /api/internal-routines/{id}   # Eliminar rutina
```

### Actuator States
```
GET    /api/actuators/states         # Estados de todos los actuadores
GET    /api/actuators/{id}/state     # Estado de un actuador específico
```

### Health
```
GET    /health                       # Health check
```

## 🚀 Funcionalidades Clave

1. **Command Execution Service**: Gestiona cola de comandos con pin locking (máx 1 activo + 1 pendiente por pin)
2. **Internal Routine Scheduler**: Ejecuta rutinas programadas por tiempo (cálculo determinístico basado en intervalos)
3. **Safety Rules Monitor**: Apaga automáticamente actuadores que exceden límites de tiempo configurados
4. **Actuator State Machine**: Mantiene estado en memoria de todos los actuadores (ON/OFF, PWM, duración)
5. **Advanced Behavior Rules**: Cooldowns post-ejecución y bloqueos temporales de pines

## ⚙️ Configuración

### appsettings.json
```json
{
  "MongoSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "hydro_db"
  },
  "MqttSettings": {
    "BrokerAddress": "localhost",
    "Port": 1883
  },
  "SensorService": {
    "BaseUrl": "http://localhost:5001"
  },
  "SafetyRules": {
    "CheckIntervalSeconds": 60,
    "Limits": [
      { "Code": "BombaRiego", "MaxOnTimeSeconds": 600 }
    ]
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
MQTT_BROKER_ADDRESS=mqtt-broker
SENSOR_SERVICE_URL=http://sensor-service:8080
JWT_SECRET_KEY=your-production-secret
```

## 🚀 Ejecución

### Desarrollo Local
```bash
# Restaurar y ejecutar
dotnet restore
dotnet run --project ActuatorService.Api

# Con hot reload
dotnet watch run --project ActuatorService.Api
```

API disponible en `http://localhost:5063`

### Docker
```bash
# Build
docker build -t actuator-service:latest .

# Run
docker run -p 5063:80 \
  -e MONGO_CONNECTION_STRING=mongodb://mongo:27017 \
  -e MQTT_BROKER_ADDRESS=mqtt-broker \
  actuator-service:latest
```

## 🔐 Autenticación

**JWT Bearer tokens** con políticas basadas en roles:

| Policy | Descripción |
|--------|-------------|
| `ActuatorRead` | Lectura de actuadores |
| `ActuatorCreate` | Creación de actuadores |
| `ActuatorUpdate` | Actualización de actuadores |
| `ActuatorDelete` | Eliminación de actuadores |
| `ActuatorControl` | Control de actuadores (ejecución de comandos) |
| `CommandRead` | Lectura de comandos y estados |

## 📝 Notas Importantes

### Concurrencia
- **Pin Locking**: Máximo 1 comando activo + 1 pendiente por pin (comandos adicionales son rechazados)
- **State Machine**: Singleton thread-safe con estado en memoria sincronizado con MongoDB
- **Workers**: Cada HostedService ejecuta en su propio scope

### Seguridad del Hardware
- **Safety Rules**: Configurar límites de tiempo máximo según hardware físico (bombas: 10-15min)
- **Cooldowns**: Evitar re-encendidos inmediatos que puedan dañar componentes
- **MQTT QoS**: Comandos críticos usan QoS 1 para garantizar entrega

### Producción
- Configurar índices MongoDB: `actuators.code`, `routine_commands.actuator_code`
- MQTT broker resiliente (cluster de 3 nodos mínimo)
- Alertar sobre comandos rechazados (límite de cola alcanzado)

---

**Stack:** .NET 9, MongoDB, MQTT  
**Última actualización:** 2025-10-20
