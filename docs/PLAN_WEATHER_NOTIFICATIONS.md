# Plan: Weather Service + Sistema de Notificaciones Avanzado

Fecha: 2026-02-20  
Autor: Agente de desarrollo (plan auto-ejecutable)  
Dependencias previas: Semanas 1-2 completadas (BI + Fuzzy CRUD), mobile funcional con auth.

---

## 0) Contexto y Análisis del Estado Actual

### Lo que YA existe y funciona

| Componente | Estado | Detalle |
|-----------|--------|---------|
| **BFF `GET /weather`** | ✅ Funcional | OpenWeather API v2.5 (current), cache por hora, lat/lon hardcodeados a Mosquera |
| **`WeatherDto`** (BFF) | ✅ Básico | Temperature, FeelsLike, Humidity, Main, Description, Icon, WindSpeed, Cloudiness, Rain1h, Sunrise, Sunset |
| **`WeatherSummary`** (shared TS) | ✅ Sincronizado | Mismos campos, consume `GET /weather` |
| **`WeatherService`** (shared TS) | ✅ Mínimo | Solo `getWeather()` — sin pronóstico, sin alertas |
| **`notification-service`** | ✅ Email maduro | Resend (primary) + SMTP/MailKit (fallback), idempotencia, templates Fluid, grupos de recipients, logs, Polly retry |
| **Canales de notificación** | 🔴 Solo email | No push, no WhatsApp, no web notifications |
| **Umbrales de alerta** | 🟡 Hardcodeados en frontend | `variableAlerts.ts` en web — solo visual, no backend |
| **Evaluaciones fuzzy** | ✅ Persisten en MongoDB | `fuzzy_evaluations` con inputs, activated_rules, output_values, timestamp |
| **Actuator analytics** | ✅ Maduro | `POST /api/commands/analytics` — timeline, duration, active time proportion |
| **Sensor aggregates** | ✅ Maduro | Worker periódico + `POST /api/aggregates/environmental` (hourly/daily/weekly/monthly) |

### Lo que FALTA implementar

| Funcionalidad | Prioridad | Complejidad |
|---------------|-----------|-------------|
| **weather-service** (microservicio dedicado) | Alta | Media |
| **Pronóstico 8 días** (OpenWeather One Call 3.0 — plan Developer/Student) | Alta | Baja |
| **Refactorizar notification-service a CQRS** (MediatR + Features/) | Alta | Media |
| **Alertas meteorológicas** (umbrales configurables + evaluación automática) | Alta | Media |
| **Push notifications** (Expo Push + Web Push API) | Alta | Media |
| **Resumen diario** (programable por usuario) | Alta | Media-Alta |
| **WhatsApp** (Twilio Sandbox → producción) | Media | Media |
| **Preferencias de notificación por usuario** | Alta | Baja |
| **Vistas frontend** (configuración alertas, pronóstico, historial notificaciones) | Alta | Media |

---

## 1) Objetivos y Alcance

### 1.1 Funcionalidad 1 — Integración Meteorológica

**DONE cuando:**
1. Existe un `weather-service` (.NET 9) independiente que encapsula toda interacción con OpenWeather.
2. Pronóstico de **8 días** (One Call API 3.0, plan Developer/Student) disponible vía `GET /weather/forecast`.
3. Los umbrales de alerta meteorológica se configuran **por fuzzy system activo** (no por usuario individual), ya que un sistema fuzzy puede ser gestionado por múltiples usuarios.
4. Un **worker periódico** evalúa el pronóstico contra umbrales y genera alertas proactivas.
5. Las alertas incluyen **recomendaciones** para ajustar el sistema fuzzy (textos placeholder editables).
6. Cada usuario decide en sus **preferencias de notificación** si quiere recibir alertas del sistema fuzzy al que está suscrito.
7. BFF expone endpoints para pronóstico, configuración de umbrales e historial de alertas meteo.
8. Vista web + mobile: pronóstico 8 días, configuración de umbrales, historial de alertas.

### 1.2 Funcionalidad 2 — Sistema de Notificaciones Avanzado

**DONE cuando:**
1. `notification-service` ha sido **refactorizada a CQRS con MediatR** (Features/, Commands/, Queries/, ValidationBehavior) para cumplir el estándar CQRS del proyecto.
2. `notification-service` soporta **4 canales**: Email, Push (Expo + Web Push), WhatsApp (Twilio).
3. Cada usuario tiene **preferencias** de notificación (canales habilitados, horario silencio, hora resumen diario, suscripción a alertas de su fuzzy system).
4. Push funciona end-to-end: mobile registra token → notificación llega al dispositivo.
5. Web Push funciona: navegador pide permiso → notificación nativa del OS (igual que WhatsApp Web).
6. El usuario puede programar un **resumen diario** a una hora específica con: reglas activadas, lecturas promedio, evaluaciones fuzzy realizadas, comportamiento de actuadores.
7. WhatsApp envía alertas y resumen diario (Twilio Sandbox para dev, Cloud API para prod).
8. Vista web + mobile: preferencias de notificación, historial de notificaciones enviadas.

---

## 2) Decisiones de Arquitectura

### 2.1 Nuevo microservicio: `weather-service`

**Justificación:** Seguir el principio de responsabilidad única que se ha respetado en todo el proyecto. El BFF actúa como gateway (no como proveedor de datos).

```
Frontend (web/mobile)
  ↓ GET /weather/* (BFF)
BFF (gateway, auth, cache de composición)
  ↓ HTTP interno Docker
weather-service (nuevo, .NET 9)
  ↓ HTTP externo
OpenWeather One Call API 3.0 (plan Developer/Student)
```

**Stack:**
- .NET 9, Clean Architecture (Api/Application/Domain/Infrastructure)
- `HydroEspinaca.Shared` v1.0.3-dev (auth JWT, Mongo base, ProblemDetails)
- MongoDB para persistencia (alertas, umbrales, historial pronósticos)
- `IMemoryCache` para cache de pronósticos
- `BackgroundService` para evaluación periódica de umbrales

**Puerto Docker:** 5030 (interno), no se expone públicamente (solo BFF lo consume).

### 2.2 Refactor previo: notification-service a CQRS

**Problema actual:** La capa Application de `notification-service` NO sigue CQRS. Tiene un solo `SendEmailUseCase`, el CRUD de `NotificationGroup` vive directamente en el controlador (inyecta `INotificationGroupRepository`), no usa MediatR, y los validators no están en pipeline. Esto rompe el estándar de todos los demás servicios.

**Refactoring requerido (debe hacerse ANTES de agregar nuevos features):**

1. Agregar `MediatR` NuGet a Application y Api.
2. Crear estructura `Features/` con slices:
   - `Features/Email/Commands/SendEmail/` → `SendEmailCommand`, `SendEmailCommandHandler`, `SendEmailCommandValidator`
   - `Features/NotificationGroups/Commands/Create/` → `CreateNotificationGroupCommand`, handler, validator
   - `Features/NotificationGroups/Commands/Update/` → `UpdateNotificationGroupCommand`, handler, validator
   - `Features/NotificationGroups/Commands/Delete/` → `DeleteNotificationGroupCommand`, handler
   - `Features/NotificationGroups/Queries/GetAll/` → `GetAllNotificationGroupsQuery`, handler
   - `Features/NotificationGroups/Queries/GetByName/` → `GetNotificationGroupQuery`, handler
3. Agregar `ValidationBehavior<TRequest,TResponse>` (copiar patrón de auth-service).
4. Registrar `AddMediatR()` en `ServiceCollectionApplicationExtensions.cs`.
5. Refactorear controladores para inyectar solo `IMediator` y delegar con `_mediator.Send()`.
6. Eliminar `IEmailNotificationService` (reemplazado por MediatR dispatch).
7. Mover lógica de mapeo del controlador a los handlers (o usar AutoMapper profiles, ya referenciado en csproj).

**Esto es prerrequisito (Fase 0) porque:**
- Los nuevos features (push, preferences, multi-channel) añadirán ~10 Commands/Queries más.
- Es más limpio refactorear lo existente primero y luego agregar sobre la estructura correcta.
- Evita deuda técnica acumulada.

### 2.3 Evolución de `notification-service` a multi-canal

No se crea un servicio nuevo. Se evoluciona el existente siguiendo el patrón que ya tiene (`IEmailSender` → `CompositeEmailSender`).

**Nueva abstracción:**

```csharp
// Generalización del sistema de envío
public interface INotificationChannel
{
    string ChannelType { get; }  // "email", "push", "whatsapp", "web_push"
    Task<NotificationSendResult> SendAsync(NotificationMessage message, CancellationToken ct);
    Task<bool> IsAvailableAsync(CancellationToken ct);
}

// Entidad genérica (complementa EmailMessage que sigue existiendo)
public class NotificationMessage
{
    public string CorrelationId { get; set; }
    public string UserId { get; set; }
    public string Channel { get; set; }            // "email" | "push" | "whatsapp" | "web_push"
    public string TemplateKey { get; set; }         // "weather_alert" | "daily_summary" | "system_alert"
    public string Title { get; set; }
    public string Body { get; set; }                // HTML para email, plain para push/whatsapp
    public Dictionary<string, string> Data { get; set; }  // payload extra (deeplink, etc.)
    public string? RecipientEmail { get; set; }
    public string? RecipientPhone { get; set; }
    public string? ExpoPushToken { get; set; }
    public string? WebPushSubscription { get; set; }
}
```

