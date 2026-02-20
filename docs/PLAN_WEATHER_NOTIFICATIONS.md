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
| **Pronóstico 7 días** (OpenWeather 5-day/3-hour forecast) | Alta | Baja |
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
2. Pronóstico de 7 días (5-day/3-hour forecast API, free tier) disponible vía `GET /weather/forecast`.
3. El usuario puede configurar **umbrales de alerta meteorológica** desde la web y mobile.
4. Un **worker periódico** evalúa el pronóstico contra umbrales y genera alertas proactivas.
5. Las alertas incluyen **recomendaciones** para ajustar el sistema fuzzy (textos placeholder editables).
6. BFF expone endpoints para pronóstico, configuración de umbrales e historial de alertas meteo.
7. Vista web + mobile: pronóstico 7 días, configuración de umbrales, historial de alertas.

### 1.2 Funcionalidad 2 — Sistema de Notificaciones Avanzado

**DONE cuando:**
1. `notification-service` soporta **4 canales**: Email, Push (Expo + Web Push), WhatsApp (Twilio).
2. Cada usuario tiene **preferencias** de notificación (canales habilitados, horario silencio, hora resumen diario).
3. Push funciona end-to-end: mobile registra token → notificación llega al dispositivo.
4. Web Push funciona: navegador pide permiso → notificación nativa del OS (igual que WhatsApp Web).
5. El usuario puede programar un **resumen diario** a una hora específica con: reglas activadas, lecturas promedio, evaluaciones fuzzy realizadas, comportamiento de actuadores.
6. WhatsApp envía alertas y resumen diario (Twilio Sandbox para dev, Cloud API para prod).
7. Vista web + mobile: preferencias de notificación, historial de notificaciones enviadas.

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
OpenWeather API (free tier)
```

**Stack:**
- .NET 9, Clean Architecture (Api/Application/Domain/Infrastructure)
- `HydroEspinaca.Shared` v1.0.3-dev (auth JWT, Mongo base, ProblemDetails)
- MongoDB para persistencia (alertas, umbrales, historial pronósticos)
- `IMemoryCache` para cache de pronósticos
- `BackgroundService` para evaluación periódica de umbrales

**Puerto Docker:** 5030 (interno), no se expone públicamente (solo BFF lo consume).

### 2.2 Evolución de `notification-service` a multi-canal

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

### 2.3 OpenWeather API — Endpoint de Pronóstico

**API a usar:** `5 day / 3 hour forecast` (free tier, misma API key que ya se tiene).

- **URL:** `https://api.openweathermap.org/data/2.5/forecast?lat={lat}&lon={lon}&units=metric&lang=es&appid={key}`
- **Respuesta:** 40 data points (cada 3 horas × 5 días = 120 horas ≈ 5 días).
- **Limitación:** 5 días, no 7. Pero es lo máximo del free tier y es más que suficiente para alertas proactivas.
- **Rate limit:** 60 calls/min (free tier). Con cache de 3h en weather-service, tendremos ~8 llamadas/día.

**Datos relevantes para umbrales por cada data point:**

| Campo | Path en JSON | Unidad | Relevancia para alertas |
|-------|-------------|--------|------------------------|
| Temperatura | `main.temp` | °C | Helada (<5°C), Calor extremo (>35°C) |
| Sensación térmica | `main.feels_like` | °C | Índice de estrés térmico |
| Humedad | `main.humidity` | % | Humedad extrema (>90% o <30%) |
| Presión | `main.pressure` | hPa | Cambios bruscos → tormentas |
| Nubosidad | `clouds.all` | % | Alta nubosidad (>80%) → afecta luz |
| Viento | `wind.speed` | m/s | Viento fuerte (>10 m/s) |
| Prob. precipitación | `pop` | 0-1 | Alta prob. lluvia (>0.7) |
| Lluvia 3h | `rain.3h` | mm | Lluvia intensa (>10mm/3h) |
| Condición | `weather[0].id` | código | **200-232**: tormenta eléctrica, **500-531**: lluvia, **600-622**: nieve, **800**: despejado |
| Descripción | `weather[0].description` | texto | Texto en español incluido |

