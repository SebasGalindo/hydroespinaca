# Notification Service - Sistema de Notificaciones

> Microservicio especializado en el envío de correos electrónicos con idempotencia, sanitización y soporte multi-proveedor

## 📋 Descripción

El **Notification Service** gestiona el envío de notificaciones por correo electrónico en el ecosistema HydroEspinaca. Permite enviar emails tanto a destinatarios individuales como a grupos predefinidos, garantizando idempotencia, sanitización del contenido HTML, y resiliencia mediante un sistema de fallback entre proveedores.

Este servicio procesa las notificaciones de forma asíncrona, encolándolas en memoria y enviándolas mediante un BackgroundService. Soporta dos modos de envío: directo (especificando To/Cc/Bcc) o por grupo (usando grupos de destinatarios almacenados en MongoDB). También proporciona auditoría completa de cada envío con logs persistentes.

El servicio está diseñado para producción con políticas de autenticación basadas en JWT, validación exhaustiva de inputs mediante FluentValidation, y manejo de errores centralizado.

## 🏗️ Arquitectura

```
NotificationService/
├── NotificationService.Api/           # REST API, Controllers, Middleware
│   ├── Controllers/                   # NotificationController, NotificationGroupController, DiagnosticsController
│   ├── Middleware/                    # GlobalExceptionMiddleware
│   ├── HealthChecks/                  # EmailProvidersHealthCheck
│   └── Services/                      # NotificationServiceExceptionMapper
├── NotificationService.Application/   # Use Cases, DTOs, Validators
│   ├── UseCases/                      # SendEmailUseCase
│   ├── DTOs/                          # NotificationGroupDto
│   ├── Validators/                    # SendEmailRequestValidator, NotificationGroupValidators
│   └── Interfaces/                    # IEmailNotificationService
├── NotificationService.Domain/        # Entities, Interfaces, Models
│   ├── Entities/                      # EmailMessage, NotificationGroup
│   ├── Models/                        # EmailLog, IdempotencyRecord
│   └── Interfaces/                    # IEmailSender, IEmailQueue, IIdempotencyStore, etc.
└── NotificationService.Infrastructure/# Implementaciones concretas
    ├── Transport/                     # ResendEmailSender, SmtpEmailSender, CompositeEmailSender
    ├── Persistence/                   # Repositorios MongoDB (EmailLog, Idempotency, Groups)
    ├── Workers/                       # EmailDispatcherHostedService
    ├── Templating/                    # FluidTemplateRenderer, HtmlSanitizerAdapter
    └── Templates/                     # base.liquid (layout de emails)
```

**Arquitectura:** Clean Architecture con separación clara de capas

## 🔌 Dependencias

### Bases de Datos
- **MongoDB**: `email_logs` - Auditoría de envíos (Queued/Sent/Failed)
- **MongoDB**: `idempotency` - Registro de idempotencia con TTL para evitar duplicados
- **MongoDB**: `notification_groups` - Grupos de destinatarios configurables

### Proveedores de Email
- **Resend** (Primario): API REST para envío de emails profesionales
- **SMTP** (Fallback): Gmail u otro servidor SMTP como respaldo

### Microservicios
- **Auth Service**: Validación de JWT para autenticación de requests

## 📡 Endpoints

### Notificaciones
```
POST   /api/notifications/email      # Enviar email (directo o por grupo)
```

**Ejemplo de envío directo:**
```json
{
  "to": "user@example.com",
  "cc": ["manager@example.com"],
  "bcc": [],
  "subject": "Alerta de riego",
  "htmlBody": "<p>El sensor detectó humedad baja.</p>"
}
```

**Ejemplo de envío por grupo:**
```json
{
  "group": "admin-alerts",
  "subject": "Mantenimiento programado",
  "htmlBody": "<p>El sistema estará en mantenimiento el sábado.</p>"
}
```

**Headers opcionales:**
- `Idempotency-Key`: Clave para evitar duplicados (si no se proporciona, se genera un hash del payload)

**Respuesta (202 Accepted):**
```json
{
  "id": "507f1f77bcf86cd799439011",
  "status": "queued"
}
```

### Grupos de Notificación
```
GET    /api/notification-groups           # Listar todos los grupos
GET    /api/notification-groups/{name}    # Obtener grupo por nombre
POST   /api/notification-groups           # Crear grupo
PUT    /api/notification-groups/{name}    # Actualizar grupo
DELETE /api/notification-groups/{name}    # Eliminar grupo
```

**Ejemplo de creación de grupo:**
```json
{
  "groupName": "admin-alerts",
  "description": "Administradores del sistema",
  "recipients": [
    {
      "email": "admin1@example.com",
      "type": "TO",
      "isActive": true
    },
    {
      "email": "backup@example.com",
      "type": "BCC",
      "isActive": true
    }
  ]
}
```