**Canales a implementar:**

| Canal | Proveedor | Registro | Prioridad |
|-------|-----------|----------|-----------|
| **Email** | Resend + SMTP (ya existe) | Automático (email del usuario) | ✅ Ya funciona |
| **Push (Expo)** | Expo Push API | `POST /api/push/register` con token del SDK | P1 |
| **Web Push** | Web Push Protocol (VAPID) | Service Worker + `PushSubscription` en frontend | P1 |
| **WhatsApp** | Twilio WhatsApp API | Registro manual (número verificado) | P2 |

### 2.4 Comunicación entre microservicios (weather → notification)

**Patrón elegido:** HTTP síncrono con retry (Polly).

**¿Por qué está bien?** Es el mismo patrón que ya se usa en el proyecto:
- fuzzy-service → actuator-service (HTTP POST para enviar comandos)
- BFF → todos los servicios (HTTP para proxying)
- notification-service ya tiene Polly configurado para reintentos

**Alternativas evaluadas y descartadas para este caso:**

| Patrón | Ventaja | Desventaja | Veredicto |
|--------|---------|------------|-----------|
| **HTTP directo (elegido)** | Simple, ya probado en el proyecto, response inmediato para logging | Acoplamiento temporal (si notification-service está caído, la alerta no se envía) | ✅ Polly retry (3 intentos con backoff) mitiga esto |
| **MQTT event** (`weather/alert` topic) | Desacoplado, notification-service suscribiria | Agrega complejidad (subscriber MQTT en notification-service que hoy no tiene), fire-and-forget sin confirmación | ❌ Overengineering para este caso |
| **Message queue** (RabbitMQ) | Garantía de entrega, desacoplado | Infraestructura nueva, no existe en el proyecto | ❌ Costo de complejidad no justificado |

**Implementación:** El weather-service crea un `HttpClient` tipado (`INotificationServiceClient`) que llama a `POST /api/notifications/multi`. Si falla después de 3 retries, loggea error y la alerta queda en estado `notification_failed` en MongoDB (retry manual posible).

### 2.5 OpenWeather One Call API 3.0

**API a usar:** One Call API 3.0 (plan Developer/Student — 1,000 llamadas gratis/día).

- **URL principal:** `https://api.openweathermap.org/data/3.0/onecall?lat={lat}&lon={lon}&units=metric&lang=es&appid={key}`
- **URL resumen diario:** `https://api.openweathermap.org/data/3.0/onecall/day_summary?lat={lat}&lon={lon}&date={YYYY-MM-DD}&appid={key}`

**Respuesta del endpoint `/onecall` (una sola llamada retorna TODO):**

| Bloque | Contenido | Granularidad | Rango |
|--------|-----------|-------------|-------|
| `current` | Clima actual completo | — | Ahora |
| `minutely[]` | Precipitación por minuto | 1 min | Próximos 60 min |
| `hourly[]` | Pronóstico completo por hora | 1 hora | Próximas 48h |
| `daily[]` | Pronóstico diario con min/max/resumen | 1 día | **8 días** (hoy + 7) |
| `alerts[]` | **Alertas gubernamentales** (IDEAM Colombia) | Evento | Vigentes |

**Ventajas sobre v2.5 free tier:**

| Característica | One Call 3.0 | v2.5 /forecast (free) |
|---------------|-------------|----------------------|
| Pronóstico diario | **8 días** con min/max/morn/eve/night | 5 días, solo 3h bloques |
| Pronóstico horario | **48h** (1h precisión) | 5 días a 3h |
| Precipitación minutal | **60 minutos** | No |
| Índice UV | **Sí** (current + hourly + daily max) | No |
| Punto de rocío | **Sí** | No |
| Alertas gubernamentales | **Sí** (IDEAM, 100+ agencias) | No |
| Resumen textual AI | **Sí** (campo `summary` por día) | No |
| Datos lunares | Moonrise, moonset, moon_phase | No |
| Frecuencia actualización | Cada 10 min | Cada 2h |

**Rate limits (Developer/Student):** 1,000 calls/día gratis. Con cache de 30 min, tendremos ~48 llamadas/día. Sobrante: 952 calls/día.

**Datos por cada `daily[]` entry (relevantes para umbrales):**

| Campo | Path en JSON | Unidad | Relevancia |
|-------|-------------|--------|------------|
| Temp mínima | `daily[].temp.min` | °C | Helada (<5°C) |
| Temp máxima | `daily[].temp.max` | °C | Calor extremo (>35°C) |
| Temp por período | `daily[].temp.{morn,day,eve,night}` | °C | Detalle temporal |
| Sensación térmica | `daily[].feels_like.{morn,day,eve,night}` | °C | Estrés térmico |
| Humedad | `daily[].humidity` | % | Extrema (>90% o <30%) |
| Presión | `daily[].pressure` | hPa | Cambios bruscos |
| Nubosidad | `daily[].clouds` | % | Alta (>80%) → afecta luz |
| Viento | `daily[].wind_speed` | m/s | Fuerte (>10 m/s) |
| Ráfaga | `daily[].wind_gust` | m/s | Ráfaga peligrosa |
| Prob. precipitación | `daily[].pop` | 0-1 | Alta lluvia (>0.7) |
| Lluvia total | `daily[].rain` | mm | Lluvia intensa (>10mm) |
| Índice UV máx | `daily[].uvi` | índice | UV extremo (>8) |
| Punto de rocío | `daily[].dew_point` | °C | Riesgo condensación |
| Condición | `daily[].weather[0].id` | código | 200-232: tormenta, 500-531: lluvia |
| Resumen AI | `daily[].summary` | texto | Descripción legible del clima del día |

**Alertas gubernamentales** (IDEAM para Colombia): Ya vienen en `alerts[]` con `sender_name`, `event`, `start`, `end`, `description`, `tags`. El worker las reenvía directamente como alertas del sistema.

**Detección de tormenta eléctrica:** Códigos `200-232` en `weather[0].id`:
- 200-202: tormenta con lluvia
- 210-212: tormenta seca
- 221: tormenta irregular
- 230-232: tormenta con llovizna

### 2.6 WhatsApp: Twilio vs Meta Cloud API

| Aspecto | **Twilio WhatsApp API** | **Meta Cloud API (directo)** |
|---------|------------------------|-----------------------------|
| **Integración** | SDK .NET oficial (`Twilio` NuGet), 5 líneas de código | REST API cruda, más verboso |
| **Sandbox (dev)** | Gratis, sin verificación de empresa | No tiene sandbox |
| **Producción** | Requiere número de teléfono + perfil de empresa | Requiere número + Facebook Business verification |
| **Costo por mensaje** | ~$0.005 USD + costo Meta (~$0.05 total) | Solo costo Meta (~$0.05) |
| **Plantillas de mensaje** | Se configuran en Twilio Console | Se configuran en Meta Business Suite |
| **Nombre del remitente** | Configurable: nombre + logo de empresa | Configurable igual |
| **Webhooks/delivery receipts** | Sí, integrados | Sí, más setup |
| **Soporte** | Soporte Twilio (bueno) | Soporte Meta (limitado) |

**Decisión:** Twilio para desarrollo y MVP (sandbox gratis, SDK limpio). Migrar a Meta Cloud API solo si el costo por mensaje se vuelve significativo en producción.

**Requisitos comunes para producción (ambas opciones):**
1. Número de teléfono dedicado (puede ser virtual).
2. Perfil de empresa verificado (nombre, logo, descripción — aparece en el chat del receptor).
3. Plantillas de mensaje aprobadas (pre-aprobación de Meta para mensajes proactivos).
4. El receptor debe haber iniciado conversación (o aceptar opt-in) en las últimas 24h, o el mensaje debe ser una plantilla aprobada.

---

## 3) Modelo de Datos

### 3.1 weather-service — MongoDB

#### Colección: `weather_alert_configs`

**Propósito:** Configuración de umbrales **por fuzzy system activo**. Un sistema fuzzy es compartido por múltiples usuarios — los umbrales se definen a nivel del sistema, no del usuario individual.

| Campo | Tipo | Notas |
|-------|------|-------|
| `_id` | ObjectId | |
| `fuzzy_system_id` | string (unique index) | ID del FuzzySystem al que pertenecen estos umbrales |
| `fuzzy_system_name` | string | Nombre del sistema (denormalizado para consulta rápida) |
| `alerts` | AlertThreshold[] | Array de umbrales configurados |
| `is_active` | bool | Permite desactivar todas las alertas de golpe |
| `created_by` | string | userId de quien creó la config |
| `updated_by` | string | userId de la última modificación |
| `created_at` | Date | |
| `updated_at` | Date | |

**AlertThreshold:**

| Campo | Tipo | Notas |
|-------|------|-------|
| `type` | string (enum) | `extreme_heat`, `extreme_cold`, `high_humidity`, `low_humidity`, `heavy_rain`, `thunderstorm`, `high_cloudiness`, `strong_wind`, `extreme_uv` |
| `enabled` | bool | Si está habilitada |
| `threshold_value` | double? | Valor numérico (null para tipos binarios como `thunderstorm`) |
| `comparison` | string? | `gt` (greater than) o `lt` (less than) — null para binarios |
| `recommendation` | string | Texto de recomendación (placeholder editable). Ej: "Considere ser más flexible con el termocalefactor" |

