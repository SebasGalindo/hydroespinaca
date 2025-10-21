# Auth Service - Autenticación y Autorización Centralizada

> Microservicio encargado de la autenticación de usuarios y aplicaciones M2M, gestión de roles/permisos, y emisión de tokens JWT para todo el ecosistema HydroEspinaca.

## 📋 Descripción

Auth Service es el **único punto de autenticación y autorización** del sistema HydroEspinaca. Implementa Clean Architecture con .NET 9 y soporta dos flujos de autenticación:

1. **Autenticación de Usuarios (Resource Owner Password)**: Login tradicional para usuarios web/móvil con gestión de sesiones
2. **Autenticación M2M (Client Credentials)**: Para comunicación segura entre microservicios

El servicio utiliza **JWT con claves RSA asimétricas**, permitiendo que otros microservicios validen tokens sin consultar a auth-service (verificación descentralizada). Los tokens incluyen claims de permisos basados en roles, siguiendo el modelo RBAC (Role-Based Access Control).

**Características principales**:
- Gestión completa de usuarios, roles y permisos granulares
- Sistema de sesiones con capacidad de revocación
- Reset de contraseña vía email con códigos de verificación
- Rotación de claves JWT con soporte multi-key (KID header)
- Seeding automático de datos iniciales en desarrollo
- JWKS endpoint público para validación de tokens

## 🏗️ Arquitectura

### Estructura de Capas (Clean Architecture)

```
AuthService/
├── AuthService.Api/              # Capa de Presentación
│   ├── Controllers/              # Endpoints REST
│   │   ├── AuthController.cs     # Login, refresh, M2M, JWKS
│   │   ├── UserController.cs     # CRUD de usuarios
│   │   ├── RoleController.cs     # CRUD de roles
│   │   ├── PermissionController.cs
│   │   └── SessionController.cs  # Gestión de sesiones activas
│   ├── Middleware/
│   │   └── GlobalExceptionMiddleware.cs
│   └── Services/
│       └── AuthServiceExceptionMapper.cs
│
├── AuthService.Application/      # Capa de Aplicación (CQRS)
│   ├── Features/                 # Casos de uso agrupados
│   │   ├── Authentication/
│   │   │   └── Commands/
│   │   │       ├── Login/
│   │   │       ├── RefreshToken/
│   │   │       ├── ClientCredentials/
│   │   │       ├── ChangePassword/
│   │   │       ├── ForgotPassword/
│   │   │       └── ResetPassword/
│   │   ├── Users/
│   │   │   ├── Commands/        # CreateUser, UpdateUser, DeleteUser
│   │   │   └── Queries/         # GetUser, GetAllUsers
│   │   ├── Roles/
│   │   └── Permissions/
│   └── Shared/
│       ├── Behaviors/           # ValidationBehavior (MediatR)
│       └── Validators/          # BaseValidator para FluentValidation
│
├── AuthService.Domain/           # Capa de Dominio
│   ├── Entities/                # User, Role, Permission, ClientApp,
│   │                            # RefreshToken, UserSession, PasswordResetToken
│   ├── Interfaces/              # Contratos de repositorios y servicios
│   ├── ValueObjects/            # Email, HashedPassword
│   ├── Exceptions/              # UserNotFoundException, TokenExpiredException
│   └── Settings/                # JwtSettings
│
└── AuthService.Infrastructure/   # Capa de Infraestructura
    ├── Persistence/
    │   ├── Repositories/        # Implementaciones MongoDB
    │   ├── Schemas/             # Documentos MongoDB
    │   └── Mappers/             # Entity <-> Document
    ├── Security/
    │   ├── BcryptPasswordHasher.cs
    │   ├── JwtTokenService.cs   # Generación de JWT
    │   ├── FileKeyStore.cs      # Gestión de claves RSA
    │   └── InMemoryTestKeyStore.cs
    └── Services/
        ├── AuthenticationService.cs
        ├── RefreshTokenService.cs
        ├── UserSessionService.cs
        ├── DataSeedingService.cs
        ├── HttpNotificationService.cs
        └── LiquidEmailTemplateRenderer.cs
```

### Tecnologías

- **.NET 9.0** (LTS)
- **MongoDB** (persistencia)
- **MediatR** (CQRS pattern)
- **FluentValidation** (validación de DTOs)
- **AutoMapper** (mapeo entidad-DTO)
- **BCrypt.Net** (hashing de passwords)
- **RSA JWT** (tokens asimétricos)
- **Liquid Templates** (emails)