**Detección de tormenta eléctrica:** Sí es posible. Los códigos `200-232` de OpenWeather indican `Thunderstorm`. El campo `weather[0].id` lo identifica:
- 200-202: tormenta con lluvia
- 210-212: tormenta sin lluvia
- 221: tormenta irregular
- 230-232: tormenta con llovizna

---

## 3) Modelo de Datos

### 3.1 weather-service — MongoDB

#### Colección: `weather_alert_configs`

**Propósito:** Configuración de umbrales por usuario. Qué alertas quiere recibir.

| Campo | Tipo | Notas |
|-------|------|-------|
| `_id` | ObjectId | |
| `user_id` | string | ID del usuario |
| `alerts` | AlertThreshold[] | Array de umbrales configurados |
| `is_active` | bool | Permite desactivar todas las alertas de golpe |
| `created_at` | Date | |
| `updated_at` | Date | |

**AlertThreshold:**

| Campo | Tipo | Notas |
|-------|------|-------|
| `type` | string (enum) | `extreme_heat`, `extreme_cold`, `high_humidity`, `low_humidity`, `heavy_rain`, `thunderstorm`, `high_cloudiness`, `strong_wind` |
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

#### Colección: `weather_alerts`

**Propósito:** Historial de alertas generadas (auditoría + vista de historial).

| Campo | Tipo | Notas |
|-------|------|-------|
| `_id` | ObjectId | |
| `user_id` | string | Destinatario |
| `alert_type` | string | Tipo del umbral que se activó |
| `severity` | string | `warning` / `critical` |
| `title` | string | Título legible: "⚠️ Calor extremo pronosticado" |
| `message` | string | Descripción: "Se pronostica 37°C para el jueves 22 Feb a las 14:00" |
| `recommendation` | string | Texto de recomendación personalizado del usuario |
| `forecast_datetime` | Date | Fecha/hora del pronóstico que disparó la alerta |
| `forecast_value` | double? | Valor del pronóstico (ej: 37.2) |
| `forecast_condition` | string? | Condición textual (ej: "Tormenta con lluvia") |
| `notification_channels` | string[] | Por qué canales se envió: `["email", "push"]` |
| `notification_sent_at` | Date? | Cuándo se envió la notificación |
| `is_read` | bool | Si el usuario la marcó como leída |
| `created_at` | Date | |

#### Colección: `weather_forecast_cache`

**Propósito:** Persistir el último pronóstico obtenido para no depender solo de `IMemoryCache` (resistente a reinicios).

| Campo | Tipo | Notas |
|-------|------|-------|
| `_id` | string | Fijo: `"latest_forecast"` (singleton) |
| `fetched_at` | Date | Cuándo se obtuvo |
| `expires_at` | Date | Cuándo expira (fetched_at + 3h) |
| `forecast_data` | ForecastDataPoint[] | Array de 40 data points |

**ForecastDataPoint:**

| Campo | Tipo |
|-------|------|
| `datetime` | Date (UTC) |
| `temperature` | double |
| `feels_like` | double |
| `humidity` | int |
| `pressure` | int |
| `cloudiness` | int |
| `wind_speed` | double |
| `rain_3h` | double? |
| `pop` | double (0-1) |
| `weather_id` | int (código OW) |
| `weather_main` | string |
| `weather_description` | string |
| `weather_icon` | string |

### 3.2 notification-service — MongoDB (nuevas colecciones)

#### Colección: `notification_preferences`

**Propósito:** Preferencias de notificación por usuario.