**Umbrales predefinidos (seed):**

| Tipo | Umbral default | Comparación | Recomendación placeholder |
|------|---------------|-------------|--------------------------|
| `extreme_heat` | 35°C | `gt` | "Temperatura exterior alta. Considere aumentar la flexibilidad del ventilador y reducir umbrales de activación del termocalefactor en su rutina fuzzy." |
| `extreme_cold` | 5°C | `lt` | "Riesgo de helada. Considere reducir umbrales de activación del termocalefactor y calefactor de agua para proteger los cultivos." |
| `high_humidity` | 90% | `gt` | "Humedad exterior muy alta. Considere reducir la frecuencia de la bomba de agua y el humidificador en su rutina fuzzy." |
| `low_humidity` | 30% | `lt` | "Humedad exterior muy baja. Considere ser más flexible con la activación del humidificador." |
| `heavy_rain` | 10mm/3h | `gt` | "Lluvia intensa pronosticada. Considere reducir la activación de la bomba de agua." |
| `thunderstorm` | — (binario) | — | "Tormenta eléctrica pronosticada. Revise conexiones eléctricas y considere modos de operación conservadores." |
| `high_cloudiness` | 80% | `gt` | "Nubosidad alta prolongada. Considere ser más flexible con las reglas de activación de la luz de amplio espectro." |
| `strong_wind` | 10 m/s | `gt` | "Viento fuerte pronosticado. Verifique estructuras del invernadero y ventilación." |
| `extreme_uv` | 8 (índice) | `gt` | "Índice UV extremo pronosticado. Considere activar sombras o mallas si las tiene disponibles, y evite exposición directa." |

#### Colección: `weather_alerts`

**Propósito:** Historial de alertas generadas (auditoría + vista de historial).

| Campo | Tipo | Notas |
|-------|------|-------|
| `_id` | ObjectId | |
| `fuzzy_system_id` | string | Sistema fuzzy al que aplica la alerta |
| `alert_type` | string | Tipo del umbral que se activó |
| `severity` | string | `warning` / `critical` |
| `title` | string | Título legible: "⚠️ Calor extremo pronosticado" |
| `message` | string | Descripción: "Se pronostica 37°C para el jueves 22 Feb a las 14:00" |
| `recommendation` | string | Texto de recomendación del sistema |
| `forecast_datetime` | Date | Fecha/hora del pronóstico que disparó la alerta |
| `forecast_value` | double? | Valor del pronóstico (ej: 37.2) |
| `forecast_condition` | string? | Condición textual (ej: "Tormenta con lluvia") |
| `government_alert` | bool | `true` si proviene de alertas gubernamentales (IDEAM) |
| `notified_users` | NotifiedUser[] | Usuarios a los que se envió y por qué canal |
| `created_at` | Date | |

**NotifiedUser:**

| Campo | Tipo | Notas |
|-------|------|-------|
| `user_id` | string | |
| `channels` | string[] | Canales por los que se le envió: `["email", "push"]` |
| `sent_at` | Date | |
| `is_read` | bool | Si el usuario la marcó como leída |

#### Colección: `weather_forecast_cache`

**Propósito:** Persistir el último pronóstico obtenido para no depender solo de `IMemoryCache` (resistente a reinicios).

| Campo | Tipo | Notas |
|-------|------|-------|
| `_id` | string | Fijo: `"latest_forecast"` (singleton) |
| `fetched_at` | Date | Cuándo se obtuvo |
| `expires_at` | Date | Cuándo expira (fetched_at + 30min) |
| `current` | CurrentWeather | Clima actual |
| `hourly` | HourlyDataPoint[] | 48 data points (1 por hora) |
| `daily` | DailyDataPoint[] | **8 data points** (hoy + 7 días) |
| `government_alerts` | GovernmentAlert[] | Alertas IDEAM vigentes |

**DailyDataPoint (ya viene pre-calculado por One Call 3.0):**

| Campo | Tipo |
|-------|------|
| `datetime` | Date (UTC) |
| `sunrise` | Date |
| `sunset` | Date |
| `temp_min` | double |
| `temp_max` | double |
| `temp_morn` | double |
| `temp_day` | double |
| `temp_eve` | double |
| `temp_night` | double |
| `feels_like_morn` | double |
| `feels_like_day` | double |
| `feels_like_eve` | double |
| `feels_like_night` | double |
| `humidity` | int |
| `pressure` | int |
| `dew_point` | double |
| `cloudiness` | int |
| `wind_speed` | double |
| `wind_gust` | double? |
| `uvi` | double |
| `pop` | double (0-1) |
| `rain` | double? (mm total) |
| `weather_id` | int (código OW) |
| `weather_main` | string |
| `weather_description` | string |
| `weather_icon` | string |
| `summary` | string (resumen AI de OW) |

**GovernmentAlert (directamente de One Call 3.0 `alerts[]`):**

| Campo | Tipo |
|-------|------|
| `sender_name` | string (ej: "IDEAM") |
| `event` | string (ej: "Alerta por lluvias") |
| `start` | Date |
| `end` | Date |
| `description` | string |
| `tags` | string[] |

### 3.2 notification-service — MongoDB (nuevas colecciones)

#### Colección: `notification_preferences`

**Propósito:** Preferencias de notificación por usuario.

| Campo | Tipo | Notas |
|-------|------|-------|
| `_id` | ObjectId | |
| `user_id` | string (unique index) | |
| `channels` | ChannelPreference[] | Configuración por canal |
| `daily_summary` | DailySummaryConfig | Configuración del resumen diario |
| `weather_alerts_subscription` | WeatherAlertSubscription | Suscripción a alertas meteorológicas |
| `quiet_hours` | QuietHoursConfig? | Horario de silencio (no enviar push/whatsapp) |
| `created_at` | Date | |
| `updated_at` | Date | |

**ChannelPreference:**

| Campo | Tipo | Notas |
|-------|------|-------|
| `channel` | string | `email`, `push`, `whatsapp`, `web_push` |
| `enabled` | bool | |
| `target` | string? | Email, teléfono, etc. (email se toma del user, phone es manual) |

**DailySummaryConfig:**

| Campo | Tipo | Notas |
|-------|------|-------|
| `enabled` | bool | Si quiere recibir resumen |
| `hour` | int (0-23) | Hora local (UTC-5) para enviar |
| `minute` | int (0-59) | Minuto |
| `channels` | string[] | Por qué canales enviar: `["email", "whatsapp"]` |
| `include_fuzzy_rules` | bool | Incluir reglas que se activaron |
| `include_sensor_averages` | bool | Incluir promedios de sensores |
| `include_actuator_runtime` | bool | Incluir duración de actuadores |
| `include_weather_forecast` | bool | Incluir pronóstico del día siguiente |

**QuietHoursConfig:**

| Campo | Tipo | Notas |
|-------|------|-------|
| `enabled` | bool | |
| `start_hour` | int | Ej: 22 (10 PM) |
| `end_hour` | int | Ej: 7 (7 AM) |

**WeatherAlertSubscription:**

| Campo | Tipo | Notas |
|-------|------|-------|
| `enabled` | bool | Si quiere recibir alertas meteorológicas |
| `fuzzy_system_id` | string? | Sistema fuzzy al que está suscrito (null = ninguno, se auto-detecta el activo) |
| `alert_types` | string[] | Tipos de alerta que quiere recibir: `["extreme_heat", "thunderstorm", ...]` (vacío = todas) |

#### Colección: `push_subscriptions`

**Propósito:** Tokens de dispositivos para push (Expo + Web Push).

| Campo | Tipo | Notas |
|-------|------|-------|
| `_id` | ObjectId | |
| `user_id` | string | |
| `platform` | string | `expo` / `web` |
| `token` | string | Expo push token (`ExponentPushToken[xxx]`) o Web Push subscription JSON |
| `device_name` | string? | Identificación legible del dispositivo |
| `is_active` | bool | Se desactiva si falla 3 veces consecutivas |
| `last_used_at` | Date | Último envío exitoso |
| `failure_count` | int | Contador de fallos consecutivos |
| `created_at` | Date | |

#### Colección: `notification_log` (evolución de `email_logs`)

**Propósito:** Log unificado de TODAS las notificaciones (no solo email).

| Campo | Tipo | Notas |
|-------|------|-------|
| `_id` | ObjectId | |
| `correlation_id` | string | Tracing |
| `user_id` | string | |
| `channel` | string | `email`, `push`, `whatsapp`, `web_push` |
| `template_key` | string | `weather_alert`, `daily_summary`, `system_alert` |
| `title` | string | |
| `status` | string | `queued`, `sent`, `failed`, `delivered` |
| `provider` | string? | `resend`, `smtp`, `expo`, `vapid`, `twilio` |
| `provider_message_id` | string? | |
| `error` | string? | |
| `created_at` | Date | |
| `updated_at` | Date | |

### 3.3 Nuevo scope JWT para weather-service

Agregar en `HydroEspinaca.Shared`:

| Scope | Policy | Uso |
|-------|--------|-----|
| `weather:read` | `WeatherRead` | Lectura de pronóstico y alertas |
| `weather:write` | `WeatherWrite` | Configurar umbrales |