## 🔌 Dependencias

### Servicios Externos
| Servicio | Uso | Crítico |
|----------|-----|---------|
| **MongoDB** | Persistencia de usuarios, roles, permisos, sesiones | ✅ Sí |
| **notification-service** | Envío de emails de reset de password | ⚠️ Parcial* |

*Si notification-service no está disponible, el resto de funcionalidades siguen operativas.

### Servicios que dependen de Auth Service
- **bff-service**: Autenticación de usuarios finales
- **sensor-service**: Validación de tokens M2M
- **actuator-service**: Validación de tokens M2M
- **fuzzy-service**: Validación de tokens M2M
- **notification-service**: Validación de tokens M2M

**Importante**: Los servicios consumen el endpoint `/api/auth/keys/public` (JWKS) para obtener las claves públicas y validar tokens localmente **sin necesidad de consultar a auth-service en cada request**.

### Librerías Compartidas
- **HydroEspinaca.Shared**: DTOs, extensiones, configuración común de microservicios

## 📡 Endpoints

### Autenticación (Públicos)

#### Login de Usuario
```bash
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@demo.com",
  "password": "dF^J`c'662:W",
  "sessionId": "web-session-123",
  "ipAddress": "192.168.1.1",
  "userAgent": "Mozilla/5.0..."
}

# Response 200 OK
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6InVzZXItMjAyNS0wMS0xNSJ9...",
  "refreshToken": "MTIzNDU2Nzg5MGFiY2RlZg...",
  "expiresIn": 3600
}
```

#### Refresh Token
```bash
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "MTIzNDU2Nzg5MGFiY2RlZg...",
  "clientId": "web-client",
  "sessionId": "web-session-123"
}

# Response 200 OK (mismo formato que login)
```

#### Client Credentials (M2M)
```bash
POST /api/auth/token
Content-Type: application/json

{
  "clientId": "sensor-service-m2m",
  "clientSecret": "sFv6IkmZX2V98"
}

# Response 200 OK
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6Im0ybS0yMDI1LTAxLTE1In0...",
  "expiresIn": 3600
}
```

#### JWKS (Claves Públicas)
```bash
GET /api/auth/keys/public

# Response 200 OK
{
  "keys": [
    {
      "kty": "RSA",
      "use": "sig",
      "kid": "user-2025-01-15",
      "n": "xGOr1...",
      "e": "AQAB"
    }
  ]
}
```

#### Reset de Contraseña
```bash
# 1. Solicitar código
POST /api/auth/forgot-password
Content-Type: application/json

{
  "email": "user@demo.com"
}

# Response 200 OK
{
  "message": "Si el email existe, recibirás un código de verificación"
}

# 2. Resetear con código
POST /api/auth/reset-password
Content-Type: application/json

{
  "email": "user@demo.com",
  "code": "123456",
  "newPassword": "NewSecureP@ssw0rd"
}

# Response 200 OK
{
  "message": "Contraseña actualizada exitosamente"
}
```

### Gestión de Usuarios (Requiere Autorización)

```bash
# Crear usuario (requiere permission: user:create)
POST /api/users
Authorization: Bearer {token}
Content-Type: application/json

{
  "username": "nuevo_usuario",
  "email": "nuevo@example.com",
  "password": "Secure123!",
  "roleId": "60d5ec49f1b2c8b5f8e4e7a1"
}

# Listar usuarios (requiere permission: user:read)
GET /api/users
Authorization: Bearer {token}

# Actualizar usuario (requiere permission: user:update)
PUT /api/users/{id}
Authorization: Bearer {token}

# Eliminar usuario (requiere permission: user:delete)
DELETE /api/users/{id}
Authorization: Bearer {token}
```

### Gestión de Sesiones

```bash
# Listar sesiones activas de todos los usuarios
GET /api/sessions
Authorization: Bearer {token}

# Response 200 OK
[
  {
    "userId": "60d5ec49f1b2c8b5f8e4e7a1",
    "userName": "admin@demo.com",
    "sessions": [
      {
        "sessionId": "web-session-123",
        "clientId": "web-client",
        "createdAt": "2025-01-15T10:00:00Z",
        "expiresAt": "2025-01-22T10:00:00Z",
        "lastActivity": "2025-01-15T12:30:00Z",
        "revoked": false
      }
    ]
  }
]

