# Diagrama de Clases — Weather Service (Capa de Dominio)

Capa de dominio del microservicio `weather-service` ubicado en
[software-project/weather-service/WeatherService.Domain/](../software-project/weather-service/WeatherService.Domain/).

Responsabilidad: ingestar pronósticos (OpenWeather One Call 3.0) y alertas
gubernamentales (IDEAM), detectar superaciones de umbrales por sistema fuzzy
y delegar el envío en `notification-service`.

---

## 1. Entidades, Value Objects y Enumeraciones

```mermaid
classDiagram
    direction LR

    class IIdentifiableMutable {
        <<interface>>
        +string Id
        +SetId(string id) void
    }

    class WeatherAlertConfig {
        +string Id
        +string FuzzySystemId
        +string FuzzySystemName
        +List~AlertThreshold~ Alerts
        +bool IsActive
        +int MaxForecastDays
        +bool AllowDuplicateAlerts
        +string CreatedBy
        +string UpdatedBy
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +SetId(string id) void
        +UpdateTimestamp(string userId) void
    }

    class AlertThreshold {
        <<value object>>
        +string Type
        +bool Enabled
        +double? ThresholdValue
        +string? Comparison
        +string Recommendation
    }

    class WeatherAlert {
        +string Id
        +string FuzzySystemId
        +string AlertType
        +string Severity
        +string Title
        +string Message
        +string Recommendation
        +DateTime ForecastDatetime
        +double? ForecastValue
        +string? ForecastCondition
        +bool GovernmentAlert
        +List~NotifiedUser~ NotifiedUsers
        +DateTime CreatedAt
        +SetId(string id) void
    }

    class NotifiedUser {
        <<value object>>
        +string UserId
        +List~string~ Channels
        +DateTime SentAt
        +bool IsRead
    }

    class ForecastCache {
        +string Id = "latest_forecast"
        +DateTime FetchedAt
        +DateTime ExpiresAt
        +CurrentWeatherDto Current
        +List~HourlyForecastDto~ Hourly
        +List~DailyForecastDto~ Daily
        +List~GovernmentAlertDto~ GovernmentAlerts
        +SetId(string id) void
    }

    class AlertDeliveryLog {
        +string Id
        +string FuzzySystemId
        +string AlertType
        +DateTime ForecastDate
        +string UserId
        +DateTime SentAt
    }

    class AlertTypes {
        <<static>>
        +ExtremeHeat = "extreme_heat"
        +ExtremeCold = "extreme_cold"
        +HighHumidity = "high_humidity"
        +LowHumidity = "low_humidity"
        +HeavyRain = "heavy_rain"
        +Thunderstorm = "thunderstorm"
        +HighCloudiness = "high_cloudiness"
        +StrongWind = "strong_wind"
        +ExtremeUv = "extreme_uv"
        +Government = "government"
        +string[] All
    }

    IIdentifiableMutable <|.. WeatherAlertConfig
    IIdentifiableMutable <|.. WeatherAlert
    IIdentifiableMutable <|.. ForecastCache

    WeatherAlertConfig "1" *-- "*" AlertThreshold : Alerts
    WeatherAlert "1" *-- "*" NotifiedUser : NotifiedUsers
    AlertThreshold ..> AlertTypes : Type ∈ constantes
    WeatherAlert ..> AlertTypes : AlertType ∈ constantes
    WeatherAlert ..> WeatherAlertConfig : FuzzySystemId
    AlertDeliveryLog ..> WeatherAlert : dedup por (system, type, day, user)
```

---

## 2. Interfaces de Repositorio y Puertos Externos

```mermaid
classDiagram
    direction LR

    class IOpenWeatherClient {
        <<interface>>
        +GetForecastAsync() Task~ForecastResponseDto~
        +GetCurrentWeatherAsync() Task~CurrentWeatherDto~
        +GetDailyForecastAsync() Task~List~DailyForecastDto~~
    }

    class IWeatherAlertConfigRepository {
        <<interface>>
        +GetByFuzzySystemIdAsync(id) Task~WeatherAlertConfig?~
        +GetAllActiveAsync() Task~List~WeatherAlertConfig~~
        +CreateAsync(config) Task~WeatherAlertConfig~
        +UpdateAsync(id, config) Task~WeatherAlertConfig?~
        +DeleteAsync(id) Task~bool~
        +ExistsAsync(id) Task~bool~
    }

    class IWeatherAlertRepository {
        <<interface>>
        +CreateAsync(alert) Task~WeatherAlert~
        +GetByFiltersAsync(systemId?, userId?, from?, to?, unreadOnly?) Task~List~WeatherAlert~~
        +MarkAsReadAsync(alertId, userId) Task~bool~
        +ExistsRecentAsync(type, systemId, dedupHours) Task~bool~
        +AddNotifiedUserAsync(alertId, user) Task
    }

    class IForecastCacheRepository {
        <<interface>>
        +GetLatestAsync() Task~ForecastCache?~
        +UpsertAsync(cache) Task
    }

    class IAlertDeliveryLogRepository {
        <<interface>>
        +InsertAsync(log) Task
        +ExistsAsync(systemId, type, forecastDate, userId) Task~bool~
    }

    class INotificationServiceClient {
        <<interface>>
        +GetSubscribersForFuzzySystemAsync(systemId) Task~List~AlertSubscriberDto~~
        +SendAlertNotificationAsync(userId, templateKey, title, body, data?) Task
    }

    class AlertSubscriberDto {
        +string UserId
        +List~string~ Channels
    }

    INotificationServiceClient ..> AlertSubscriberDto : devuelve
    IWeatherAlertConfigRepository ..> WeatherAlertConfig : maneja
    IWeatherAlertRepository ..> WeatherAlert : maneja
    IForecastCacheRepository ..> ForecastCache : maneja
    IAlertDeliveryLogRepository ..> AlertDeliveryLog : maneja
```

---

## Notas de diseño

- **Configuración por sistema fuzzy**: cada `WeatherAlertConfig` está vinculado 1:1 con un `FuzzySystemId` (índice único). Permite tener distintos umbrales por invernadero/cultivo.
- **Deduplicación de envíos**: `AllowDuplicateAlerts = false` activa la verificación contra `AlertDeliveryLog` (Time Series Collection con `timeField = SentAt`, `metaField = FuzzySystemId`) — evita re-notificar el mismo usuario por el mismo (tipo, día de pronóstico).
- **Cache persistido**: `ForecastCache` usa el patrón singleton (ID fijo `"latest_forecast"`) para sobrevivir reinicios; complementa al `IMemoryCache` del cliente HTTP.
- **Snapshot de canales notificados**: `NotifiedUser` se persiste embebido en `WeatherAlert` con la lista de canales por los que se envió y un flag `IsRead` por usuario.
- **Acoplamiento con notification-service**: `INotificationServiceClient` es un puerto del dominio (no llama directo a Mongo) — respeta la frontera de microservicios.
- **Tipos de alerta como constantes**: `AlertTypes` es una clase estática con strings — facilita extensión sin recompilar enums y mantiene compatibilidad con MongoDB.