Los scopes existentes de notification-service se mantienen: `notification:read`, `notification:send`, `notification:manage`.

Agregar nuevos:
| Scope | Policy | Uso |
|-------|--------|-----|
| `notification:preferences` | `NotificationPreferences` | CRUD de preferencias de usuario |
| `notification:push` | `NotificationPush` | Registrar/desregistrar tokens push |

---

## 4) Endpoints — Contrato de API

### 4.1 weather-service (interno, solo BFF lo consume)

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| `GET` | `/api/weather/current` | M2M | Clima actual (migrado del BFF) |
| `GET` | `/api/weather/forecast` | M2M | Pronóstico completo: current + hourly 48h + daily 8 días + alertas gov |
| `GET` | `/api/weather/forecast/daily` | M2M | Solo el bloque `daily[]` (8 días con min/max/resumen) |
| `GET` | `/api/weather/alerts/config/{fuzzySystemId}` | M2M | Umbrales configurados por fuzzy system |
| `PUT` | `/api/weather/alerts/config/{fuzzySystemId}` | M2M | Actualizar umbrales del fuzzy system |
| `POST` | `/api/weather/alerts/config/{fuzzySystemId}/seed` | M2M | Crear config con umbrales predeterminados |
| `GET` | `/api/weather/alerts?fuzzySystemId=&userId=&from=&to=&unreadOnly=` | M2M | Historial de alertas (filtrable por sistema o usuario) |
| `PATCH` | `/api/weather/alerts/{alertId}/read?userId=` | M2M | Marcar alerta como leída para un usuario específico |
| `GET` | `/health` | Anon | Health check |

### 4.2 notification-service (endpoints nuevos)

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| `POST` | `/api/notifications/push` | `notification:send` | Enviar push notification (Expo o Web) |
| `POST` | `/api/notifications/whatsapp` | `notification:send` | Enviar WhatsApp |
| `POST` | `/api/notifications/multi` | `notification:send` | Enviar a múltiples canales en paralelo |
| `POST` | `/api/push/register` | `notification:push` | Registrar token push (Expo/Web) |
| `DELETE` | `/api/push/register/{subscriptionId}` | `notification:push` | Desregistrar token |
| `GET` | `/api/push/subscriptions/{userId}` | `notification:push` | Listar dispositivos registrados |
| `GET` | `/api/preferences/{userId}` | `notification:preferences` | Obtener preferencias |
| `PUT` | `/api/preferences/{userId}` | `notification:preferences` | Actualizar preferencias |
| `GET` | `/api/notifications/log/{userId}?from=&to=&channel=` | `notification:read` | Historial de notificaciones |
| `GET` | `/api/preferences/subscribers?fuzzySystemId=` | `notification:send` | Usuarios suscritos a alertas de un fuzzy system (usado por weather-service) |

*(Los endpoints existentes de email y grupos se mantienen intactos.)*

### 4.3 BFF (endpoints nuevos / modificados)

| Método | Ruta BFF | Upstream | Descripción |
|--------|----------|----------|-------------|
| `GET` | `/weather` | `weather-service /api/weather/current` | **Migrar**: ahora llama al weather-service en vez de OpenWeather directo |
| `GET` | `/weather/forecast` | `weather-service /api/weather/forecast` | Pronóstico completo (8 días daily + 48h hourly + alertas gov) |
| `GET` | `/weather/forecast/daily` | `weather-service /api/weather/forecast/daily` | Pronóstico resumido por día (8 días) |
| `GET` | `/weather/alerts/config` | `weather-service /api/weather/alerts/config/{fuzzySystemId}` | Umbrales del fuzzy system activo del usuario |
| `PUT` | `/weather/alerts/config` | `weather-service /api/weather/alerts/config/{fuzzySystemId}` | Actualizar umbrales |
| `GET` | `/weather/alerts` | `weather-service /api/weather/alerts?userId={userId}` | Historial de alertas del usuario |
| `PATCH` | `/weather/alerts/{alertId}/read` | `weather-service /api/weather/alerts/{alertId}/read?userId=` | Marcar leída |
| `POST` | `/notifications/push/register` | `notification-service /api/push/register` | Registrar token push |
| `DELETE` | `/notifications/push/register/{id}` | `notification-service /api/push/register/{id}` | Desregistrar |
| `GET` | `/notifications/preferences` | `notification-service /api/preferences/{userId}` | Preferencias del usuario |
| `PUT` | `/notifications/preferences` | `notification-service /api/preferences/{userId}` | Actualizar preferencias |
| `GET` | `/notifications/history` | `notification-service /api/notifications/log/{userId}` | Historial de notificaciones |

---

## 5) Workers (Background Services)

### 5.1 `WeatherAlertEvaluationWorker` (weather-service)

**Responsabilidad:** Cada 30 minutos, obtener el pronóstico de One Call 3.0, evaluar contra umbrales de todos los fuzzy systems activos, y disparar notificaciones a los usuarios suscritos.

**Flujo:**

```
[Cada 30 minutos]
  │
  ├─ 1. Llamar OpenWeather One Call 3.0 /onecall
  ├─ 2. Cachear respuesta en IMemoryCache + MongoDB (weather_forecast_cache)
  ├─ 3. Obtener todos los weather_alert_configs activos (por fuzzy_system_id)
  ├─ 4. Por cada fuzzy system con alertas activas:
  │     ├─ Por cada daily[] y hourly[] data point:
  │     │     ├─ Evaluar contra cada umbral habilitado del sistema
  │     │     ├─ Si hay match → crear weather_alert
  │     │     └─ Deduplicar: no alertar sobre el mismo tipo+sistema más de 1 vez cada 6h
  │     ├─ Reenviar alertas gubernamentales (alerts[]) de IDEAM como alertas del sistema
  │     └─ Si hay alertas nuevas:
  │           ├─ Obtener usuarios suscritos al fuzzy system (vía notification-service)
  │           └─ POST a notification-service por cada usuario con canales preferidos
  └─ 5. Log de ejecución
```

**Deduplicación:** Antes de crear una alerta, verificar que no exista una del mismo `alert_type` + `fuzzy_system_id` en las últimas 6 horas. Esto evita spam si el pronóstico no cambia.

**Resolución de suscriptores:** El weather-service necesita saber qué usuarios están suscritos a un fuzzy system. Dos opciones:
- **Opción A (elegida):** weather-service llama a `notification-service GET /api/preferences/subscribers?fuzzySystemId={id}` para obtener la lista de usuarios suscritos con sus canales habilitados.
- **Opción B:** weather-service publica la alerta genérica y notification-service resuelve internamente a quién enviar.

Se elige **Opción A** porque mantiene la lógica de "qUé alerta se genera" en weather-service y la lógica de "cómo se envía" en notification-service.

**Notificación:** El weather-service llama a `notification-service POST /api/notifications/multi` pasando:
- `userId` → notification-service consulta preferencias y envía a los canales correctos
- `templateKey: "weather_alert"`
- `title`, `body`, `data` (con info del pronóstico + recomendación)

### 5.2 `DailySummaryScheduler` (notification-service)

**Responsabilidad:** Ejecutar el resumen diario a la hora exacta programada por cada usuario, usando **Quartz.NET** como scheduler.

**¿Por qué Quartz.NET y no un loop cada minuto?**

| Enfoque | CPU entre ejecuciones | Precisión | Complejidad | Reprogramable |
|---------|----------------------|-----------|-------------|---------------|
| `while(true) + Task.Delay(60s)` | Consulta MongoDB cada 60s (~1440/día por nada) | ±1 min | Baja | Requiere reiniciar worker |
| **Quartz.NET** (elegido) | **0 CPU** (thread duerme hasta el trigger) | **Exacta** | Media | **Sí**, `ITrigger.RescheduleJob()` |
| Hangfire | Similar a Quartz, requiere storage adicional | Exacta | Alta | Sí |

**Quartz.NET** usa `System.Threading.Timer` internamente — el thread literalmente duerme hasta el próximo trigger calculado. Impacto en memoria: ~2MB para el scheduler + jobs. Impacto en CPU: **cero** entre ejecuciones.

**Flujo:**

```
[Al iniciar el servicio]
  │
  ├─ 1. Cargar todos los notification_preferences con daily_summary.enabled=true
  ├─ 2. Por cada usuario, programar un Quartz CronTrigger:
  │     cron: "0 {minute} {hour} * * ?" (UTC-5)
  └─ 3. Registrar listener para cambios de preferencias (reprogramar al PUT)

[Cuando Quartz dispara el job de un usuario]
  │
  ├─ 1. Obtener token M2M del auth-service
  ├─ 2. Si include_sensor_averages: POST sensor-service /api/aggregates/environmental (view=daily, rango=ayer)
  ├─ 3. Si include_actuator_runtime: POST actuator-service /api/commands/analytics (rango=ayer)
  ├─ 4. Si include_fuzzy_rules: GET fuzzy-service /api/fuzzy-evaluations?from=ayer&to=hoy
  ├─ 5. Si include_weather_forecast: GET weather-service /api/weather/forecast/daily
  ├─ 6. Renderizar template "daily_summary" con los datos recopilados
  └─ 7. Enviar por los canales configurados en daily_summary.channels

[Cuando un usuario cambia su hora de resumen (PUT /preferences)]
  │
  └─ Reprogramar el CronTrigger con scheduler.RescheduleJob()
```

