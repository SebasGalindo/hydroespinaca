# Diagrama de Clases — Notification Service (Capa de Dominio)

Capa de dominio del microservicio `notification-service` ubicado en
[software-project/notification-service/NotificationService.Domain/](../software-project/notification-service/NotificationService.Domain/).

Responsabilidad: orquestar el envío de notificaciones multi-canal (email, push
Expo, WhatsApp, web push), gestionar preferencias por usuario, idempotencia,
plantillas, log unificado, y resúmenes diarios programados.

Por la cantidad de tipos, el dominio se descompone en tres vistas: pipeline de
email, pipeline multi-canal, y puertos/repositorios.

---

## 1. Pipeline de Email (legacy específico)

```mermaid
classDiagram
    direction LR

    class IIdentifiableMutable {
        <<interface>>
        +string Id
        +SetId(string id) void
    }

    class EmailMessage {
        +string CorrelationId
        +string IdempotencyKey
        +string[] To
        +string[] Cc
        +string[] Bcc
        +string Subject
        +string HtmlBody
        +string TemplateKey
        +List~EmailAttachment~ Attachments
    }

    class EmailAttachment {
        +string FileName
        +string ContentType
        +string ContentBase64
    }

    class EmailSendResult {
        <<record>>
        +bool Success
        +string? Provider
        +string? ProviderMessageId
        +string? Error
    }

    class EmailLog {
        +string Id
        +string CorrelationId
        +string To
        +string? Subject
        +EmailDeliveryStatus Status
        +string? Provider
        +string? ProviderMessageId
        +string? Error
        +DateTimeOffset CreatedAt
        +DateTimeOffset? UpdatedAt
        +SetId(string id) void
    }

    class EmailDeliveryStatus {
        <<enumeration>>
        Queued
        Sent
        Failed
    }

    class IdempotencyRecord {
        +string Key
        +string CorrelationId
        +DateTimeOffset CreatedAt
        +DateTimeOffset ExpiresAt
        +IdempotencyStatus Status
        +string? ResponseJson
        +string Id (= Key)
        +SetId(string id) void
    }

    class IdempotencyStatus {
        <<enumeration>>
        Reserved
        Queued
        Sent
        Failed
    }

    class NotificationGroup {
        +string Id
        +string GroupName
        +string? Description
        +List~GroupRecipient~ Recipients
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +HasActiveRecipients() bool
        +GetRecipientsByType() (to, cc, bcc)
        +UpdateTimestamp() void
        +SetId(string id) void
    }

    class GroupRecipient {
        +string Email
        +RecipientType Type
        +bool IsActive
    }

    class RecipientType {
        <<enumeration>>
        TO
        CC
        BCC
    }

    IIdentifiableMutable <|.. EmailLog
    IIdentifiableMutable <|.. IdempotencyRecord
    IIdentifiableMutable <|.. NotificationGroup

    EmailMessage "1" *-- "*" EmailAttachment : Attachments
    EmailLog --> EmailDeliveryStatus
    IdempotencyRecord --> IdempotencyStatus
    NotificationGroup "1" *-- "*" GroupRecipient : Recipients
    GroupRecipient --> RecipientType
```

---

## 2. Pipeline Multi-Canal y Resumen Diario

