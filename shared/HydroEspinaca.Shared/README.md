# HydroEspinaca.Shared

**Librería compartida de tipos, utilidades y configuración común** para todos los microservicios del sistema HydroEspinaca.

---

## Descripción

`HydroEspinaca.Shared` es una librería NuGet local que centraliza:
- DTOs compartidos entre microservicios
- Lógica de autenticación y autorización JWT
- Extensiones de configuración para MongoDB, MQTT, Swagger
- Utilidades, validaciones y constantes del sistema
- Tipos base para repositorios y entidades

Esta librería es consumida por **todos los microservicios** del proyecto (auth-service, sensor-service, actuator-service, bff-service, notification-service).

---

## Estructura del Proyecto

```
HydroEspinaca.Shared/
├── Abstractions/              # Interfaces base (IIdentifiable, IEntityMapper)
├── Authentication/            # JWT, M2M auth, handlers de autorización
│   ├── Extensions/
│   ├── Handlers/
│   ├── Interfaces/
│   └── Services/
├── Constants/                 # Constantes del sistema (MQTT topics, roles, etc.)
├── DTOs/                      # Data Transfer Objects
│   ├── Actuator/
│   ├── Alerts/
│   ├── Analytics/
│   ├── Authentication/
│   ├── Esp32/
│   ├── Mqtt/
│   ├── Notifications/
│   ├── Readings/
│   ├── Sensors/
│   └── Variables/
├── Enums/                     # Enumeraciones del dominio
├── Errors/                    # Excepciones y ProblemDetails factory
├── Extensions/                # Extension methods para DI y configuración
├── Interfaces/                # Interfaces de servicios compartidos
├── Mongo/                     # BaseMongoRepository y contexto
├── Mqtt/                      # Cliente MQTT compartido
├── Options/                   # Clases de configuración (Settings)
├── Responses/                 # Wrappers de respuesta HTTP
├── Utils/                     # Utilidades (ObjectIdHelper, TimeFormatter)
└── Validations/               # Validadores FluentValidation reutilizables
```

---

## Responsabilidades Principales

### 1. **Autenticación y Autorización**
- **JWT Token Management**: `JwtKeyResolver`, `M2MTokenService`
- **Authorization Handlers**: `SystemAdminOverrideHandler` (scope `system:admin` bypass)
- **Scope-Based Policies**: Configuración de políticas por scopes
- **Extension Methods**: `AddHydroEspinacaAuth()`, `AddMicroserviceAuth()`

### 2. **Persistencia MongoDB**
- **BaseMongoRepository**: Repositorio genérico base para entidades
- **IEntityMapper**: Interfaz para mapeo Entity ↔ Document
- **MongoDbContext**: Contexto de base de datos compartido
- **MongoSettings**: Configuración de conexión MongoDB

### 3. **Mensajería MQTT**
- **IMqttClientService**: Interfaz de cliente MQTT
- **MqttClientService**: Implementación con MQTTnet
- **MqttSettings**: Configuración de broker MQTT
- **MqttTopics**: Constantes de topics MQTT del sistema

### 4. **DTOs Compartidos**
Contratos de comunicación entre microservicios:
- **Authentication**: Login, tokens, usuarios, roles, permisos
- **Actuator**: Comandos de actuadores, rutinas, jobs
- **Sensor**: Lecturas, nodos ESP32, variables, alertas
- **Analytics**: Requests de analytics de actuadores y sensores
- **Notifications**: Envío de emails

### 5. **Configuración de Microservicios**
- **AddHydroEspinacaMicroservice()**: Configura autenticación, Swagger, validación, health checks
- **AddStandardWebServices()**: Configuración básica de Web API
- **AddHydroEspinacaSwagger()**: Documentación Swagger con soporte de JWT
- **AddMongoSettings()**, **AddMqttSettings()**: Configuración de infraestructura

### 6. **Validaciones**
- **CustomValidators**: Validadores FluentValidation reutilizables
  - `BeValidObjectId()`: Valida ObjectId de MongoDB

### 7. **Manejo de Errores**
- **ProblemDetailsFactory**: Factory para crear ProblemDetails (RFC 7807)
- **DomainException**: Excepción base del dominio
- **CommonExceptions**: Excepciones comunes (NotFound, Validation, Unauthorized)

---

## Uso en Microservicios

### Instalación
La librería se distribuye como **NuGet package local**. Está referenciada en todos los proyectos mediante:

```xml
<PackageReference Include="HydroEspinaca.Shared" Version="1.0.0" />
```

### Configuración Típica en Program.cs

```csharp
using HydroEspinaca.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configuración completa de microservicio HydroEspinaca
builder.Services.AddHydroEspinacaMicroservice(
    configuration: builder.Configuration,
    serviceName: "ActuatorService",
    apiTitle: "Actuator Service API",
    validatorAssembly: typeof(CreateActuatorDtoValidator).Assembly
);

// Configuración de MongoDB
builder.Services.AddMongoSettings(builder.Configuration);

// Configuración de MQTT (si aplica)
builder.Services.AddMqttSettings(builder.Configuration);

var app = builder.Build();
app.Run();
```

---

## Integraciones con Microservicios

### **Auth Service**
- Usa: DTOs de autenticación, M2MTokenService, JwtKeyResolver
- Provee: Tokens JWT, gestión de usuarios/roles/permisos

### **Sensor Service**
- Usa: DTOs de sensores, BaseMongoRepository, MqttClientService
- Provee: Lecturas de sensores, gestión de ESP32 nodes