**NuGet:** `Quartz` + `Quartz.Extensions.Hosting` (integra con `IHostedService` de .NET).

**Template del resumen diario (email HTML):**

```
📊 Resumen Diario — HydroEspinaca
Fecha: {fecha}

🌡️ SENSORES (promedios de ayer)
• Temperatura ambiente: {avg}°C (min {min} — max {max})
• Humedad: {avg}% (min {min} — max {max})
• pH: {avg} (min {min} — max {max})
• [... por cada variable ...]

⚡ ACTUADORES (tiempo activo ayer)
• Ventiladores: {duracion}min ({porcentaje}% del día)
• Termocalefactor: {duracion}min ({porcentaje}% del día)
• [... por cada actuador ...]

🧠 SISTEMA FUZZY
• Evaluaciones realizadas: {count}
• Reglas activadas: {top_rules con nombre y frecuencia}
• Sistema activo: {nombre_sistema}

🌦️ PRONÓSTICO MAÑANA
• Temperatura: {min}–{max}°C
• Probabilidad lluvia: {max_pop}%
• Condición: {descripcion}

{footer con link a la app}
```

Para push/WhatsApp: versión resumida en texto plano (2-3 líneas principales).

### 5.3 `PushCleanupWorker` (notification-service)

**Responsabilidad:** Cada 24h, desactivar tokens push con `failure_count >= 3` y eliminar suscripciones inactivas de más de 90 días.

---

## 6) Implementación Push Notifications

### 6.1 Expo Push (Mobile)

**SDK:** `expo-notifications` (ya compatible con Expo SDK 54).

**Flujo de registro:**

```
[Mobile app start]
  │
  ├─ 1. Pedir permiso: Notifications.requestPermissionsAsync()
  ├─ 2. Obtener token: Notifications.getExpoPushTokenAsync({ projectId })
  ├─ 3. POST /notifications/push/register { platform: "expo", token: "ExponentPushToken[xxx]" }
  └─ 4. Configurar listener para notificaciones recibidas
```

**Envío (backend):**

```csharp
// En notification-service
public class ExpoPushSender : INotificationChannel
{
    // POST https://exp.host/--/api/v2/push/send
    // Body: { to: "ExponentPushToken[xxx]", title, body, data, sound: "default" }
    // Batch: hasta 100 tokens por request
}
```

**Dependencias mobile:**
- `expo-notifications` (ya en Expo SDK 54)  
- `expo-device` (para verificar dispositivo físico)
- `expo-constants` (para `projectId`)

### 6.2 Web Push (Browser Notifications)

**Protocolo:** Web Push API con VAPID keys.

**Flujo:**

```
[Web app]
  ├─ 1. Registrar Service Worker (sw.js) en Next.js
  ├─ 2. Pedir permiso: Notification.requestPermission()
  ├─ 3. Obtener suscripción: registration.pushManager.subscribe({ applicationServerKey: VAPID_PUBLIC })
  ├─ 4. POST /notifications/push/register { platform: "web", token: JSON.stringify(subscription) }
  └─ 5. Service Worker recibe push → showNotification()
```

**Envío (backend):**

```csharp
// En notification-service
public class WebPushSender : INotificationChannel
{
    // Usa librería WebPush (NuGet: WebPush)
    // Lee subscription (endpoint + keys.p256dh + keys.auth)
    // Envía payload cifrado vía VAPID
}
```

**NuGet:** `WebPush` (paquete .NET para Web Push Protocol).

**Nota sobre Linux/Fedora:** Las notificaciones Web Push se muestran como notificaciones nativas del desktop (igual que las de WhatsApp Web o Gmail). En Fedora con GNOME, usan el sistema de notificaciones de freedesktop (libnotify). El usuario las ve arriba a la derecha, exactamente como describió.

### 6.3 WhatsApp (Twilio)

**Flujo:**

```
[Configuración]
  ├─ 1. Usuario registra su número en preferencias de notificación
  ├─ 2. Twilio Sandbox (dev): usuario envía "join <sandbox-keyword>" al number de Twilio
  └─ 3. notification-service envía vía Twilio WhatsApp API

[Envío]
  POST https://api.twilio.com/2010-04-01/Accounts/{SID}/Messages.json
  { From: "whatsapp:+{twilio_number}", To: "whatsapp:+{user_number}", Body: "..." }
```

**NuGet:** `Twilio` (SDK oficial).

**Limitaciones Sandbox:**
- El destinatario debe enviar "join xxx" cada 72h para mantener la sesión activa.
- Para producción: Meta WhatsApp Business Platform (requiere verificación de empresa).

**Feature flag:** `WhatsApp__Enabled=true/false` en configuración. Si es `false`, el canal se omite silenciosamente.

---

## 7) Frontend — Vistas Nuevas

### 7.1 Web

#### Página: `/clima` (nueva)

| Componente | Descripción |
|-----------|-------------|
| `ForecastSection` | Cards por día (8 días) con icono de clima, temp min/max, probabilidad lluvia, UV max, resumen AI. Expandible para ver detalle hourly (48h). |
| `AlertConfigSection` | Tabla/cards de umbrales configurables con switches on/off, inputs numéricos para valor umbral, textarea para recomendación personalizada. Botón guardar. |
| `WeatherAlertHistory` | Tabla con alertas generadas: tipo, severidad, fecha pronosticada, mensaje, estado leída/no leída. Filtros por tipo y rango de fechas. |
| `CurrentWeatherCard` | (Migrar del dashboard) Card con clima actual. |

#### Página: `/notificaciones` (nueva)

| Componente | Descripción |
|-----------|-------------|
| `ChannelPreferences` | Switches para habilitar/deshabilitar cada canal (email, push, WhatsApp). Para push: botón "Habilitar notificaciones en este navegador" que invoca Web Push registration. Para WhatsApp: input del número + instrucciones del sandbox. |
| `DailySummaryConfig` | Switch para habilitar resumen diario. Time picker para hora:minuto. Checkboxes para qué incluir (sensores, actuadores, fuzzy, pronóstico). Selector de canales para el resumen. |
| `QuietHoursConfig` | Switch + selectores de hora inicio/fin. |
| `DeviceList` | Lista de dispositivos registrados para push (nombre, plataforma, último uso). Botón desregistrar. |
| `NotificationHistory` | Tabla paginada de notificaciones enviadas: canal, tipo, título, estado, fecha. Filtros. |

#### Modificación: Dashboard

- Agregar un widget de "Alertas Meteorológicas" con badge de conteo de no leídas.
- Link a `/clima` para más detalles.

### 7.2 Mobile (Atomic Design)

Siguiendo la arquitectura molecular existente (`atoms/` → `molecules/` → `organisms/` → `components/<feature>/` → `screens/`):

#### Nuevos Atoms (si no existen ya):

| Atom | Descripción | Reuso |
|------|-------------|-------|
| `TimePicker` | Selector de hora:minuto nativo | DailySummaryForm, QuietHoursForm |
| `ProgressBar` | Barra de progreso (para pop, uvi) | ForecastDataRow |
| `WeatherIcon` | Wrapper de icono OW con fallback | ForecastDayCard, WeatherCard |

#### Nuevas Molecules:

| Molecule | Compone | Descripción |
|----------|---------|-------------|
| `ForecastDayCard` | `Card` + `WeatherIcon` + `Text` + `Badge` | Card compacta de un día: icono, temp min/max, pop, condición |
| `AlertThresholdRow` | `ListItem` + `Switch` + `Input` | Fila editable de un umbral: switch on/off + valor numérico |
| `WeatherAlertItem` | `ListItem` + `Badge` + `StatusIndicator` | Ítem de alerta en historial: tipo, severidad, fecha, leído/no leído |
| `ChannelToggleRow` | `ListItem` + `Switch` + `Icon` | Fila de canal de notificación con icono y toggle |
| `NotificationHistoryItem` | `ListItem` + `Badge` + `Text` | Ítem de historial: canal, título, estado, fecha |

#### Nuevos Organisms:

| Organism | Compone | Descripción |
|----------|---------|-------------|
| `ForecastCarousel` | `ScrollView` + `ForecastDayCard[]` | Carrusel horizontal de 8 días |
| `AlertConfigSheet` | `BottomSheetForm` + `AlertThresholdRow[]` + `TextArea` | Sheet para editar umbrales y recomendación de un tipo |
| `DailySummaryConfigSheet` | `BottomSheetForm` + `TimePicker` + `Checkbox[]` + `ChannelToggleRow[]` | Config del resumen diario |
| `QuietHoursSheet` | `BottomSheetForm` + `Switch` + `TimePicker` ×2 | Config horarios de silencio |

#### Feature Components (`components/weather/` y `components/notifications/`):

| Componente | Directorio | Descripción |
|-----------|------------|-------------|
| `ForecastSection` | `weather/` | `ForecastCarousel` + `CurrentWeatherCard` (reutiliza `WeatherCard` existente) |
| `AlertConfigList` | `weather/` | `FlatList` de `AlertThresholdRow` + botón abrir `AlertConfigSheet` |
| `WeatherAlertList` | `weather/` | `FlatList` de `WeatherAlertItem` con pull-to-refresh y `EmptyState` fallback |
| `GovernmentAlertBanner` | `weather/` | `Alert` molecule mostrando alertas IDEAM vigentes |
| `ChannelPreferences` | `notifications/` | Lista de `ChannelToggleRow` para cada canal |
| `DeviceList` | `notifications/` | `FlatList` de dispositivos push registrados con swipe-to-delete |
| `NotificationHistory` | `notifications/` | `FlatList` paginada de `NotificationHistoryItem` con filtros |