| Campo | Tipo | Notas |
|-------|------|-------|
| `_id` | ObjectId | |
| `user_id` | string (unique index) | |
| `channels` | ChannelPreference[] | Configuración por canal |
| `daily_summary` | DailySummaryConfig | Configuración del resumen diario |
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
| `GET` | `/api/weather/forecast` | M2M | Pronóstico 5 días / 3h (40 data points) |
| `GET` | `/api/weather/forecast/daily` | M2M | Pronóstico resumido por día (5 días, promedios/min/max) |
| `GET` | `/api/weather/alerts/config/{userId}` | M2M | Umbrales configurados por usuario |
| `PUT` | `/api/weather/alerts/config/{userId}` | M2M | Actualizar umbrales |
| `GET` | `/api/weather/alerts/{userId}?from=&to=&unreadOnly=` | M2M | Historial de alertas |
| `PATCH` | `/api/weather/alerts/{alertId}/read` | M2M | Marcar alerta como leída |
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

*(Los endpoints existentes de email y grupos se mantienen intactos.)*

### 4.3 BFF (endpoints nuevos / modificados)

| Método | Ruta BFF | Upstream | Descripción |
|--------|----------|----------|-------------|
| `GET` | `/weather` | `weather-service /api/weather/current` | **Migrar**: ahora llama al weather-service en vez de OpenWeather directo |
| `GET` | `/weather/forecast` | `weather-service /api/weather/forecast` | Pronóstico 5 días |
| `GET` | `/weather/forecast/daily` | `weather-service /api/weather/forecast/daily` | Pronóstico resumido por día |
| `GET` | `/weather/alerts/config` | `weather-service /api/weather/alerts/config/{userId}` | Umbrales del usuario autenticado |
| `PUT` | `/weather/alerts/config` | `weather-service /api/weather/alerts/config/{userId}` | Actualizar umbrales |
| `GET` | `/weather/alerts` | `weather-service /api/weather/alerts/{userId}` | Historial de alertas meteo |
| `PATCH` | `/weather/alerts/{alertId}/read` | `weather-service /api/weather/alerts/{alertId}/read` | Marcar leída |
| `POST` | `/notifications/push/register` | `notification-service /api/push/register` | Registrar token push |
| `DELETE` | `/notifications/push/register/{id}` | `notification-service /api/push/register/{id}` | Desregistrar |
| `GET` | `/notifications/preferences` | `notification-service /api/preferences/{userId}` | Preferencias del usuario |
| `PUT` | `/notifications/preferences` | `notification-service /api/preferences/{userId}` | Actualizar preferencias |
| `GET` | `/notifications/history` | `notification-service /api/notifications/log/{userId}` | Historial de notificaciones |

---

## 5) Workers (Background Services)

### 5.1 `WeatherAlertEvaluationWorker` (weather-service)

**Responsabilidad:** Cada 3 horas, obtener el pronóstico de OpenWeather, evaluar contra umbrales de todos los usuarios, y disparar notificaciones.

**Flujo:**

```
[Cada 3 horas]
  │
  ├─ 1. Llamar OpenWeather 5-day forecast API
  ├─ 2. Cachear respuesta en IMemoryCache + MongoDB (weather_forecast_cache)
  ├─ 3. Obtener todos los weather_alert_configs activos
  ├─ 4. Por cada usuario con alertas activas:
  │     ├─ Por cada data point del pronóstico:
  │     │     ├─ Evaluar contra cada umbral habilitado del usuario
  │     │     ├─ Si hay match → crear weather_alert
  │     │     └─ Deduplicar: no alertar sobre el mismo tipo+ventana temporal más de 1 vez cada 6h
  │     └─ Si hay alertas nuevas:
  │           └─ POST a notification-service con los canales preferidos del usuario
  └─ 5. Log de ejecución
```

**Deduplicación:** Antes de crear una alerta, verificar que no exista una del mismo `alert_type` para el mismo usuario en las últimas 6 horas. Esto evita spam si el pronóstico no cambia.

**Notificación:** El weather-service llama a `notification-service POST /api/notifications/multi` pasando:
- `userId` → notification-service consulta preferencias y envía a los canales correctos
- `templateKey: "weather_alert"`
- `title`, `body`, `data` (con info del pronóstico)

