# BFF Service - Backend for Frontend (HydroEspinaca)

> Orquesta las llamadas a microservicios, maneja sesiones y tokens, agrega/cachea respuestas y expone una API simple para el frontend.

## 📋 Descripción

BFF Service es el punto de entrada del frontend web y móvil. Implementa patrones de BFF y API Composition sobre una arquitectura Clean Architecture (.NET 9):

- Gestiona sesiones de usuario (sessionId + csrfToken) y refresco automático de tokens JWT con el Auth Service.
- Agrega datos de múltiples micros (sensor-service, actuator-service y fuzzy-service) para exponer endpoints listos para UI.
- Aplica caché de consultas recurrentes (p.ej. permisos, analíticas, clima) y política CORS específica para frontend.
- No usa FallbackPolicy global; cada controlador valida sesión explícitamente.

## 🏗️ Arquitectura (alto nivel)

- API (Controllers, Middleware, Swagger)
- Application (Servicios de caso de uso, validaciones, caché)
- Domain (Entidades, DTOs internos, constantes, excepciones)
- Infrastructure (Http clients, proxy, repositorio de sesión en memoria)

Integraciones:
- Auth Service (login, refresh, users/roles/permissions, sesiones)
- Sensor Service (lecturas y agregados ambientales)
- Actuator Service (estado de jobs y analíticas de actuadores)
- Fuzzy Service (metadatos de reglas difusas)
- OpenWeather (clima, opcional)

## 🔌 Dependencias externas

- HTTP a microservicios internos (URLs configurables):
  - Services:AuthService:Url
  - Services:SensorService:Url
  - Services:ActuatorService:Url
  - Services:FuzzyService:Url
- M2M/Auth:
  - M2M:AuthServiceUrl, M2M:ClientId, M2M:ClientSecret
  - Jwt:Issuer, Jwt:Audience
- OpenWeather:
  - ExternalApis:OpenWeather:BaseUrl, Latitude, Longitude, ApiKey

## 📡 Endpoints principales

- auth
  - POST auth/login/web → setea cookies HttpOnly (SessionId) y accesible (CsrfToken)
  - POST auth/login/mobile → devuelve sessionId y csrfToken por headers/body
  - POST auth/logout → limpia cookies y cierra sesión si existe
  - POST auth/refresh → renueva el access token (si procede)
  - GET  auth/session → datos actuales del usuario (nombre, email, rol)
  - GET  auth/session/{sessionId} → info de sesión (válido/expirado)

- users (requiere sesión válida)
  - GET users, GET users/{id}, POST users, PUT users/{id}, DELETE users/{id}

- roles (requiere sesión válida)
  - GET roles, GET roles/{code}, POST roles, PUT roles/{code}, DELETE roles/{code}

- permissions (requiere sesión válida)
  - GET permissions, GET permissions/{code}
  - GET permissions/grouped (cache 24h)
  - POST/PUT/DELETE invalidan la caché

- system
  - GET system/status → compone: lecturas (sensor), estado jobs/estadísticas (actuator) y clima (OpenWeather)
  - GET system/fuzzy-rules → resumen de reglas (cache 24h)

- weather
  - GET weather → clima actual (cache hasta la siguiente hora)

Ejemplo JSON (system/status):

```json
{
  "readings": { "temperature": 23.4 },
  "jobStatus": { "pending": 0, "running": 0 },
  "stats": { "totalJobs": 0 },
  "internalRoutines": [],
  "weather": { "temperature": 14.0, "main": "Clouds" }
}
```

Notas de error/estados:
- 401 cuando la sesión no existe/expiró o el token fue invalidado.
- 503 si un micro rechaza el JWT de manera consistente (problema de configuración aguas abajo).
- 504 si hay timeouts en servicios internos.

## ⚙️ Configuración

Variables (ejemplos de claves; proveer por env/secret manager):

- Cookies y sesión
  - Cookies:Secure=true|false
  - Cookies:SameSite=Strict|Lax|None
  - Cookies:Domain=hydroespinaca.online (opcional)
  - Sessions:RefreshTokenExpiryDays=7
  - Sessions:AccessTokenExpiryMinutes=60
  - Sessions:SessionIdHeader=X-Session-Id
  - Sessions:CsrfTokenHeader=X-CSRF-Token

