using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeatherService.Domain.DTOs;
using WeatherService.Domain.Entities;
using WeatherService.Domain.Enums;
using WeatherService.Domain.Interfaces;
using WeatherService.Domain.Settings;

namespace WeatherService.Infrastructure.Workers;

/// <summary>
/// Background worker that runs every N minutes (default 30).
/// Fetches forecast, evaluates against alert thresholds per fuzzy system,
/// creates deduplicated alerts, and sends notifications to subscribed users.
/// </summary>
public class WeatherAlertEvaluationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WeatherAlertEvaluationWorker> _logger;
    private readonly WeatherSettings _weatherSettings;
    private readonly IAlertDeliveryLogRepository _deliveryLogRepo;

    public WeatherAlertEvaluationWorker(
        IServiceProvider serviceProvider,
        IOptions<WeatherSettings> weatherSettings,
        IAlertDeliveryLogRepository deliveryLogRepo,
        ILogger<WeatherAlertEvaluationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _weatherSettings = weatherSettings.Value;
        _deliveryLogRepo = deliveryLogRepo;
    }

    /// <summary>
    /// Main execution loop of the worker. It runs indefinitely until the application is stopped, 
    /// performing alert evaluation cycles at configured intervals.
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "WeatherAlertEvaluationWorker started. Interval: {Interval}min, Dedup window: {Dedup}h",
            _weatherSettings.AlertEvaluationIntervalMinutes,
            _weatherSettings.AlertDeduplicationHours);

        // Wait a bit on startup to let other services initialize
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EvaluateAlertsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in alert evaluation cycle");
            }

            await Task.Delay(
                TimeSpan.FromMinutes(_weatherSettings.AlertEvaluationIntervalMinutes),
                stoppingToken);
        }

        _logger.LogInformation("WeatherAlertEvaluationWorker stopped");
    }

    /// <summary>
    /// Main method that performs the alert evaluation cycle:
    /// 1. Fetches the latest forecast from the OpenWeather client.
    /// 2. Persists the forecast to the cache repository.
    /// 3. Retrieves all active alert configurations.
    /// 4. Evaluates each configuration against the forecast data, creating alerts as needed while preventing duplicates.
    /// 5. Notifies subscribers of any new alerts through the notification service client
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async Task EvaluateAlertsAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting alert evaluation cycle");

        using var scope = _serviceProvider.CreateScope();
        var openWeatherClient = scope.ServiceProvider.GetRequiredService<IOpenWeatherClient>();
        var alertConfigRepo = scope.ServiceProvider.GetRequiredService<IWeatherAlertConfigRepository>();
        var alertRepo = scope.ServiceProvider.GetRequiredService<IWeatherAlertRepository>();
        var forecastCacheRepo = scope.ServiceProvider.GetRequiredService<IForecastCacheRepository>();
        var notificationClient = scope.ServiceProvider.GetRequiredService<INotificationServiceClient>();

        // 1. Fetch forecast
        var forecast = await openWeatherClient.GetForecastAsync(ct);

        // 2. Persist to MongoDB cache (survives restarts)
        await PersistForecastCacheAsync(forecastCacheRepo, forecast, ct);

        // 3. Get all active alert configs
        var configs = await alertConfigRepo.GetAllActiveAsync(ct);
        if (configs.Count == 0)
        {
            _logger.LogDebug("No active alert configs found. Skipping evaluation.");
            return;
        }

        _logger.LogInformation("Evaluating {Count} active alert configs", configs.Count);

        var totalAlertsCreated = 0;

        // 4. Evaluate each config
        foreach (var config in configs)
        {
            try
            {
                var alertsCreated = await EvaluateConfigAsync(
                    config, forecast, alertRepo, notificationClient, ct);
                totalAlertsCreated += alertsCreated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating config for fuzzy system {FuzzySystemId}",
                    config.FuzzySystemId);
            }
        }

        _logger.LogInformation("Alert evaluation cycle complete. Alerts created: {Count}", totalAlertsCreated);
    }

    /// <summary>
    /// Persists the fetched forecast to the cache repository with an expiration time based on settings.
    /// This allows the system to retain the last known forecast across restarts and provides a source of truth for alert evaluations, 
    /// even if the OpenWeather API is temporarily unavailable. 
    /// The cache entry includes the fetched timestamp and an expiration timestamp 
    /// calculated from the configured cache duration. 
    /// This method is called during each alert evaluation cycle after 
    /// fetching the latest forecast data.
    /// </summary>
    /// <param name="repo">The forecast cache repository to persist the forecast to.</param>
    /// <param name="forecast">The forecast data to persist.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns></returns>
    private async Task PersistForecastCacheAsync(
        IForecastCacheRepository repo, ForecastResponseDto forecast, CancellationToken ct)
    {
        var cache = new ForecastCache
        {
            FetchedAt = forecast.FetchedAt,
            ExpiresAt = forecast.FetchedAt.AddMinutes(_weatherSettings.ForecastCacheMinutes),
            Current = forecast.Current,
            Hourly = forecast.Hourly,
            Daily = forecast.Daily,
            GovernmentAlerts = forecast.GovernmentAlerts
        };

        await repo.UpsertAsync(cache, ct);
    }

    /// <summary>
    /// Evaluates a single alert config against the provided forecast data.
    /// For each enabled threshold in the config, it checks if the forecast meets the alert conditions. 
    /// If an alert condition is met, it checks for recent similar alerts to avoid duplicates
    /// and creates a new alert in the repository if it's a new event.
    /// </summary>
    /// <param name="config">The alert configuration to evaluate.</param>
    /// <param name="forecast">The forecast data to evaluate against.</param>
    /// <param name="alertRepo">The repository to create new alerts in.</param>
    /// <param name="notificationClient">The notification service client to notify subscribers.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The number of alerts created.</returns>
    private async Task<int> EvaluateConfigAsync(
        WeatherAlertConfig config,
        ForecastResponseDto forecast,
        IWeatherAlertRepository alertRepo,
        INotificationServiceClient notificationClient,
        CancellationToken ct)
    {
        var newAlerts = new List<WeatherAlert>();
        var enabledThresholds = config.Alerts.Where(a => a.Enabled).ToList();

        if (enabledThresholds.Count == 0) return 0;

        // Evaluate daily forecast limited by MaxForecastDays
        var daysToEvaluate = forecast.Daily.Take(config.MaxForecastDays);
        foreach (var daily in daysToEvaluate)
        {
            foreach (var threshold in enabledThresholds)
            {
                var evaluation = EvaluateDailyAgainstThreshold(daily, threshold);
                if (evaluation == null) continue;

                // Check deduplication
                if (await alertRepo.ExistsRecentAsync(
                        threshold.Type, config.FuzzySystemId,
                        _weatherSettings.AlertDeduplicationHours, ct))
                    continue;

                var alert = BuildAlert(config, threshold, evaluation.Value, daily.DateTime);
                await alertRepo.CreateAsync(alert, ct);
                newAlerts.Add(alert);

                _logger.LogInformation(
                    "Alert created: {Type} for fuzzy system {FuzzySystemId} — {Title}",
                    threshold.Type, config.FuzzySystemId, alert.Title);
            }
        }

        // Evaluate hourly forecast (next 48h) — only for rain accumulation
        var heavyRainThreshold = enabledThresholds.FirstOrDefault(
            t => t.Type == AlertTypes.HeavyRain);
        if (heavyRainThreshold != null)
        {
            await EvaluateHourlyRainAsync(
                forecast.Hourly, heavyRainThreshold, config, alertRepo, newAlerts, ct);
        }

        // Forward government alerts
        var govThreshold = enabledThresholds.FirstOrDefault(t => t.Type == AlertTypes.Government);
        if (govThreshold != null && forecast.GovernmentAlerts.Count > 0)
        {
            foreach (var govAlert in forecast.GovernmentAlerts)
            {
                if (await alertRepo.ExistsRecentAsync(
                        AlertTypes.Government, config.FuzzySystemId,
                        _weatherSettings.AlertDeduplicationHours, ct))
                    continue;

                var alert = new WeatherAlert
                {
                    FuzzySystemId = config.FuzzySystemId,
                    AlertType = AlertTypes.Government,
                    Severity = "critical",
                    Title = $"🚨 Alerta oficial: {govAlert.Event}",
                    Message = $"{govAlert.SenderName} informa: {govAlert.Description}",
                    Recommendation = govThreshold.Recommendation ?? "Alerta emitida por autoridades. Tome las precauciones necesarias para proteger sus cultivos.",
                    ForecastDatetime = govAlert.Start,
                    ForecastCondition = govAlert.Event,
                    GovernmentAlert = true
                };

                await alertRepo.CreateAsync(alert, ct);
                newAlerts.Add(alert);
            }
        }

        // Notify subscribers with all grouped alerts
        if (newAlerts.Count > 0)
        {
            await NotifySubscribersGroupedAsync(newAlerts, config, alertRepo, notificationClient, _deliveryLogRepo, ct);
        }

        return newAlerts.Count;
    }

    private async Task EvaluateHourlyRainAsync(
        List<HourlyForecastDto> hourly,
        AlertThreshold threshold,
        WeatherAlertConfig config,
        IWeatherAlertRepository alertRepo,
        List<WeatherAlert> newAlerts,
        CancellationToken ct)
    {
        // Accumulate rain over 3-hour windows
        for (var i = 0; i + 2 < hourly.Count; i++)
        {
            var rain3h = (hourly[i].Rain1h ?? 0)
                       + (hourly[i + 1].Rain1h ?? 0)
                       + (hourly[i + 2].Rain1h ?? 0);

            if (threshold.ThresholdValue.HasValue && rain3h > threshold.ThresholdValue.Value)
            {
                if (await alertRepo.ExistsRecentAsync(
                        AlertTypes.HeavyRain, config.FuzzySystemId,
                        _weatherSettings.AlertDeduplicationHours, ct))
                    return;

                var alert = new WeatherAlert
                {
                    FuzzySystemId = config.FuzzySystemId,
                    AlertType = AlertTypes.HeavyRain,
                    Severity = rain3h > threshold.ThresholdValue.Value * 2 ? "critical" : "warning",
                    Title = "🌧️ Lluvia intensa pronosticada",
                    Message = $"Se pronostica una acumulación de lluvia de {rain3h:F1} mm en 3 horas a partir del {hourly[i].DateTime:dd/MM/yyyy} a las {hourly[i].DateTime:HH:mm} UTC.",
                    Recommendation = threshold.Recommendation,
                    ForecastDatetime = hourly[i].DateTime,
                    ForecastValue = rain3h
                };

                await alertRepo.CreateAsync(alert, ct);
                newAlerts.Add(alert);
                return; // One alert per cycle is enough
            }
        }
    }


    /// <summary>
    /// Evaluates a daily forecast against a specific alert threshold.
    /// </summary>
    /// <param name="daily">The daily forecast data to evaluate.</param>
    /// <param name="threshold">The alert threshold to compare against.</param>
    /// <returns>A tuple containing the value that triggered the alert and its severity, or null if no alert is triggered.</returns>
    private static (double value, string severity)? EvaluateDailyAgainstThreshold(
        DailyForecastDto daily, AlertThreshold threshold)
    {
        return threshold.Type switch
        {
            AlertTypes.ExtremeHeat => EvaluateNumeric(daily.TempMax, threshold),
            AlertTypes.ExtremeCold => EvaluateNumeric(daily.TempMin, threshold),
            AlertTypes.HighHumidity => EvaluateNumeric(daily.Humidity, threshold),
            AlertTypes.LowHumidity => EvaluateNumeric(daily.Humidity, threshold),
            AlertTypes.HighCloudiness => EvaluateNumeric(daily.Cloudiness, threshold),
            AlertTypes.StrongWind => EvaluateNumeric(daily.WindSpeed, threshold),
            AlertTypes.ExtremeUv => EvaluateNumeric(daily.Uvi, threshold),
            AlertTypes.Thunderstorm => EvaluateThunderstorm(daily),
            // HeavyRain is evaluated separately via hourly data
            _ => null
        };
    }

    /// <summary>
    /// Evaluates a numeric forecast value against an alert threshold, 
    /// determining if the condition is met and calculating the severity 
    /// based on the deviation from the threshold.
    /// </summary>
    /// <param name="actual">The actual forecast value to evaluate.</param>
    /// <param name="threshold">The alert threshold to compare against.</param>
    /// <returns>A tuple containing the value that triggered the alert and its severity, or null if no alert is triggered.</returns>
    private static (double value, string severity)? EvaluateNumeric(
        double actual, AlertThreshold threshold)
    {
        if (!threshold.ThresholdValue.HasValue || threshold.Comparison == null)
            return null;

        var exceeds = threshold.Comparison switch
        {
            "gt" => actual > threshold.ThresholdValue.Value,
            "lt" => actual < threshold.ThresholdValue.Value,
            _ => false
        };

        if (!exceeds) return null;

        // Determine severity: >150% deviation from threshold = critical
        var deviation = Math.Abs(actual - threshold.ThresholdValue.Value) / threshold.ThresholdValue.Value;
        var severity = deviation > 0.5 ? "critical" : "warning";

        return (actual, severity);
    }

    /// <summary>
    /// Evaluates a daily forecast for thunderstorm conditions based on weather codes,
    /// and assigns a severity level based on the specific thunderstorm type.
    /// </summary>
    /// <param name="daily">The daily forecast data to evaluate.</param>
    /// <returns></returns>
    private static (double value, string severity)? EvaluateThunderstorm(DailyForecastDto daily)
    {
        // OW weather IDs 2xx = Thunderstorm group
        if (daily.WeatherId is >= 200 and < 300)
        {
            var severity = daily.WeatherId switch
            {
                >= 202 and <= 212 => "critical", // heavy thunderstorm
                _ => "warning"
            };
            return (daily.WeatherId, severity);
        }
        return null;
    }

    /// <summary>
    /// Builds a WeatherAlert entity based on the alert configuration, 
    /// threshold that was triggered, the evaluation result, 
    /// and the forecast datetime.
    /// </summary>
    /// <param name="config">The alert configuration.</param>
    /// <param name="threshold">The threshold that was triggered.</param>
    /// <param name="evaluation">The evaluation result.</param>
    /// <param name="forecastDatetime">The forecast datetime.</param>
    /// <returns>The built WeatherAlert entity.</returns>
    private static WeatherAlert BuildAlert(
        WeatherAlertConfig config,
        AlertThreshold threshold,
        (double value, string severity) evaluation,
        DateTime forecastDatetime)
    {
        var title = threshold.Type switch
        {
            AlertTypes.ExtremeHeat => $"🔥 Calor extremo pronosticado: {evaluation.value:F1}°C",
            AlertTypes.ExtremeCold => $"🥶 Frío extremo pronosticado: {evaluation.value:F1}°C",
            AlertTypes.HighHumidity => $"💧 Humedad alta pronosticada: {evaluation.value:F0}%",
            AlertTypes.LowHumidity => $"🏜️ Humedad baja pronosticada: {evaluation.value:F0}%",
            AlertTypes.HighCloudiness => $"☁️ Nubosidad alta pronosticada: {evaluation.value:F0}%",
            AlertTypes.StrongWind => $"💨 Viento fuerte pronosticado: {evaluation.value:F1} m/s",
            AlertTypes.ExtremeUv => $"☀️ UV extremo pronosticado: {evaluation.value:F1}",
            AlertTypes.Thunderstorm => "⛈️ Tormenta eléctrica pronosticada",
            _ => $"⚠️ Alerta meteorológica: {threshold.Type}"
        };

        var dateStr = forecastDatetime.ToString("dd/MM/yyyy");
        string comparisonText = threshold.Comparison == "gt" ? "mayor" : (threshold.Comparison == "lt" ? "menor" : "diferente");

        var message = threshold.ThresholdValue.HasValue
            ? $"Se pronostica para el {dateStr} un valor de {evaluation.value:F1}, el cual es {comparisonText} al umbral de riesgo configurado ({threshold.ThresholdValue.Value:F1})."
            : $"Se ha detectado una condición de riesgo meteorológico pronosticada para el {dateStr}.";

        return new WeatherAlert
        {
            FuzzySystemId = config.FuzzySystemId,
            AlertType = threshold.Type,
            Severity = evaluation.severity,
            Title = title,
            Message = message,
            Recommendation = threshold.Recommendation,
            ForecastDatetime = forecastDatetime,
            ForecastValue = evaluation.value,
            GovernmentAlert = false
        };
    }

    /// <summary>
    /// Notifies all subscribers of a fuzzy system about new alerts grouped in a single notification.
    /// </summary>
    /// <param name="newAlerts">The alerts to notify subscribers about.</param>
    /// <param name="fuzzySystemId">The fuzzy system ID.</param>
    /// <param name="alertRepo">The alert repository.</param>
    /// <param name="notificationClient">The notification service client.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns></returns>
    private static async Task NotifySubscribersGroupedAsync(
        List<WeatherAlert> newAlerts,
        WeatherAlertConfig config,
        IWeatherAlertRepository alertRepo,
        INotificationServiceClient notificationClient,
        IAlertDeliveryLogRepository deliveryLogRepo,
        CancellationToken ct)
    {
        if (newAlerts.Count == 0) return;

        // Get subscribers for the fuzzy system from the notification service
        var subscribers = await notificationClient.GetSubscribersForFuzzySystemAsync(config.FuzzySystemId, ct);
        if (subscribers.Count == 0) return;

        // Build a grouped message
        var title = newAlerts.Count == 1 
            ? newAlerts[0].Title 
            : $"⚠️ {newAlerts.Count} nuevas alertas meteorológicas detectadas";
            
        var bodyBuilder = new System.Text.StringBuilder();
        if (newAlerts.Count == 1)
        {
            var a = newAlerts[0];
            bodyBuilder.AppendLine(a.Message);
            if (!string.IsNullOrEmpty(a.Recommendation))
                bodyBuilder.AppendLine($"\n💡 {a.Recommendation}");
        }
        else
        {
            bodyBuilder.AppendLine("Se han pronosticado las siguientes condiciones que superan tus umbrales:\n");
            foreach (var a in newAlerts)
            {
                bodyBuilder.AppendLine($"• {a.Title}: {a.Message}");
                if (!string.IsNullOrEmpty(a.Recommendation))
                    bodyBuilder.AppendLine($"  💡 {a.Recommendation}");
                bodyBuilder.AppendLine();
            }
        }
        
        var body = bodyBuilder.ToString().TrimEnd();

        // Data dictionary for navigation
        var data = new Dictionary<string, string>
        {
            ["fuzzySystemId"] = config.FuzzySystemId,
            ["isGrouped"] = newAlerts.Count > 1 ? "true" : "false",
            ["alertCount"] = newAlerts.Count.ToString()
        };
        if (newAlerts.Count == 1)
        {
            data["alertId"] = newAlerts[0].Id;
            data["alertType"] = newAlerts[0].AlertType;
            data["severity"] = newAlerts[0].Severity;
            data["recommendation"] = newAlerts[0].Recommendation;
            data["forecastDate"] = newAlerts[0].ForecastDatetime.ToString("dd/MM/yyyy");
        }
        else
        {
            // For grouped alerts: pick highest severity, earliest forecast date
            var hasCritical = newAlerts.Any(a => a.Severity == "critical");
            data["severity"] = hasCritical ? "critical" : "warning";
            data["forecastDate"] = newAlerts.Min(a => a.ForecastDatetime).ToString("dd/MM/yyyy");
        }

        foreach (var subscriber in subscribers)
        {
            // Per-user deduplication: skip alerts already delivered to this user
            List<WeatherAlert> alertsToSend;
            if (!config.AllowDuplicateAlerts)
            {
                alertsToSend = new List<WeatherAlert>();
                foreach (var alert in newAlerts)
                {
                    var alreadySent = await deliveryLogRepo.ExistsAsync(
                        config.FuzzySystemId,
                        alert.AlertType,
                        alert.ForecastDatetime.Date,
                        subscriber.UserId,
                        ct);

                    if (!alreadySent)
                        alertsToSend.Add(alert);
                }

                if (alertsToSend.Count == 0) continue;
            }
            else
            {
                alertsToSend = newAlerts;
            }

            // Send single grouped notification to the subscriber
            await notificationClient.SendAlertNotificationAsync(
                subscriber.UserId,
                "weather_alert",
                title,
                body,
                data,
                ct);

            // Record that this user has been notified for ALL these alerts
            foreach (var alert in alertsToSend)
            {
                await alertRepo.AddNotifiedUserAsync(alert.Id, new NotifiedUser
                {
                    UserId = subscriber.UserId,
                    Channels = subscriber.Channels,
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                }, ct);

                if (!config.AllowDuplicateAlerts)
                {
                    await deliveryLogRepo.InsertAsync(new AlertDeliveryLog
                    {
                        FuzzySystemId = config.FuzzySystemId,
                        AlertType = alert.AlertType,
                        ForecastDate = alert.ForecastDatetime.Date,
                        UserId = subscriber.UserId,
                        SentAt = DateTime.UtcNow,
                    }, ct);
                }
            }
        }
    }
}