### 5.2 `DailySummaryWorker` (notification-service)

**Responsabilidad:** Cada minuto, verificar si algún usuario tiene un resumen diario programado para esta hora:minuto, y generarlo.

**Flujo:**

```
[Cada 60 segundos]
  │
  ├─ 1. Obtener hora actual (UTC-5, zona de Colombia)
  ├─ 2. Consultar notification_preferences donde:
  │     daily_summary.enabled=true AND
  │     daily_summary.hour=horaActual AND
  │     daily_summary.minute=minutoActual
  ├─ 3. Por cada usuario que coincida:
  │     ├─ Obtener token M2M del auth-service
  │     ├─ Si include_sensor_averages: POST sensor-service /api/aggregates/environmental (view=daily, rango=ayer)
  │     ├─ Si include_actuator_runtime: POST actuator-service /api/commands/analytics (rango=ayer)
  │     ├─ Si include_fuzzy_rules: GET fuzzy-service /api/fuzzy-evaluations?from=ayer&to=hoy (o endpoint interno)
  │     ├─ Si include_weather_forecast: GET weather-service /api/weather/forecast/daily
  │     ├─ Renderizar template "daily_summary" con los datos recopilados
  │     └─ Enviar por los canales configurados en daily_summary.channels
  └─ 4. Log de ejecución
```

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
| `ForecastSection` | Cards por día (5 días) con icono de clima, temp min/max, probabilidad lluvia, condición. Expandible para ver detalle por franja horaria (3h). |
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

### 7.2 Mobile

#### Pantalla: `WeatherScreen` (tab "Clima" o sección en Dashboard)

| Componente | Descripción |
|-----------|-------------|
| `ForecastCard` | ScrollView horizontal de cards por día con icono, temp, condición. |
| `AlertConfigList` | FlatList de umbrales con switches. BottomSheet para editar umbral. |
| `WeatherAlertList` | FlatList de alertas recientes. Pull-to-refresh. |

#### Pantalla: `NotificationSettingsScreen` (en tab "Más")

| Componente | Descripción |
|-----------|-------------|
| `ChannelToggles` | Switches para cada canal. |
| `DailySummaryForm` | BottomSheet con configuración del resumen. |
| `QuietHoursForm` | Selectores de hora. |

#### Integración Push:

- `MobileAuthInitializer` (ya existe) → agregar registro de push token al montar.
- Listener de notificaciones recibidas → mostrar in-app banner.
- Listener de notificación presionada → deep link a la pantalla relevante.

### 7.3 Shared TS Package (nuevos tipos y servicios)

#### Tipos nuevos:

```typescript
// types/weather.ts (extensiones)
export interface ForecastDataPoint { ... }     // Un punto del pronóstico (3h)
export interface DailyForecast { ... }          // Resumen de un día
export interface WeatherAlertConfig { ... }     // Configuración de umbrales
export interface AlertThreshold { ... }         // Un umbral individual
export interface WeatherAlert { ... }           // Alerta generada

// types/notification.ts (nuevo archivo)
export interface NotificationPreferences { ... }
export interface ChannelPreference { ... }
export interface DailySummaryConfig { ... }
export interface QuietHoursConfig { ... }
export interface PushSubscription { ... }
export interface NotificationLogEntry { ... }
export type NotificationChannel = 'email' | 'push' | 'whatsapp' | 'web_push';
export type AlertType = 'extreme_heat' | 'extreme_cold' | 'high_humidity' | 'low_humidity'
                      | 'heavy_rain' | 'thunderstorm' | 'high_cloudiness' | 'strong_wind';
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

### Fase 1 — weather-service scaffold + pronóstico (Día 1-2)

| # | Tarea | Estimación | Detalle |
|---|-------|-----------|---------|
| 1.1 | Scaffold `weather-service` (.NET 9 Clean Architecture) | 2h | Api, Application, Domain, Infrastructure. NuGet refs a Shared. Dockerfile. |
| 1.2 | Migrar lógica de OpenWeather desde BFF | 1h | Mover `WeatherService.cs`, `WeatherDto`, crear `OpenWeatherClient` en Infrastructure |
| 1.3 | Implementar `GET /api/weather/current` | 1h | Cache por hora (migración directa) |
| 1.4 | Implementar `GET /api/weather/forecast` | 2h | Llamar a `/data/2.5/forecast`, parsear 40 data points, cache 3h |
| 1.5 | Implementar `GET /api/weather/forecast/daily` | 1.5h | Agregar data points por día: min/max temp, max pop, condición dominante |
| 1.6 | Docker-compose: agregar weather-service | 0.5h | Puerto 5030, env vars, depends_on mosquitto/mongo |
| 1.7 | BFF: crear `IWeatherServiceClient` + cambiar `WeatherController` | 2h | El BFF ahora llama a weather-service en vez de a OpenWeather directo |
| 1.8 | Shared NuGet: agregar scopes `weather:read`, `weather:write` | 0.5h | |
| 1.9 | Smoke test: pronóstico llega al BFF → Postman | 0.5h | |

**Entregable Fase 1:** `GET /weather/forecast` funcional desde BFF. Frontend sigue mostrando clima actual (no romper nada).

### Fase 2 — Alertas meteorológicas + worker (Día 2-3)

| # | Tarea | Estimación | Detalle |
|---|-------|-----------|---------|
| 2.1 | Domain: entidades `WeatherAlertConfig`, `WeatherAlert`, `ForecastCache` | 1.5h | |
| 2.2 | Infrastructure: repositorios MongoDB | 1.5h | Tres colecciones con índices |
| 2.3 | Application: CRUD de alert configs (`CreateDefaultConfigCommand`, `UpdateConfigCommand`, `GetConfigQuery`) | 2h | CQRS con MediatR |
| 2.4 | Application: `GetAlertsQuery` con filtros | 1h | |
| 2.5 | Application: `MarkAlertReadCommand` | 0.5h | |
| 2.6 | Api: `WeatherAlertController` con todos los endpoints | 1.5h | |
| 2.7 | Infrastructure: `WeatherAlertEvaluationWorker` | 3h | El worker principal: fetch → evaluar → deduplicar → notificar |
| 2.8 | BFF: exponer endpoints de alertas meteo | 1.5h | |
| 2.9 | Seed: umbrales predefinidos con recomendaciones | 1h | |

**Entregable Fase 2:** Worker evalúa pronóstico y crea alertas. Endpoints de configuración funcionan.

### Fase 3 — notification-service multi-canal (Día 3-5)

| # | Tarea | Estimación | Detalle |
|---|-------|-----------|---------|
| 3.1 | Domain: `INotificationChannel`, `NotificationMessage`, `NotificationSendResult` | 1h | Abstracción multi-canal |
| 3.2 | Domain: entidades `NotificationPreference`, `PushSubscription`, `NotificationLog` | 1.5h | |
| 3.3 | Infrastructure: `ExpoPushSender` (Expo Push API) | 2h | POST a `https://exp.host/--/api/v2/push/send` |
| 3.4 | Infrastructure: `WebPushSender` (VAPID) | 2h | NuGet `WebPush` |
| 3.5 | Infrastructure: `TwilioWhatsAppSender` | 2h | NuGet `Twilio`, feature flag |
| 3.6 | Infrastructure: `CompositeNotificationDispatcher` | 1.5h | Orquesta envío a múltiples canales según preferencias |
| 3.7 | Infrastructure: repos MongoDB para nuevas colecciones | 1.5h | |
| 3.8 | Application: `SendMultiChannelNotificationUseCase` | 2h | Consulta preferencias → envía a canales habilitados |
| 3.9 | Application: CRUD de preferencias (Commands/Queries) | 1.5h | |
| 3.10 | Application: CRUD de push subscriptions | 1h | |
| 3.11 | Application: `GetNotificationHistoryQuery` | 1h | |
| 3.12 | Api: controladores nuevos (`PushController`, `PreferencesController`, `MultiNotificationController`) | 2h | |
| 3.13 | Config: VAPID keys generation + env vars | 0.5h | `npx web-push generate-vapid-keys` |
| 3.14 | Config: Twilio env vars (Account SID, Auth Token, WhatsApp number) | 0.5h | Feature-flagged |
| 3.15 | BFF: exponer endpoints de notificaciones | 2h | |