- Servicios
  - Services:AuthService:Url=http://auth-service:8080
  - Services:SensorService:Url=http://sensor-service:8080
  - Services:ActuatorService:Url=http://actuator-service:8080
  - Services:FuzzyService:Url=http://fuzzy-service:8000

- M2M/JWT
  - M2M:AuthServiceUrl=http://auth-service:8080
  - M2M:ClientId=...
  - M2M:ClientSecret=...
  - Jwt:Issuer=...
  - Jwt:Audience=...

- OpenWeather (opcional)
  - ExternalApis:OpenWeather:BaseUrl=https://api.openweathermap.org/data/2.5/weather
  - ExternalApis:OpenWeather:Latitude=4.7002001
  - ExternalApis:OpenWeather:Longitude=-74.2385058
  - ExternalApis:OpenWeather:ApiKey=... (requerida si se usa clima)

CORS:
- Dev: http://localhost, http://localhost:3000
- Prod: https://hydroespinaca.online, https://www.hydroespinaca.online

## 🚀 Ejecución

- Local (dotnet)
  - Requisitos: .NET 9 SDK
  - Ejecutar desde carpeta del proyecto API

```bash
# Backend BFF (API)
cd software-project/bff-service/BffService.Api
DOTNET_ENVIRONMENT=Development dotnet run
```

- Contenedor (Docker)

```bash
# Build (desde raíz del repo o carpeta del servicio)
docker build -t hydroespinaca/bff-service:latest software-project/bff-service

# Run (pasar variables por -e o Docker Compose)
docker run -p 8080:8080 \
  -e ASPNETCORE_URLS=http://+:8080 \
  -e Services__AuthService__Url=http://auth-service:8080 \
  -e Services__SensorService__Url=http://sensor-service:8080 \
  -e Services__ActuatorService__Url=http://actuator-service:8080 \
  -e Services__FuzzyService__Url=http://fuzzy-service:8000 \
  hydroespinaca/bff-service:latest
```

- Health
  - GET /health → liveness (puede extenderse con checks de dependencias)

## 🔐 Autenticación y sesión

- Diseño: [AllowAnonymous] en controladores y validación manual de sesión (cookie SessionId o header X-Session-Id) para máxima flexibilidad web/móvil.
- Web:
  - Cookies: SessionId (HttpOnly), CsrfToken (legible por JS)
- Móvil:
  - Headers: X-Session-Id, X-CSRF-Token
- Refresco de tokens:
  - Automático vía SessionTokenService.GetSessionWithValidTokensAsync(sessionId)
  - Concurrency-safe con semáforos por sesión

Resumen por recurso:
- auth/login/* → anónimo
- Resto de endpoints → requieren sesión válida (401 si no)

CSRF:
- Servicio ICsrfValidationService disponible pero actualmente no aplicado. Si no se requiere para MVP, puede removerse para simplificar.


## 📝 Notas Importantes (producción)

1) Concurrencia y refresh de tokens
- SessionTokenService usa un diccionario estático de SemaphoreSlim para serializar refresh por sessionId.
- Evita condiciones de carrera en llamadas concurrentes.

2) Caché
- InMemorySessionRepository: Sliding/Absolute = RefreshTokenExpiryDays
- Permisos agrupados: 24h
- Fuzzy rules: 24h
- Clima: hasta el inicio de la siguiente hora

3) Resiliencia HTTP
- Actualmente no hay políticas de retry/circuit breaker configuradas para HttpClient.
- Sugerencia: agregar Polly (retry con backoff, circuit breaker) y timeouts por cliente.

4) Seguridad (CSRF)
- Existe CsrfValidationService pero no se aplica en controladores.
- Sugerencia: si no se usará, removerlo del proyecto y del registro de DI.

5) Swagger
- OperationFilter agrega headers de sesión en AuthController.
- Sugerencia: extender a controladores que heredan de BaseAuthenticatedController.

6) Registro de HttpClient
- ServiceCollectionInfrastructureExtensions registra AddHttpClient y AddScoped para los mismos servicios.
- Sugerencia: usar sólo AddHttpClient<TClient,TImpl>() y remover AddScoped duplicado.

7) Configuración
- Asegurar ExternalApis:OpenWeather:ApiKey en prod si se usa /weather o system/status.
- Revisar Cookies:Domain/SameSite en escenarios con dominios/aplicaciones cruzadas.

---

**Stack**: .NET 9, ASP.NET Core, FluentValidation, Swashbuckle, MemoryCache
**Última actualización:** 2025-10-20