#### Pantallas:

| Pantalla | Tab/Stack | Descripción |
|----------|-----------|-------------|
| `WeatherScreen` | `DashboardStack` o nuevo `WeatherStack` | `ScreenLayout` con `ForecastSection` + `AlertConfigList` + `WeatherAlertList` |
| `WeatherAlertDetailScreen` | `WeatherStack` | Detalle de una alerta con recomendación completa |
| `NotificationSettingsScreen` | `MoreStack` | `ScreenLayout` con `ChannelPreferences` + `DailySummaryConfigSheet` trigger + `QuietHoursSheet` trigger + `DeviceList` |
| `NotificationHistoryScreen` | `MoreStack` | `ScreenLayout` con `NotificationHistory` paginado |

#### Navegación:

- Agregar `WeatherStack` en `navigation/stacks/WeatherStack.tsx` con `WeatherScreen` → `WeatherAlertDetailScreen`.
- Agregar screens a `MoreStack` para `NotificationSettingsScreen` → `NotificationHistoryScreen`.
- Registrar nuevos param lists en `navigation/types.ts`.
- Agregar tab en `MainTabNavigator.tsx` (icono clima) o nested en Dashboard.

#### Integración Push:

- `MobileAuthInitializer` (ya existe) → agregar registro de push token al montar.
- Listener de notificaciones recibidas → mostrar in-app banner.
- Listener de notificación presionada → deep link a la pantalla relevante.

### 7.3 Shared TS Package (nuevos tipos y servicios)

#### Tipos nuevos:

```typescript
// types/weather.ts (extensiones para One Call 3.0)
export interface HourlyForecast { ... }         // Un punto horario (48h)
export interface DailyForecast { ... }          // Un día completo (min/max/morn/eve/night, uvi, dew_point, summary)
export interface GovernmentAlert { ... }        // Alerta IDEAM
export interface ForecastResponse {             // Respuesta completa de /forecast
  current: WeatherSummary;
  hourly: HourlyForecast[];
  daily: DailyForecast[];                       // 8 días
  governmentAlerts: GovernmentAlert[];
}
export interface WeatherAlertConfig { ... }     // Configuración de umbrales (por fuzzy system)
export interface AlertThreshold { ... }         // Un umbral individual
export interface WeatherAlert { ... }           // Alerta generada

// types/notification.ts (nuevo archivo)
export interface NotificationPreferences { ... }
export interface ChannelPreference { ... }
export interface DailySummaryConfig { ... }
export interface QuietHoursConfig { ... }
export interface WeatherAlertSubscription { ... }
export interface PushSubscription { ... }
export interface NotificationLogEntry { ... }
export type NotificationChannel = 'email' | 'push' | 'whatsapp' | 'web_push';
export type AlertType = 'extreme_heat' | 'extreme_cold' | 'high_humidity' | 'low_humidity'
                      | 'heavy_rain' | 'thunderstorm' | 'high_cloudiness' | 'strong_wind'
                      | 'extreme_uv' | 'government';
```

#### Servicios API nuevos:

```typescript
// api/weatherService.ts (extender)
class WeatherService extends BaseApiService {
  getWeather(): Promise<WeatherSummary>                           // ya existe
  getForecast(): Promise<ForecastResponse>                        // nuevo
  getDailyForecast(): Promise<DailyForecast[]>                    // nuevo
  getAlertConfig(): Promise<WeatherAlertConfig>                   // nuevo
  updateAlertConfig(config: WeatherAlertConfig): Promise<void>    // nuevo
  getAlerts(params?: AlertFilterParams): Promise<WeatherAlert[]>  // nuevo
  markAlertRead(alertId: string): Promise<void>                   // nuevo
}

// api/notificationService.ts (nuevo)
class NotificationApiService extends BaseApiService {
  getPreferences(): Promise<NotificationPreferences>                      // nuevo
  updatePreferences(prefs: NotificationPreferences): Promise<void>        // nuevo
  registerPushToken(registration: PushRegistration): Promise<void>        // nuevo
  unregisterPushToken(subscriptionId: string): Promise<void>              // nuevo
  getDevices(): Promise<PushSubscription[]>                               // nuevo
  getNotificationHistory(params?: HistoryParams): Promise<NotificationLogEntry[]>  // nuevo
}
```

#### Stores Zustand nuevos:

```typescript
// store/weatherStore.ts (nuevo)
// Estado: forecast, dailyForecast, alertConfig, alerts, loading states
// Acciones: fetchForecast, fetchDailyForecast, fetchAlertConfig, updateAlertConfig,
//           fetchAlerts, markAlertRead

// store/notificationStore.ts (nuevo)
// Estado: preferences, devices, history, loading states
// Acciones: fetchPreferences, updatePreferences, registerPushToken,
//           unregisterPushToken, fetchDevices, fetchHistory
```

---

## 8) Plan de Implementación por Fases

### Fase 0 — Refactorizar notification-service a CQRS (Día 1)

**Prerrequisito obligatorio.** La capa Application actual no sigue CQRS. Refactorear ANTES de agregar features nuevos.

| # | Tarea | Estimación | Detalle |
|---|-------|-----------|--------|
| 0.1 | Agregar `MediatR` + `MediatR.Extensions.Microsoft.DependencyInjection` a Application.csproj y Api.csproj | 0.5h | |
| 0.2 | Crear `Shared/Behaviors/ValidationBehavior.cs` (copiar patrón de auth-service) | 0.5h | Pipeline behavior para FluentValidation automático |
| 0.3 | Crear `Features/Email/Commands/SendEmail/` → Command, Handler, Validator | 1.5h | Migrar lógica de `SendEmailUseCase` al handler |
| 0.4 | Crear `Features/NotificationGroups/Commands/Create/` → Command, Handler, Validator | 1h | Extraer del controller |
| 0.5 | Crear `Features/NotificationGroups/Commands/Update/` → Command, Handler, Validator | 1h | Extraer del controller |
| 0.6 | Crear `Features/NotificationGroups/Commands/Delete/` → Command, Handler | 0.5h | |
| 0.7 | Crear `Features/NotificationGroups/Queries/GetAll/` + `GetByName/` | 1h | |
| 0.8 | Registrar `AddMediatR()` + `ValidationBehavior` en DI | 0.5h | |
| 0.9 | Refactorear `NotificationGroupController` y `EmailController` para usar solo `IMediator` | 1h | Eliminar inyección directa de repos |
| 0.10 | Eliminar `IEmailNotificationService` y `SendEmailUseCase` (reemplazados) | 0.5h | |
| 0.11 | Verificar endpoints existentes siguen funcionando (smoke test) | 0.5h | |

**Entregable Fase 0:** notification-service internamente reestructurada con CQRS+MediatR. Endpoints idénticos externamente.

### Fase 1 — weather-service scaffold + pronóstico (Día 2-3)

| # | Tarea | Estimación | Detalle |
|---|-------|-----------|---------|
| 1.1 | Scaffold `weather-service` (.NET 9 Clean Architecture) | 2h | Api, Application, Domain, Infrastructure. NuGet refs a Shared. Dockerfile. |
| 1.2 | Migrar lógica de OpenWeather desde BFF | 1h | Mover `WeatherService.cs`, `WeatherDto`, crear `OpenWeatherClient` en Infrastructure |
| 1.3 | Implementar `GET /api/weather/current` | 1h | Cache por hora (migración directa) |
| 1.4 | Implementar `GET /api/weather/forecast` | 2h | Llamar a One Call 3.0 `/onecall`, parsear current + hourly 48h + daily 8 días + alerts gov, cache 30min |
| 1.5 | Implementar `GET /api/weather/forecast/daily` | 1h | Retornar solo el bloque `daily[]` (ya viene pre-calculado con min/max/resumen de One Call 3.0) |
| 1.6 | Docker-compose: agregar weather-service | 0.5h | Puerto 5030, env vars, depends_on mosquitto/mongo |
| 1.7 | BFF: crear `IWeatherServiceClient` + cambiar `WeatherController` | 2h | El BFF ahora llama a weather-service en vez de a OpenWeather directo |
| 1.8 | Shared NuGet: agregar scopes `weather:read`, `weather:write` | 0.5h | |
| 1.9 | Smoke test: pronóstico llega al BFF → Postman | 0.5h | |

**Entregable Fase 1:** `GET /weather/forecast` funcional desde BFF con 8 días de pronóstico. Frontend sigue mostrando clima actual (no romper nada).

### Fase 2 — Alertas meteorológicas + worker (Día 3-4)