**Entregable Fase 3:** Push y WhatsApp funcionales. Alertas meteo llegan por el canal preferido del usuario.

### Fase 4 — Resumen diario (Día 5-6)

| # | Tarea | Estimación | Detalle |
|---|-------|-----------|---------|
| 4.1 | Infrastructure: `DailySummaryWorker` (HostedService) | 3h | Verifica cada minuto, compila datos, envía |
| 4.2 | Infrastructure: `DailySummaryDataAggregator` | 3h | Llama a sensor-service, actuator-service, fuzzy-service, weather-service |
| 4.3 | Infrastructure: templates `daily_summary.liquid` (email) | 1.5h | Template Fluid con el formato de la sección 5.2 |
| 4.4 | Infrastructure: formatter texto plano (push/WhatsApp) | 1h | Versión corta para canales de texto |
| 4.5 | BFF: endpoint `GET /notifications/preferences` + `PUT` | 1h | (Si no se hizo en Fase 3) |
| 4.6 | Integration test: simular la hora, verificar que llega | 1.5h | |

**Entregable Fase 4:** Resumen diario funcional a la hora configurada por el usuario.

### Fase 5 — Frontend Web (Día 6-8)

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
| 6.1 | Instalar `expo-notifications` + `expo-device` | 0.5h | |
| 6.2 | `MobileAuthInitializer`: agregar registro de push token | 1.5h | Pedir permiso + registrar token al backend después de login |
| 6.3 | Push listeners: in-app banner + deep link on tap | 1.5h | |
| 6.4 | `WeatherScreen` con pronóstico y alertas | 3h | Cards horizontales + alert config list |
| 6.5 | `NotificationSettingsScreen` en tab "Más" | 2h | Channel toggles + daily summary config |
| 6.6 | Navegación: agregar WeatherScreen (tab o sección) | 0.5h | |
| 6.7 | Test en dispositivo físico + emulador | 1h | |

**Entregable Fase 6:** Push funcional en mobile, vistas de clima y configuración de notificaciones.

### Fase 7 — Testing + Polish (Día 9-10)

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
    - ExternalApis__OpenWeather__BaseUrl=https://api.openweathermap.org/data/2.5
    - ExternalApis__OpenWeather__ApiKey=${OPENWEATHER_API_KEY}
    - ExternalApis__OpenWeather__Latitude=4.7002001
    - ExternalApis__OpenWeather__Longitude=-74.2385058
    - Weather__ForecastCacheMinutes=180
    - Weather__AlertEvaluationIntervalMinutes=180
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
│  weather-service (WeatherAlertEvaluationWorker, cada 3h)          │
│                                                                    │
│  1. GET api.openweathermap.org/data/2.5/forecast                  │
│  2. Parsear 40 data points → cache en Mongo + Memory              │
│  3. Load all active WeatherAlertConfigs                            │
│  4. Por cada usuario, por cada data point:                         │
│     ¿temp > 35? ¿temp < 5? ¿pop > 0.7? ¿weather_id 200-232?     │
│  5. Deduplicar (no repetir mismo tipo en 6h)                      │
│  6. Persistir WeatherAlert en MongoDB                              │
│  7. POST notification-service /api/notifications/multi             │
└────────────────────────┬───────────────────────────────────────────┘
                         │
                         ▼