### **Actuator Service**
- Usa: DTOs de actuadores, BaseMongoRepository, MqttClientService
- Provee: Control de actuadores, ejecución de rutinas

### **BFF Service**
- Usa: DTOs de todos los servicios, M2MTokenService para auth entre servicios
- Provee: Gateway unificado para frontend

### **Notification Service**
- Usa: DTOs de notificaciones, configuración estándar
- Provee: Envío de emails

---

## Endpoints MQTT Compartidos

Definidos en `Constants/MqttTopics.cs`:

### **Actuator Topics**
- `actuator/routine/commands` - Comandos de rutinas
- `actuator/routine/completions` - Notificaciones de completitud
- `actuator/routine/notifications` - Notificaciones de estado
- `actuator/job/schedule` - Programación de jobs

### **Sensor Topics**
- `sensor/#` - Todas las lecturas de sensores (wildcard)
- `sensor/readings` - Batches de lecturas procesadas

---

## Constantes del Sistema

### **System Roles** (`Constants/SystemRoles.cs`)
- Roles de autorización del sistema

### **Client Identifiers** (`Constants/ClientIdentifiers.cs`)
- `WebApp`, `SensorService`, `ActuatorService`, `Esp32Service`, `SystemMonitor`, `AdminDashboard`

### **Actuator Constants**
- Modos: DIGITAL, PWM
- Power States: ON, OFF
- Límites de validación (duty cycle, duración, etc.)
- Configuración de cleanup de comandos

### **Sensor Constants**
- Estados: Active, Offline, Maintenance
- Configuración de retención de datos (alertas: 30 días, lecturas: 90 días)

---

## Enumeraciones Importantes

| Enum | Descripción | Uso |
|------|-------------|-----|
| `ActuatorMode` | DIGITAL, PWM | Control de actuadores |
| `PowerState` | ON, OFF | Estado de actuadores digitales |
| `ActuatorStatus` | Estados del actuador | Lifecycle management |
| `RoutineCommandStatus` | RUNNING, FINISHED | Ejecución de rutinas |
| `Esp32Status` | Active, Offline, Maintenance | Gestión de nodos ESP32 |
| `AlertSeverities` | Niveles de severidad | Sistema de alertas |
| `AlertTypes` | Tipos de alerta | Clasificación de alertas |
| `VariableTypes` | Tipos de variables de sensores | Procesamiento de lecturas |
| `AuthorizationScopes` | Scopes de autorización | Control de acceso |
| `RegulationType` | Tipos de regulación | Variables de control |

---

## Validaciones Personalizadas

### `BeValidObjectId()`
Valida que un string sea un ObjectId válido de MongoDB (24 caracteres hexadecimales).

```csharp
RuleFor(x => x.SensorId)
    .BeValidObjectId();
```

---

## Notas de Producción

### **Seguridad**
- ✅ Todos los microservicios usan JWT con validación JWKS
- ✅ Soporte para Machine-to-Machine (M2M) authentication
- ✅ Authorization basada en scopes (OAuth 2.0 style)
- ✅ Scope `system:admin` bypass automático (documentado en Swagger)
- ⚠️ **Contraseñas**: El sistema usa **BCrypt** (NO SHA256). Ver `BcryptPasswordHasher` en auth-service.

### **Persistencia**
- MongoDB 3.4.1+ requerido
- Todas las entidades implementan `IIdentifiable` con `Id` como ObjectId

### **Mensajería**
- MQTTnet 5.0.1+ requerido
- Cliente singleton compartido en cada microservicio

### **Concurrency**
- `BaseMongoRepository` no implementa optimistic concurrency por defecto
- Para operaciones críticas, implementar versioning en entidades

### **Caching**
- No hay caching implementado en la librería compartida
- Cada microservicio implementa su propia estrategia de cache si necesario

### **Logging**
- Usa `ILogger<T>` de Microsoft.Extensions.Logging
- Los errores se loggean en `BaseMongoRepository` automáticamente

---

## Versionado

La librería sigue **Semantic Versioning** (SemVer):
- **MAJOR**: Cambios breaking (incompatibles)
- **MINOR**: Nuevas funcionalidades (backwards-compatible)
- **PATCH**: Bug fixes

**Versión Actual**: `1.0.0`

---

## Dependencias Principales

```xml
<PackageReference Include="FluentValidation" Version="12.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.7" />
<PackageReference Include="Microsoft.IdentityModel.Tokens" Version="8.0.2" />
<PackageReference Include="MongoDB.Driver" Version="3.4.1" />
<PackageReference Include="MQTTnet" Version="5.0.1.1416" />
<PackageReference Include="Swashbuckle.AspNetCore.SwaggerGen" Version="9.0.3" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.0.2" />
```

---

## Build y Publicación

### Build Local
```bash
cd HydroEspinaca.Shared
dotnet build
```

### Crear NuGet Package
```bash
dotnet pack --configuration Release
```

El package se generará en `bin/Release/HydroEspinaca.Shared.{version}.nupkg`

### Actualizar en Microservicios
Después de cambios en la librería:
1. Incrementar versión en `.csproj`
2. Hacer `dotnet pack`
3. Actualizar referencias en microservicios
4. Ejecutar `dotnet restore` en cada microservicio
5. Ejecuta el script ubicado en la raíz (hydroespinaca/build-shared-and-install.sh) para hacerlo de forma automática

**Stack**: .NET 9, ASP.NET Core
**Última actualización:** 2025-10-20