```mermaid
classDiagram
    direction LR

    class NotificationMessage {
        +string CorrelationId
        +string UserId
        +string Channel
        +string TemplateKey
        +string Title
        +string Body
        +Dictionary~string,string~ Data
        +string? RecipientEmail
        +string? RecipientPhone
        +string? ExpoPushToken
        +string? WebPushSubscription
    }

    class NotificationSendResult {
        <<record>>
        +bool Success
        +string Channel
        +string? Provider
        +string? ProviderMessageId
        +string? Error
    }

    class NotificationChannels {
        <<static>>
        +Email = "email"
        +Push = "push"
        +WebPush = "web_push"
        +WhatsApp = "whatsapp"
        +string[] All
    }

    class NotificationLog {
        +string Id
        +string CorrelationId
        +string UserId
        +string Channel
        +string TemplateKey
        +string Title
        +string Status
        +string? Provider
        +string? ProviderMessageId
        +string? Error
        +DateTime CreatedAt
        +DateTime? UpdatedAt
        +MarkSent(provider, messageId?) void
        +MarkFailed(provider, error) void
        +SetId(string id) void
    }

    class NotificationPreference {
        +string Id
        +string UserId
        +List~ChannelPreference~ Channels
        +DailySummaryConfig DailySummary
        +WeatherAlertSubscription WeatherAlertsSubscription
        +QuietHoursConfig? QuietHours
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +GetEnabledChannels() List~string~
        +IsChannelEnabled(channel) bool
        +UpdateTimestamp() void
        +SetId(string id) void
    }

    class ChannelPreference {
        +string Channel
        +bool Enabled
        +string? Target
    }

    class DailySummaryConfig {
        +bool Enabled
        +int Hour
        +int Minute
        +List~string~ Channels
        +bool IncludeFuzzyRules
        +bool IncludeSensorAverages
        +bool IncludeActuatorRuntime
        +bool IncludeWeatherForecast
    }

    class QuietHoursConfig {
        +bool Enabled
        +int StartHour
        +int EndHour
    }

    class WeatherAlertSubscription {
        +bool Enabled
        +string? FuzzySystemId
        +List~string~ AlertTypes
    }

    class PushSubscription {
        +string Id
        +string UserId
        +string Platform
        +string Token
        +string? DeviceName
        +bool IsActive
        +DateTime LastUsedAt
        +int FailureCount
        +DateTime CreatedAt
        +RecordSuccess() void
        +RecordFailure() void
        +SetId(string id) void
    }

    class DailySummaryData {
        +DateTime Date
        +string UserId
        +List~SensorVariableSummary~ SensorSummaries
        +List~ActuatorRuntimeSummary~ ActuatorSummaries
        +FuzzyEvaluationSummary? FuzzyEvaluation
        +WeatherForecastSummary? WeatherForecast
        +bool IncludeSensorAverages
        +bool IncludeActuatorRuntime
        +bool IncludeFuzzyRules
        +bool IncludeWeatherForecast
    }

    class SensorVariableSummary {
        +string VariableCode
        +string VariableName
        +double Min
        +double Max
        +double Avg
        +int Count
    }

    class ActuatorRuntimeSummary {
        +string ActuatorCode
        +double TotalDurationMinutes
        +double Percentage
        +int ActivationCount
    }

    class FuzzyEvaluationSummary {
        +int EvaluationCount
        +string? SystemName
        +List~TopRuleSummary~ TopRules
    }

    class TopRuleSummary {
        +string RuleId
        +int ActivationCount
        +double AvgFiringStrength
    }

    class WeatherForecastSummary {
        +double TempMin
        +double TempMax
        +double Pop
        +string Description
        +string? Summary
    }

    class IIdentifiableMutable {
        <<interface>>
    }

    IIdentifiableMutable <|.. NotificationLog
    IIdentifiableMutable <|.. NotificationPreference
    IIdentifiableMutable <|.. PushSubscription

    NotificationMessage ..> NotificationChannels : Channel ∈
    NotificationLog ..> NotificationChannels : Channel ∈
    NotificationPreference "1" *-- "*" ChannelPreference : Channels
    NotificationPreference "1" *-- "1" DailySummaryConfig : DailySummary
    NotificationPreference "1" *-- "1" WeatherAlertSubscription : WeatherAlertsSubscription
    NotificationPreference "1" *-- "0..1" QuietHoursConfig : QuietHours
    DailySummaryData "1" *-- "*" SensorVariableSummary
    DailySummaryData "1" *-- "*" ActuatorRuntimeSummary
    DailySummaryData "1" *-- "0..1" FuzzyEvaluationSummary
    DailySummaryData "1" *-- "0..1" WeatherForecastSummary
    FuzzyEvaluationSummary "1" *-- "*" TopRuleSummary : TopRules
```

---

## 3. Puertos, Repositorios y Servicios de Dominio