# Revocar sesión específica
DELETE /api/sessions/{sessionId}
Authorization: Bearer {token}
```

### Health Check

```bash
GET /health

# Response 200 OK
{
  "status": "Healthy"
}
```

## ⚙️ Configuración

### Variables de Entorno / appsettings.json

```json
{
  "Mongo": {
    "ConnectionString": "mongodb://localhost:27017",
    "Database": "hydroespinaca-auth"
  },
  "Jwt": {
    "KeysDirectory": "Keys",
    "Issuer": "HydroEspinaca.AuthService",
    "Audience": "HydroEspinaca.Services",
    "AccessTokenExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  },
  "M2M": {
    "ClientId": "auth-service-m2m",
    "ClientSecret": "Arfmk2Fk7r4f",
    "AuthServiceUrl": "http://auth-service:8080",
    "TokenEndpoint": "/api/auth/token",
    "TokenCacheDurationMinutes": 50
  },
  "NotificationService": {
    "BaseUrl": "http://notification-service:8080"
  }
}
```

### Generación de Claves RSA

El servicio requiere claves RSA en la carpeta `Keys/`:

```bash
# Generar claves para tokens de usuario
openssl genrsa -out Keys/user-private.pem 2048
openssl rsa -in Keys/user-private.pem -pubout -out Keys/user-public.pem

# Generar claves para tokens M2M
openssl genrsa -out Keys/machinetomachine-private.pem 2048
openssl rsa -in Keys/machinetomachine-private.pem -pubout -out Keys/machinetomachine-public.pem

# Configuración de KID (Key ID) en JSON
cat > Keys/user-config.json << EOF
{
  "KeyId": "user-2025-01-15",
  "PrivateKeyPath": "user-private.pem",
  "PublicKeyPath": "user-public.pem"
}
EOF

cat > Keys/machinetomachine-config.json << EOF
{
  "KeyId": "m2m-2025-01-15",
  "PrivateKeyPath": "machinetomachine-private.pem",
  "PublicKeyPath": "machinetomachine-public.pem"
}
EOF
```

**⚠️ PRODUCCIÓN**: Montar `Keys/` como volumen secreto en Kubernetes/Docker con permisos restrictivos (600).

### Datos de Seeding (Solo Development)

El servicio crea automáticamente en desarrollo:
- **2 Roles**: `admin`, `user`
- **60+ Permisos**: Scopes para todos los microservicios
- **2 Usuarios**:
  - Admin: `admin@demo.com` / `dF^J`c'662:W`
  - User: `user@demo.com` / `N16'+4a597|V!`
- **6 Clientes M2M**: Para inter-comunicación de servicios

## 🚀 Ejecución

### Desarrollo Local

```bash
# 1. Iniciar MongoDB
docker run -d -p 27017:27017 --name mongo mongo:latest

# 2. Generar claves RSA (ver sección anterior)

# 3. Configurar appsettings.Development.json
cd software-project/auth-service

# 4. Ejecutar
dotnet run --project AuthService.Api/AuthService.Api.csproj

# API disponible en: http://localhost:5018
# Swagger UI: http://localhost:5018/swagger
```

### Docker

```bash
# Build
docker build -t hydroespinaca/auth-service:latest \
  -f software-project/auth-service/Dockerfile .

# Run
docker run -d \
  -p 8080:8080 \
  -e Mongo__ConnectionString="mongodb://mongo:27017" \
  -e Mongo__Database="hydroespinaca-auth" \
  -v $(pwd)/Keys:/app/Keys:ro \
  --name auth-service \
  hydroespinaca/auth-service:latest
```

### Docker Compose (Sistema Completo)

```bash
# Desde raíz del proyecto
docker-compose up auth-service
```

## 🔐 Autenticación y Autorización

### Modelo de Permisos (RBAC)

El sistema usa permisos granulares basados en **scopes**:

| Scope | Descripción | Servicios |
|-------|-------------|-----------|
| `user:create` | Crear usuarios | auth-service |
| `user:read` | Leer información de usuarios | auth-service |
| `user:update` | Actualizar usuarios | auth-service |
| `user:delete` | Eliminar usuarios | auth-service |
| `sensor:read` | Leer datos de sensores | sensor-service |
| `sensor:write` | Escribir datos de sensores | sensor-service |
| `actuator:control` | Controlar actuadores | actuator-service |
| `command:create` | Crear comandos de actuación | actuator-service |
| `notification:send` | Enviar notificaciones | notification-service |
| `fuzzy:system:create` | Crear sistemas fuzzy | fuzzy-service |
| `system:admin` | **Acceso total al sistema** | Todos |

### Políticas de Autorización

Las políticas se configuran automáticamente en `HydroEspinaca.Shared`:

```csharp
// Ejemplo de uso en controller
[Authorize(Policy = PolicyNames.UserCreate)]
public async Task<ActionResult> CreateUser([FromBody] UserCreateDto dto)
{
    // Solo usuarios con permiso "user:create" pueden acceder
}
```

### Estructura del JWT

**Access Token (Usuario)**:
```json
{
  "sub": "60d5ec49f1b2c8b5f8e4e7a1",
  "email": "admin@demo.com",
  "name": "Administrador",
  "role": "admin",
  "permissions": [
    "user:create",
    "user:read",
    "sensor:read",
    "actuator:control"
  ],
  "session_id": "web-session-123",
  "iss": "HydroEspinaca.AuthService",
  "aud": "HydroEspinaca.Services",
  "exp": 1705329600,
  "iat": 1705326000,
  "kid": "user-2025-01-15"
}
```

**Access Token (M2M)**:
```json
{
  "client_id": "sensor-service-m2m",
  "scopes": [
    "notification:send",
    "system:health"
  ],
  "iss": "HydroEspinaca.AuthService",
  "aud": "HydroEspinaca.Services",
  "exp": 1705329600,
  "iat": 1705326000,
  "kid": "m2m-2025-01-15"
}
```

## 📝 Notas Importantes

### Seguridad

1. **Claves RSA**:
   - ✅ NO están en código fuente
   - ⚠️ **CRÍTICO**: Rotar claves cada 90 días en producción
   - ⚠️ Permisos de carpeta `Keys/`: 600 (solo lectura por proceso)
   - ⚠️ En Kubernetes: usar Secrets, nunca ConfigMaps

2. **Client Secrets M2M**:
   - 🚨 Los secrets del seeding son **SOLO PARA DESARROLLO**
   - 🚨 **OBLIGATORIO**: Cambiar secrets antes de producción
   - Usar variables de entorno o Azure KeyVault en producción

3. **Password Hashing**:
   - Usa BCrypt con salt automático
   - Factor de costo: 11 (balance seguridad/performance)

4. **Rate Limiting**:
   - ⚠️ **NO IMPLEMENTADO** - Agregar antes de producción
   - Recomendado: 5 intentos/min en `/api/auth/login`

### Performance

1. **Validación de Tokens**:
   - Los microservicios validan tokens **localmente** usando JWKS
   - NO hay llamadas a auth-service en cada request
   - Cachear JWKS con TTL de 1 hora

2. **Sesiones**:
   - ⚠️ NO hay limpieza automática de sesiones expiradas
   - Implementar background job para limpiar `UserSession` expirados
   - Considerar usar Redis para sesiones de alto volumen

3. **Password Reset Tokens**:
   - ⚠️ NO hay limpieza automática de tokens expirados
   - Tokens válidos por 15 minutos
   - Implementar limpieza periódica en `PasswordResetToken`

### Monitoreo

1. **Health Checks**:
   - Endpoint `/health` básico implementado
   - ⚠️ Agregar health check de MongoDB
   - Separar readiness vs liveness checks

2. **Logging**:
   - ⚠️ Usa `SimpleConsole` logger (no óptimo para producción)
   - Cambiar a structured logging (JSON) en producción
   - Integrar con Serilog/ELK Stack

3. **Métricas**:
   - ⚠️ NO hay métricas expuestas
   - Considerar Prometheus metrics para:
     - Login attempts (success/failed)
     - Token generation rate
     - Active sessions count

### Concurrencia

- ✅ Los repositorios MongoDB son thread-safe
- ✅ Generación de tokens es stateless y thread-safe
- ⚠️ Seeding de datos NO tiene control de concurrencia (solo se ejecuta una vez en startup de desarrollo)

### CORS

- ⚠️ Configuración de CORS delegada a `HydroEspinaca.Shared`
- Validar que BFF/frontend estén en lista de orígenes permitidos

---

**Stack**: .NET 9, MongoDB, JWT RSA, MediatR, FluentValidation  
**Última actualización:** 2025-10-20