┌────────────────────────────────────────────────────────────────────┐
│  notification-service (SendMultiChannelNotificationUseCase)        │
│                                                                    │
│  1. Load NotificationPreferences del userId                        │
│  2. Filtrar canales habilitados                                    │
│  3. Verificar quiet hours                                          │
│  4. Enviar en paralelo:                                            │
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
| 1 | OpenWeather free tier: 5 días no 7 | Certeza | Bajo | Usar 5 días (suficiente para alertas). Documentar que con One Call 3.0 ($) se podría extender a 16 días |
| 2 | OpenWeather rate limit (60/min) | Bajo | Bajo | Cache de 3h + singleton worker = ~8 calls/día |
| 3 | Twilio WhatsApp Sandbox expire (72h) | Alto en dev | Medio | Feature flag disabled por defecto. Documentar instrucciones de renovación. Push es el MVP |
| 4 | Web Push bloqueado por navegador | Medio | Medio | Fallback a email siempre activo. UI muestra instrucciones si permiso denegado |
| 5 | DailySummaryWorker falla al llamar a múltiples servicios | Medio | Medio | Try/catch por sección: si falla sensores, enviar sin esa sección con nota "datos no disponibles" |
| 6 | Token Expo Push caduca o se invalida | Medio | Bajo | `PushCleanupWorker` desactiva tras 3 fallos + re-registro al abrir app |
| 7 | Spam de alertas si pronóstico no cambia | Medio | Alto | Deduplicación por tipo+ventana de 6h. Config max alertas/día por usuario |
| 8 | Shared NuGet requiere nuevo build para scopes | Bajo | Bajo | Agregar scopes, rebuild NuGet, actualizar refs en todos los servicios |

---

## 12) Dependencias entre Fases

```
Fase 1 (weather scaffold + forecast)
  │
  ├──→ Fase 2 (alertas meteo + worker) ──→ Fase 5 (frontend web clima)
  │                                          │
  │                                          ├──→ Fase 6 (frontend mobile)
  │                                          │
  └──→ Fase 3 (notification multi-canal) ───┘
         │
         └──→ Fase 4 (resumen diario) ──→ Fase 5 (frontend web notif.)
                                            │
                                            └──→ Fase 6 (frontend mobile notif.)

Fase 7 (testing) requiere todo lo anterior.
```

**Trabajo paralelizable:**
- Fase 1 + Fase 3 pueden arrancar en paralelo (weather-service y notification-service son independientes).
- Frontend web (Fase 5) puede avanzar shared TS types mientras los backends se completan.

---

## 13) Checklist de Verificación Final

- [ ] `GET /weather` sigue funcionando (ahora via weather-service, no directo OpenWeather)
- [ ] `GET /weather/forecast` retorna pronóstico de 5 días con cache
- [ ] `GET /weather/forecast/daily` retorna resumen diario
- [ ] Usuario puede configurar umbrales desde web y mobile
- [ ] Worker evalúa pronóstico cada 3h y genera alertas
- [ ] Alertas llegan por push (Expo) al mobile
- [ ] Alertas llegan por Web Push al browser
- [ ] Alertas llegan por email
- [ ] WhatsApp funciona si está habilitado (feature flag)
- [ ] Resumen diario se envía a la hora configurada
- [ ] Resumen incluye: sensores, actuadores, fuzzy, pronóstico
- [ ] Preferencias de notificación persistidas por usuario
- [ ] Historial de notificaciones visible en web
- [ ] Quiet hours respetados
- [ ] Deduplicación de alertas funciona (no spam)
- [ ] Service Worker registrado en Next.js para Web Push
- [ ] `expo-notifications` integrado en mobile
- [ ] Todos los endpoints tienen auth JWT con scopes correctos
- [ ] Docker-compose actualizado con weather-service
- [ ] Nginx config actualizada (si weather-service necesita upstream)
- [ ] Tests unitarios y de integración pasan
- [ ] `tsc --noEmit` exitoso en shared + web
- [ ] Debug logs limpiados del shared package