### Diagnóstico
```
POST   /api/diagnostics/send-test?to=email  # Envío de prueba
```

### Health
```
GET    /health                              # Health check básico
GET    /health/ready                        # Readiness con verificación de proveedores
```

## ⚙️ Configuración

### appsettings.json
```json
{
  "Mongo": {
    "ConnectionString": "mongodb://localhost:27017",
    "Database": "HydroEspinacaDB"
  },
  "Email": {
    "FromEmail": "noreply@hydroespinaca.com",
    "FromName": "HydroEspinaca",
    "Provider": "Resend",
    "Resend": {
      "ApiBaseUrl": "https://api.resend.com",
      "ApiKey": "re_your_key_here"
    },
    "Smtp": {
      "Host": "smtp.gmail.com",
      "Port": 587,
      "UseStartTls": true,
      "Username": "your-email@gmail.com",
      "Password": "app-password"
    }
  }
}
```

### Variables de Entorno (Producción)
```bash
# MongoDB
MONGO__ConnectionString=mongodb+srv://user:pass@cluster.mongodb.net/
MONGO__Database=HydroEspinacaDB

# JWT (desde Auth Service)
JWT__Issuer=http://auth-service:8080
JWT__Audience=hydroespinaca-services

# Email
EMAIL__FromEmail=noreply@hydroespinaca.com
EMAIL__FromName=HydroEspinaca
EMAIL__Resend__ApiKey=re_production_key
EMAIL__Smtp__Username=notifications@hydroespinaca.com
EMAIL__Smtp__Password=app-password

# M2M (Machine-to-Machine)
M2M__ClientId=notification-service-m2m
M2M__ClientSecret=your-secret
M2M__AuthServiceUrl=http://auth-service:8080
```

## 🚀 Ejecución

### Desarrollo Local
```bash
# Restaurar dependencias
dotnet restore

# Ejecutar con hot reload
dotnet watch run --project NotificationService.Api

# Configurar secrets
dotnet user-secrets set "Email:Resend:ApiKey" "re_your_key"
dotnet user-secrets set "Email:Smtp:Password" "your_password"
```

API disponible en `http://localhost:5285`
Swagger disponible en `http://localhost:5285/swagger`

### Docker
```bash
# Build
docker build -t notification-service:latest .

# Run
docker run -p 8080:80 \
  -e MONGO__ConnectionString=mongodb://mongo:27017 \
  -e EMAIL__Resend__ApiKey=re_key \
  notification-service:latest
```

## 🔐 Autenticación

**JWT Bearer tokens** con políticas basadas en scopes:

| Policy | Scope | Descripción |
|--------|-------|-------------|
| `NotificationSend` | `notification:send` | Enviar notificaciones |
| `NotificationRead` | `notification:read` | Consultar grupos |
| `NotificationManage` | `notification:manage` | Gestionar grupos (CRUD) |
| `NotificationDiagnostics` | `notification:diagnostics` | Endpoints de prueba |

**Nota:** El endpoint `/health` es público (AllowAnonymous).

## 📝 Notas Importantes

### Producción
- **Cola In-Memory**: La cola actual (`InMemoryEmailQueue`) solo funciona en instancia única. Para escalado horizontal, reemplazar por Redis/RabbitMQ.
- **Idempotencia TTL**: Las claves de idempotencia expiran a las 24h automáticamente mediante índice TTL de MongoDB.
- **Límites de adjuntos**: Máximo 5MB por archivo, 10MB total por email (configurado en validator).
- **Sanitización**: Todo HTML se sanitiza con HtmlSanitizer (Ganss.Xss) para prevenir XSS.
- **Rate Limiting**: Considerar agregar políticas de rate limiting en producción para evitar abuso.

### Seguridad
- Las API keys de Resend y SMTP **NUNCA** deben estar en `appsettings.json` en producción. Usar variables de entorno o secretos de Kubernetes.
- Los logs NO registran el contenido de los emails por razones de privacidad, solo metadatos (correlationId, estado, destinatario).

### Monitoreo
- Los estados de envío se guardan en `email_logs` (Queued → Sent/Failed).
- El health check `/health/ready` verifica conectividad con Resend y resolución DNS de SMTP.
- En caso de falla de ambos proveedores, el estado se marca como `Failed` con el error registrado.

### Plantillas
- El layout base (`Templates/layouts/base.liquid`) usa Fluid como motor de plantillas.
- Soporta dark mode automático mediante media queries.
- Para personalizar estilos, editar `base.liquid` directamente.

---

**Stack:** .NET 9, MongoDB, Resend, SMTP, Fluid Templates, HtmlSanitizer  
**Última actualización:** 2025-10-20