```mermaid
classDiagram
    direction LR

    class IEmailQueue {
        <<interface>>
        +EnqueueAsync(message) Task
        +DequeueAllAsync() IAsyncEnumerable~EmailMessage~
    }

    class IEmailSender {
        <<interface — provider port>>
        +SendAsync(message) Task~EmailSendResult~
    }

    class IEmailLogRepository {
        <<interface>>
        +InsertAsync(log) Task
        +UpdateStatusAsync(correlationId, status, provider?, providerMessageId?, error?) Task
    }

    class IIdempotencyStore {
        <<interface>>
        +TryReserveAsync(key, correlationId, ttl) Task~(bool, IdempotencyRecord)~
        +GetAsync(key) Task~IdempotencyRecord?~
        +UpdateAsync(key, update) Task
    }

    class ISanitizer {
        <<interface>>
        +Sanitize(html) string
    }

    class ITemplateRenderer {
        <<interface>>
        +RenderAsync(templateKey, sanitizedBodyHtml, model?) Task~string~
    }

    class INotificationGroupRepository {
        <<interface>>
        +GetAllAsync() Task~IEnumerable~NotificationGroup~~
        +GetByGroupNameAsync(name) Task~NotificationGroup?~
        +CreateAsync(group) Task~NotificationGroup~
        +UpdateAsync(name, group) Task~NotificationGroup?~
        +DeleteAsync(name) Task~bool~
        +ExistsAsync(name) Task~bool~
    }

    class INotificationChannel {
        <<interface — strategy>>
        +ChannelType: string
        +SendAsync(message) Task~NotificationSendResult~
        +IsAvailableAsync() Task~bool~
    }

    class INotificationDispatcher {
        <<interface — orchestrator>>
        +DispatchAsync(userId, templateKey, title, body, data?, allowedChannels?) Task~IEnumerable~NotificationSendResult~~
    }

    class INotificationPreferenceRepository {
        <<interface>>
        +GetByUserIdAsync(userId) Task~NotificationPreference?~
        +CreateAsync(pref) Task~NotificationPreference~
        +UpdateAsync(pref) Task
        +GetSubscribersForFuzzySystemAsync(systemId) Task~IEnumerable~NotificationPreference~~
        +GetDailySummarySubscribersAsync() Task~IEnumerable~NotificationPreference~~
    }

    class IPushSubscriptionRepository {
        <<interface>>
        +CreateAsync(sub) Task~PushSubscription~
        +DeleteAsync(id) Task~bool~
        +GetByUserIdAsync(userId) Task~IEnumerable~PushSubscription~~
        +GetActiveByUserIdAsync(userId, platform?) Task~IEnumerable~PushSubscription~~
        +UpdateAsync(sub) Task
        +DeactivateFailedSubscriptionsAsync(maxFailures = 3) Task~int~
        +CleanupInactiveAsync(olderThanDays = 90) Task~int~
    }

    class INotificationLogRepository {
        <<interface>>
        +CreateAsync(log) Task~NotificationLog~
        +UpdateAsync(log) Task
        +GetByUserIdAsync(userId, channel?, from?, to?, limit) Task~IEnumerable~NotificationLog~~
    }

    class IDailySummaryDataAggregator {
        <<interface>>
        +AggregateAsync(userId, preference) Task~DailySummaryData~
    }

    class IDailySummaryScheduler {
        <<interface — Quartz>>
        +RescheduleAsync(userId, hour, minute) Task
        +RemoveAsync(userId) Task
    }

    IEmailQueue ..> EmailMessage : encola
    IEmailSender ..> EmailMessage : envía
    IEmailSender ..> EmailSendResult : devuelve
    IEmailLogRepository ..> EmailLog : maneja
    IIdempotencyStore ..> IdempotencyRecord : maneja
    INotificationGroupRepository ..> NotificationGroup : maneja
    INotificationChannel ..> NotificationMessage : envía
    INotificationChannel ..> NotificationSendResult : devuelve
    INotificationDispatcher ..> INotificationChannel : compone canales
    INotificationDispatcher ..> INotificationPreferenceRepository : consulta prefs
    INotificationPreferenceRepository ..> NotificationPreference : maneja
    IPushSubscriptionRepository ..> PushSubscription : maneja
    INotificationLogRepository ..> NotificationLog : maneja
    IDailySummaryDataAggregator ..> DailySummaryData : produce
    IDailySummaryDataAggregator ..> NotificationPreference : usa flags
```

---

## Notas de diseño

- **Dos pipelines coexistentes**: el de **email puro** (`EmailMessage` → `IEmailQueue` → `IEmailSender` → `EmailLog`) sigue vivo para flujos que sólo necesitan correo; el **multi-canal** (`NotificationMessage` → `INotificationDispatcher` → `INotificationChannel` por estrategia → `NotificationLog`) lo complementa para alertas y resumen diario.
- **Patrón Strategy con `INotificationChannel`**: cada canal (email, push, web push, WhatsApp) implementa la misma interfaz, y `INotificationDispatcher` los compone según las preferencias del usuario y el filtro `allowedChannels`.
- **Idempotencia distribuida**: `IIdempotencyStore` usa Mongo con TTL — `TryReserveAsync` es atómico (`acquired` indica si fue el primer reservista) para evitar duplicados aún con reintentos del cliente o concurrencia entre nodos.
- **Política de salud de tokens push**: `PushSubscription.RecordFailure()` desactiva automáticamente el token al alcanzar 3 fallos consecutivos; `RecordSuccess()` resetea el contador. `DeactivateFailedSubscriptionsAsync` y `CleanupInactiveAsync` permiten mantenimiento batch.
- **Plantillas seguras**: `ISanitizer` (limpia HTML embebido) y `ITemplateRenderer` (renderiza el layout final) están desacoplados — la sanitización corre **antes** del renderizado para evitar XSS en cuerpos definidos por usuarios o servicios upstream.
- **Resumen diario por usuario**: `DailySummaryConfig` define hora local (UTC-5) y secciones a incluir; `IDailySummaryScheduler` programa un trigger Quartz por usuario y `IDailySummaryDataAggregator` consulta sólo las secciones habilitadas (sensores, actuadores, fuzzy, clima) — evita fan-out innecesario.
- **`QuietHoursConfig`**: ventana opcional para suprimir notificaciones no críticas (p.ej. 22:00 → 07:00). El dispatcher debe consultarla antes de enviar.
- **`NotificationLog` unificado vs `EmailLog` legacy**: el log nuevo cubre todos los canales con campos `Channel` y `Status` ("queued"/"sent"/"failed"/"delivered") y métodos `MarkSent`/`MarkFailed`. `EmailLog` se mantiene para auditoría histórica del pipeline antiguo.
- **Subscriber lookup por sistema fuzzy**: `GetSubscribersForFuzzySystemAsync` devuelve preferencias donde `WeatherAlertsSubscription.Enabled = true` y `FuzzySystemId` matchea o es `null` (auto-detect) — usado por `weather-service` vía cliente HTTP.