| # | Tarea | Estimación | Detalle |
|---|-------|-----------|---------|
| 2.1 | Domain: entidades `WeatherAlertConfig` (por fuzzy_system_id), `WeatherAlert`, `ForecastCache` | 1.5h | |
| 2.2 | Infrastructure: repositorios MongoDB | 1.5h | Tres colecciones con índices (unique en fuzzy_system_id para configs) |
| 2.3 | Application: CRUD de alert configs por fuzzy system (`CreateDefaultConfigCommand`, `UpdateConfigCommand`, `GetConfigQuery`) | 2h | CQRS con MediatR |
| 2.4 | Application: `GetAlertsQuery` con filtros (por fuzzy_system_id o userId) | 1h | |
| 2.5 | Application: `MarkAlertReadCommand` (por alertId + userId) | 0.5h | |
| 2.6 | Api: `WeatherAlertController` con todos los endpoints | 1.5h | |
| 2.7 | Infrastructure: `WeatherAlertEvaluationWorker` | 3h | Fetch One Call 3.0 → evaluar por fuzzy system → deduplicar → notificar suscriptores |
| 2.8 | Infrastructure: `INotificationServiceClient` (HttpClient tipado + Polly retry ×3) | 1h | Comunicación HTTP weather → notification |
| 2.9 | BFF: exponer endpoints de alertas meteo (fuzzy_system_id derivado del usuario autenticado) | 1.5h | |
| 2.10 | Seed: umbrales predefinidos con recomendaciones | 1h | |

**Entregable Fase 2:** Worker evalúa pronóstico y crea alertas. Endpoints de configuración funcionan.

### Fase 3 — notification-service multi-canal (Día 4-6)

| # | Tarea | Estimación | Detalle |
|---|-------|-----------|---------|
| 3.1 | Domain: `INotificationChannel`, `NotificationMessage`, `NotificationSendResult` | 1h | Abstracción multi-canal |
| 3.2 | Domain: entidades `NotificationPreference`, `PushSubscription`, `NotificationLog` | 1.5h | |
| 3.3 | Infrastructure: `ExpoPushSender` (Expo Push API) | 2h | POST a `https://exp.host/--/api/v2/push/send` |
| 3.4 | Infrastructure: `WebPushSender` (VAPID) | 2h | NuGet `WebPush` |
| 3.5 | Infrastructure: `TwilioWhatsAppSender` | 2h | NuGet `Twilio`, feature flag |
| 3.6 | Infrastructure: `CompositeNotificationDispatcher` | 1.5h | Orquesta envío a múltiples canales según preferencias |
| 3.7 | Infrastructure: repos MongoDB para nuevas colecciones | 1.5h | |
| 3.8 | Application: `SendMultiChannelNotificationCommand` + Handler (CQRS) | 2h | Consulta preferencias → envía a canales habilitados |
| 3.9 | Application: CRUD de preferencias (Commands/Queries en CQRS) | 1.5h | `UpdatePreferencesCommand`, `GetPreferencesQuery` |
| 3.10 | Application: CRUD de push subscriptions (Commands/Queries en CQRS) | 1h | `RegisterPushCommand`, `UnregisterPushCommand`, `GetSubscriptionsQuery` |
| 3.11 | Application: `GetNotificationHistoryQuery` | 1h | |
| 3.12 | Application: `GetSubscribersByFuzzySystemQuery` | 1h | Para que weather-service resuelva a quién notificar |
| 3.13 | Api: controladores nuevos (`PushController`, `PreferencesController`, `MultiNotificationController`) | 2h | Todos inyectan solo `IMediator` |
| 3.14 | Config: VAPID keys generation + env vars | 0.5h | `npx web-push generate-vapid-keys` |
| 3.15 | Config: Twilio env vars (Account SID, Auth Token, WhatsApp number) | 0.5h | Feature-flagged |
| 3.16 | BFF: exponer endpoints de notificaciones | 2h | |

**Entregable Fase 3:** Push y WhatsApp funcionales. Alertas meteo llegan por el canal preferido del usuario.

### Fase 4 — Resumen diario con Quartz.NET (Día 6-7)

| # | Tarea | Estimación | Detalle |
|---|-------|-----------|---------|
| 4.1 | Agregar `Quartz` + `Quartz.Extensions.Hosting` NuGet a notification-service | 0.5h | |
| 4.2 | Infrastructure: `DailySummaryJob : IJob` (Quartz job) | 2h | Compila datos de todos los servicios y envía |
| 4.3 | Infrastructure: `DailySummarySchedulerService` (HostedService) | 2h | Carga preferencias al iniciar, programa CronTriggers, reprograma en PUT |
| 4.4 | Infrastructure: `DailySummaryDataAggregator` | 3h | Llama a sensor-service, actuator-service, fuzzy-service, weather-service |
| 4.5 | Infrastructure: templates `daily_summary.liquid` (email) | 1.5h | Template Fluid con el formato de la sección 5.2 |
| 4.6 | Infrastructure: formatter texto plano (push/WhatsApp) | 1h | Versión corta para canales de texto |
| 4.7 | Integration test: simular trigger Quartz, verificar que llega | 1.5h | |

**Entregable Fase 4:** Resumen diario funcional a la hora exacta configurada por el usuario (Quartz.NET). Reprogramable sin reinicio.

### Fase 5 — Frontend Web (Día 7-9)

| # | Tarea | Estimación | Detalle |
|---|-------|-----------|---------|
| 5.1 | Shared TS: tipos weather extendidos + notification types | 2h | |
| 5.2 | Shared TS: `WeatherService` extendido (forecast, alerts, config) | 1.5h | |
| 5.3 | Shared TS: `NotificationApiService` nuevo | 1.5h | |
| 5.4 | Shared TS: `weatherStore` + `notificationStore` (Zustand) | 2h | |
| 5.5 | Web: página `/clima` completa | 4h | ForecastSection + AlertConfigSection + AlertHistory |
| 5.6 | Web: página `/notificaciones` completa | 4h | ChannelPreferences + DailySummaryConfig + DeviceList + History |
| 5.7 | Web: Service Worker para Web Push | 2h | `public/sw.js` + registration en Next.js |
| 5.8 | Web: Widget alertas meteo en Dashboard | 1h | Badge con conteo no leídas |
| 5.9 | Web: navegación actualizada (SideNav + BottomNav) | 0.5h | |
| 5.10 | Shared: rebuild dist para web | 0.5h | `cd packages/shared && pnpm build` |

**Entregable Fase 5:** Web completa con pronóstico, alertas configurables, notificaciones Web Push.

### Fase 6 — Frontend Mobile (Día 8-9)

| # | Tarea | Estimación | Detalle |
|---|-------|-----------|---------|
### Fase 7 — Testing + Polish (Día 12-13)

| # | Tarea | Estimación | Detalle |
|---|-------|-----------|---------|
| 7.1 | Tests unitarios weather-service (domain + application) | 2h | |
| 7.2 | Tests unitarios notification-service (nuevos canales) | 2h | |
| 7.3 | Tests de integración BFF → weather-service | 1.5h | |
| 7.4 | Smoke test E2E: alerta meteo → push llega | 1h | |
| 7.5 | Smoke test E2E: resumen diario → email llega | 1h | |
| 7.6 | Documentación XML (C# services nuevos/modificados) | 2h | |
| 7.7 | Limpieza debug logs en shared package (los que agregamos hoy) | 0.5h | |
| 7.8 | SonarQube scan + fix code smells | 1h | |

---

## 9) Configuración y Variables de Entorno

### 9.1 weather-service

```yaml
# docker-compose.yml
weather-service:
  build:
    context: .
    dockerfile: software-project/weather-service/Dockerfile
  container_name: weather-service
  expose:
    - "8080"
  ports:
    - "5030:8080"
  depends_on:
    - mongodb
  environment:
    - ASPNETCORE_ENVIRONMENT=${ENVIRONMENT}
    - MongoDb__ConnectionString=mongodb://mongodb:27017
    - MongoDb__DatabaseName=hydroespinaca_weather
    - ExternalApis__OpenWeather__BaseUrl=https://api.openweathermap.org/data/3.0
    - ExternalApis__OpenWeather__ApiKey=${OPENWEATHER_API_KEY}
    - ExternalApis__OpenWeather__Latitude=4.7002001
    - ExternalApis__OpenWeather__Longitude=-74.2385058
    - Weather__ForecastCacheMinutes=30
    - Weather__AlertEvaluationIntervalMinutes=30
    - Weather__AlertDeduplicationHours=6
    - Services__NotificationService__Url=http://notification-service:8080
    - Jwt__Authority=${JWT_AUTHORITY}
    - Jwt__Audience=${JWT_AUDIENCE}
  profiles:
    - development
    - production
```

### 9.2 notification-service (env vars nuevos)

```yaml
# Agregar al bloque existente de notification-service
    - Push__Expo__Enabled=true
    - Push__WebPush__Enabled=true
    - Push__WebPush__VapidPublicKey=${VAPID_PUBLIC_KEY}
    - Push__WebPush__VapidPrivateKey=${VAPID_PRIVATE_KEY}
    - Push__WebPush__VapidSubject=mailto:admin@hydroespinaca.online
    - WhatsApp__Enabled=${WHATSAPP_ENABLED:-false}
    - WhatsApp__Twilio__AccountSid=${TWILIO_ACCOUNT_SID}
    - WhatsApp__Twilio__AuthToken=${TWILIO_AUTH_TOKEN}
    - WhatsApp__Twilio__FromNumber=${TWILIO_WHATSAPP_NUMBER}
    - DailySummary__Enabled=true
    - DailySummary__TimezoneOffset=-5
    - Quartz__Scheduler__InstanceName=NotificationScheduler
    - Services__SensorService__Url=http://sensor-service:8080
    - Services__ActuatorService__Url=http://actuator-service:8080
    - Services__FuzzyService__Url=http://fuzzy-service:8000
    - Services__WeatherService__Url=http://weather-service:8080
```

### 9.3 BFF (env var nuevo)

```yaml
# Agregar al bloque existente de bff-service
    - Services__WeatherService__Url=http://weather-service:8080
# Nota: eliminar ExternalApis__OpenWeather__ApiKey del BFF (ya no la necesita)
```

### 9.4 Archivo `env` (root)

```properties
# Agregar:
VAPID_PUBLIC_KEY=<generado con npx web-push generate-vapid-keys>
VAPID_PRIVATE_KEY=<generado>
WHATSAPP_ENABLED=false
TWILIO_ACCOUNT_SID=
TWILIO_AUTH_TOKEN=
TWILIO_WHATSAPP_NUMBER=
```

---

## 10) Diagrama de Flujo: Alerta Meteorológica End-to-End

```
┌────────────────────────────────────────────────────────────────────┐
│  weather-service (WeatherAlertEvaluationWorker, cada 30min)       │
│                                                                    │
│  1. GET api.openweathermap.org/data/3.0/onecall                   │
│  2. Parsear current + hourly[48] + daily[8] + alerts[]            │
│     → cache en Mongo + Memory (30min TTL)                         │
│  3. Load all active WeatherAlertConfigs (por fuzzy_system_id)     │
│  4. Por cada fuzzy system, por cada daily[] + hourly[]:           │
│     ¿temp.max > 35? ¿temp.min < 5? ¿pop > 0.7? ¿uvi > 8?         │
│     ¿weather_id 200-232? ¿clouds > 80? ¿wind_speed > 10?          │
│  5. Reenviar alerts[] gubernamentales (IDEAM) como alertas        │
│  6. Deduplicar (no repetir mismo tipo+sistema en 6h)              │
│  7. Persistir WeatherAlert en MongoDB                              │
│  8. GET notification-service /subscribers?fuzzySystemId={id}       │
│  9. POST notification-service /api/notifications/multi por user   │
└────────────────────────┬───────────────────────────────────────────┘
                         │
                         ▼
┌────────────────────────────────────────────────────────────────────┐
│  notification-service (SendMultiChannelNotificationCommand)        │
│  [CQRS: command va a MediatR → handler]                          │
│                                                                    │
│  1. Load NotificationPreferences del userId                        │
│  2. Filtrar canales habilitados + verificar quiet hours             │
│  3. Filtrar alert_types suscritos por el usuario                   │
│  4. Enviar en paralelo (Task.WhenAll):                             │
│     ├─ Email (Resend/SMTP) → HTML con template weather_alert      │
│     ├─ Expo Push → título + body + data (deeplink /clima)         │
│     ├─ Web Push → VAPID push con título + body                    │
│     └─ WhatsApp (Twilio) → texto plano con recomendación          │
│  5. Log cada envío en notification_log                              │
└────────────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────┐
│  Usuario recibe:                             │
│  📱 Push en celular (Expo)                  │
│  🖥️ Notificación OS en desktop (Web Push)   │
│  📧 Email con template HTML profesional     │
│  💬 WhatsApp con texto y recomendación      │
└─────────────────────────────────────────────┘
```

---

## 11) Riesgos y Mitigaciones

| # | Riesgo | Probabilidad | Impacto | Mitigación |
|---|--------|-------------|---------|------------|
| 1 | One Call 3.0 1,000 calls/día gratis excedido | Muy bajo | Bajo | Cache 30min = ~48 calls/día. Sobrante: 952 calls/día. Monitor con métrica |
| 2 | Twilio WhatsApp Sandbox expire (72h) | Alto en dev | Medio | Feature flag disabled por defecto. Documentar instrucciones de renovación. Push es el MVP |
| 3 | Web Push bloqueado por navegador | Medio | Medio | Fallback a email siempre activo. UI muestra instrucciones si permiso denegado |
| 4 | DailySummaryJob falla al llamar a múltiples servicios | Medio | Medio | Try/catch por sección: si falla sensores, enviar sin esa sección con nota "datos no disponibles" |
| 5 | Token Expo Push caduca o se invalida | Medio | Bajo | `PushCleanupWorker` desactiva tras 3 fallos + re-registro al abrir app |
| 6 | Spam de alertas si pronóstico no cambia | Medio | Alto | Deduplicación por tipo+fuzzy_system+ventana de 6h. Config max alertas/día por sistema |
| 7 | Shared NuGet requiere nuevo build para scopes | Bajo | Bajo | Agregar scopes, rebuild NuGet, actualizar refs en todos los servicios |
| 8 | Refactoring CQRS rompe endpoints existentes | Bajo | Alto | Fase 0 incluye smoke tests. Contratos HTTP no cambian, solo internals |
| 9 | Quartz.NET no persiste jobs al reiniciar container | Medio | Medio | `DailySummarySchedulerService` recarga preferencias de MongoDB al inicio (stateless recovery) |
| 10 | weather-service no puede resolver suscriptores si notification-service está caído | Bajo | Medio | La alerta se persiste de todas formas. Retry con Polly. Notificación pendiente se puede reenviar |

---

## 12) Dependencias entre Fases

```
Fase 0 (CQRS refactoring notification-service)
  │
  └──→ Fase 3 (notification multi-canal sobre CQRS)
         │
         ├──→ Fase 4 (resumen diario + Quartz.NET)
         │
  Fase 1 (weather scaffold + One Call 3.0)
  │
  └──→ Fase 2 (alertas meteo por fuzzy_system_id + worker)
         │
         └──→ Fase 5 (frontend web) ──→ Fase 6 (frontend mobile Atomic Design)

Fase 7 (testing) requiere todo lo anterior.
```

**Trabajo paralelizable:**
- **Fase 0 + Fase 1** pueden arrancar en paralelo (refactoring notification-service y scaffold weather-service son independientes).
- Frontend web (Fase 5) puede avanzar shared TS types mientras los backends se completan.
- Fase 2 depende de Fase 1 pero NO de Fase 0.

---

## 13) Checklist de Verificación Final

**Fase 0 — CQRS Refactoring:**
- [ ] notification-service usa MediatR para todos los endpoints
- [ ] CRUD de NotificationGroups pasa por Commands/Queries (no directo en controller)
- [ ] ValidationBehavior automático en pipeline
- [ ] Endpoints existentes siguen funcionando idénticamente

**Fase 1-2 — Weather Service:**
- [ ] `GET /weather` sigue funcionando (ahora via weather-service, no directo OpenWeather)
- [ ] `GET /weather/forecast` retorna pronóstico de **8 días** (One Call 3.0 `daily[]`)
- [ ] `GET /weather/forecast/daily` retorna resumen diario con min/max/resumen AI
- [ ] Alertas gubernamentales (IDEAM) incluidas en la respuesta
- [ ] UV index y dew point disponibles en el pronóstico
- [ ] Umbrales configurables **por fuzzy system** (no por usuario)
- [ ] Worker evalúa pronóstico cada 30min y genera alertas
- [ ] Deduplicación de alertas funciona (no spam, ventana 6h por tipo+sistema)
- [ ] Comunicación HTTP weather → notification con Polly retry ×3

**Fase 3-4 — Notifications Multi-canal:**
- [ ] Alertas llegan por push (Expo) al mobile
- [ ] Alertas llegan por Web Push al browser (notificación OS nativa)
- [ ] Alertas llegan por email
- [ ] WhatsApp funciona si está habilitado (feature flag)
- [ ] Resumen diario se envía a la hora **exacta** configurada (Quartz.NET)
- [ ] Resumen reprogramable al cambiar preferencias (sin reinicio)
- [ ] Resumen incluye: sensores, actuadores, fuzzy, pronóstico
- [ ] Preferencias de notificación persistidas por usuario (incluyendo suscripción a fuzzy system)
- [ ] Quiet hours respetados

**Fase 5-6 — Frontend:**
- [ ] Web: página `/clima` con pronóstico 8 días + alertas configurables
- [ ] Web: página `/notificaciones` con preferencias + historial
- [ ] Web: Service Worker registrado para Web Push
- [ ] Web: Dashboard widget alertas meteo con badge de no leídas
- [ ] Mobile: nuevos atoms (`TimePicker`, `ProgressBar`, `WeatherIcon`)
- [ ] Mobile: nuevas molecules (`ForecastDayCard`, `AlertThresholdRow`, `ChannelToggleRow`, etc.)
- [ ] Mobile: nuevos organisms (`ForecastCarousel`, `AlertConfigSheet`, etc.)
- [ ] Mobile: feature components en `components/weather/` y `components/notifications/`
- [ ] Mobile: `WeatherScreen` + `NotificationSettingsScreen` + navegación
- [ ] Mobile: `expo-notifications` integrado con registro de token post-login
- [ ] Mobile: barrel exports actualizados en todos los `index.ts`

**General:**
- [ ] Todos los endpoints tienen auth JWT con scopes correctos
- [ ] Docker-compose actualizado con weather-service (One Call 3.0)
- [ ] Nginx config actualizada (si weather-service necesita upstream)
- [ ] Tests unitarios y de integración pasan
- [ ] `tsc --noEmit` exitoso en shared + web
- [ ] Debug logs limpiados del shared package
