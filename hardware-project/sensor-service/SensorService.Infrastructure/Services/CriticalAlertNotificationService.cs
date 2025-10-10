using System.Reflection;
using System.Text;
using System.Text.Json;
using DotLiquid;
using HydroEspinaca.Shared.DTOs.Notifications;
using HydroEspinaca.Shared.Authentication.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Domain.ValueObjects;

namespace SensorService.Infrastructure.Services;

public class CriticalAlertNotificationService : ICriticalAlertNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<CriticalAlertNotificationService> _logger;
    private readonly string _notificationServiceUrl;
    private readonly Template _alertTemplate;

    // In-memory alert state management (synchronized with database)
    // TODO: In production with multiple replicas, migrate to Redis for distributed deployments
    private readonly Dictionary<string, HashSet<string>> _activeAlerts = new();
    private readonly object _alertStateLock = new();

    // ✅ Cooldown cache: Track when alerts were last sent to prevent spam
    // Key: "esp32Id:variableName", Value: timestamp of last email sent
    private readonly Dictionary<string, DateTime> _lastEmailSentCache = new();
    private readonly TimeSpan _emailCooldownPeriod = TimeSpan.FromMinutes(30);

    public CriticalAlertNotificationService(
        HttpClient httpClient,
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration,
        ILogger<CriticalAlertNotificationService> logger)
    {
        _httpClient = httpClient;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _notificationServiceUrl = configuration.GetValue<string>("NotificationService:BaseUrl")
            ?? "http://notification-service:8080";

        // Load and parse the Liquid template
        _alertTemplate = LoadAlertTemplate();
    }

    public async Task SendCriticalAlertAsync(CriticalAlertData alertData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending critical alert for ESP32: {Esp32Id}", alertData.Esp32Id);

            // Render the alert template
            var htmlBody = RenderAlertTemplate(alertData);

            // Determine the subject based on alerts
            var subject = GenerateSubject(alertData);

            // Create the notification request
            var notificationRequest = new SendEmailRequestDto
            {
                Group = "mantenimiento",
                Subject = subject,
                HtmlBody = htmlBody
            };

            // ✅ Create a scope to resolve scoped services
            using var scope = _serviceScopeFactory.CreateScope();
            var m2mTokenService = scope.ServiceProvider.GetRequiredService<M2MTokenService>();

            // Get M2M access token
            var accessToken = await m2mTokenService.GetAccessTokenAsync();

            // Send the notification
            var endpoint = $"{_notificationServiceUrl}/api/notifications/email";
            var json = JsonSerializer.Serialize(notificationRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add authorization header
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ Critical alert sent successfully for ESP32: {Esp32Id}", alertData.Esp32Id);
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("❌ Failed to send critical alert for ESP32: {Esp32Id}. Status: {Status}, Response: {Response}",
                    alertData.Esp32Id, response.StatusCode, responseContent);
                throw new HttpRequestException($"Failed to send notification: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error sending critical alert for ESP32: {Esp32Id}", alertData.Esp32Id);
            throw;
        }
    }

    public async Task<bool> ShouldSendAlertAsync(string esp32Id, IEnumerable<string> alertVariables, CancellationToken cancellationToken = default)
    {
        var variableList = alertVariables.ToList();

        // ✅ FIRST LAYER: Cooldown check (prevents spam for persistent conditions)
        var recentlySentVariables = variableList.Where(v => WasAlertSentRecently(esp32Id, v)).ToList();

        if (recentlySentVariables.Any())
        {
            _logger.LogWarning("⚠️ Duplicate alert suppressed for {Esp32Id} - variables: [{Variables}] (sent within last {Cooldown} min)",
                esp32Id, string.Join(", ", recentlySentVariables), _emailCooldownPeriod.TotalMinutes);

            // If ALL variables were recently sent, skip completely
            if (recentlySentVariables.Count == variableList.Count)
            {
                return false;
            }

            // Otherwise, filter out recently sent variables
            variableList = variableList.Except(recentlySentVariables).ToList();
            _logger.LogInformation("📧 Will send email only for new variables: [{Variables}]",
                string.Join(", ", variableList));
        }

        lock (_alertStateLock)
        {
            // ✅ Log del estado actual en memoria
            var memoryCount = _activeAlerts.TryGetValue(esp32Id, out var activeVariables)
                ? activeVariables.Count
                : 0;

            _logger.LogDebug("📊 Memory state for ESP32 {Esp32Id}: {MemoryCount} active variables in memory",
                esp32Id, memoryCount);

            if (activeVariables != null)
            {
                _logger.LogDebug("   Active in memory: [{Variables}]",
                    string.Join(", ", activeVariables));
            }

            _logger.LogDebug("   Incoming alerts: [{Variables}]",
                string.Join(", ", variableList));

            // Check if there are new alerts not in memory
            var newAlertsInMemory = activeVariables == null
                ? variableList
                : variableList.Except(activeVariables).ToList();

            if (newAlertsInMemory.Any())
            {
                _logger.LogDebug("🆕 New variables not in memory: [{Variables}]",
                    string.Join(", ", newAlertsInMemory));
            }
        }

        // ✅ SECOND LAYER: Query database to check for unsent email alerts (source of truth)
        List<SensorAlert> unsentAlerts;
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var sensorAlertRepository = scope.ServiceProvider.GetRequiredService<ISensorAlertRepository>();
            var sensorRepository = scope.ServiceProvider.GetRequiredService<ISensorRepository>();
            unsentAlerts = await GetUnsentEmailAlertsFromDatabaseAsync(esp32Id, sensorRepository, sensorAlertRepository, cancellationToken);
        }

        _logger.LogDebug("📊 Database state for ESP32 {Esp32Id}: {DbCount} unsent email alerts in sensor_alerts",
            esp32Id, unsentAlerts.Count);

        // ✅ Decision logic based on database (source of truth)
        if (!unsentAlerts.Any())
        {
            _logger.LogInformation("🚫 All active alerts already have emails sent → skip email");

            // Re-sync memory from current state (all variables are already notified)
            lock (_alertStateLock)
            {
                _activeAlerts[esp32Id] = new HashSet<string>(variableList);
            }

            return false;
        }

        // There are unsent alerts in the database → must send email
        _logger.LogInformation("✅ Found {Count} unsent email alerts in database → will send email",
            unsentAlerts.Count);

        return true;
    }

    public async Task MarkAlertAsSentAsync(string esp32Id, IEnumerable<string> alertVariables, CancellationToken cancellationToken = default)
    {
        var variableList = alertVariables.ToList();
        var sentAt = DateTime.UtcNow;

        // ✅ CRITICAL: Persist email sent timestamp in database
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var sensorAlertRepository = scope.ServiceProvider.GetRequiredService<ISensorAlertRepository>();
            var sensorRepository = scope.ServiceProvider.GetRequiredService<ISensorRepository>();
            var unsentAlerts = await GetUnsentEmailAlertsFromDatabaseAsync(esp32Id, sensorRepository, sensorAlertRepository, cancellationToken);

            foreach (var alert in unsentAlerts)
            {
                await sensorAlertRepository.MarkEmailAsSentAsync(alert.Id, sentAt, cancellationToken);
            }

            _logger.LogInformation("💾 Persisted email sent timestamp for {Count} alerts in database", unsentAlerts.Count);
        }

        // ✅ Update cooldown cache (primary anti-spam mechanism)
        MarkAlertAsSentInCache(esp32Id, variableList);

        // Also update in-memory cache for performance
        lock (_alertStateLock)
        {
            if (!_activeAlerts.TryGetValue(esp32Id, out var activeVariables))
            {
                activeVariables = new HashSet<string>();
                _activeAlerts[esp32Id] = activeVariables;
            }

            var newVariables = 0;
            foreach (var variable in variableList)
            {
                if (activeVariables.Add(variable))
                    newVariables++;
            }

            _logger.LogInformation("📌 Marked {New}/{Total} variables as sent in memory for ESP32: {Esp32Id}",
                newVariables, variableList.Count, esp32Id);

            _logger.LogDebug("   Now tracking: [{Variables}]",
                string.Join(", ", activeVariables));
        }
    }

    public Task MarkAlertAsResolvedAsync(string esp32Id, IEnumerable<string> alertVariables, CancellationToken cancellationToken = default)
    {
        var variableList = alertVariables.ToList();

        lock (_alertStateLock)
        {
            if (_activeAlerts.TryGetValue(esp32Id, out var activeVariables))
            {
                var removed = 0;
                foreach (var variable in variableList)
                {
                    if (activeVariables.Remove(variable))
                        removed++;

                    // ✅ Also clear from cooldown cache (allow new emails if condition recurs)
                    var cacheKey = $"{esp32Id}:{variable}";
                    _lastEmailSentCache.Remove(cacheKey);
                }

                // Remove the ESP32 entry if no active alerts remain
                if (!activeVariables.Any())
                {
                    _activeAlerts.Remove(esp32Id);
                    _logger.LogInformation("✅ All alerts resolved for ESP32: {Esp32Id} → removed from memory and cooldown cache",
                        esp32Id);
                }
                else
                {
                    _logger.LogInformation("✅ Resolved {Removed} variables for ESP32: {Esp32Id}, {Remaining} still active",
                        removed, esp32Id, activeVariables.Count);
                }
            }
            else
            {
                _logger.LogDebug("ℹ️ No active alerts in memory for ESP32: {Esp32Id} (already clean)",
                    esp32Id);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Check if an alert was sent recently (within cooldown period).
    /// Prevents spam for the same alert condition.
    /// </summary>
    private bool WasAlertSentRecently(string esp32Id, string variableName)
    {
        var cacheKey = $"{esp32Id}:{variableName}";

        lock (_alertStateLock)
        {
            if (_lastEmailSentCache.TryGetValue(cacheKey, out var lastSentTime))
            {
                var timeSinceLastEmail = DateTime.UtcNow - lastSentTime;

                if (timeSinceLastEmail < _emailCooldownPeriod)
                {
                    _logger.LogDebug("⏱️ Alert for {Variable} on {Esp32Id} was sent {Minutes:F1} minutes ago (cooldown: {Cooldown} min)",
                        variableName, esp32Id, timeSinceLastEmail.TotalMinutes, _emailCooldownPeriod.TotalMinutes);
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Mark an alert as recently sent in the cooldown cache.
    /// </summary>
    private void MarkAlertAsSentInCache(string esp32Id, IEnumerable<string> variableNames)
    {
        var now = DateTime.UtcNow;

        lock (_alertStateLock)
        {
            foreach (var variableName in variableNames)
            {
                var cacheKey = $"{esp32Id}:{variableName}";
                _lastEmailSentCache[cacheKey] = now;
            }

            _logger.LogDebug("🕒 Updated cooldown cache for {Count} variables on {Esp32Id}",
                variableNames.Count(), esp32Id);
        }
    }

    /// <summary>
    /// Query database to get unsent email alerts for the ESP32.
    /// This prevents sending duplicate emails when memory state is lost (e.g., after restart).
    /// </summary>
    private async Task<List<SensorAlert>> GetUnsentEmailAlertsFromDatabaseAsync(
        string esp32Id,
        ISensorRepository sensorRepository,
        ISensorAlertRepository sensorAlertRepository,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get all sensors for this ESP32 to find their IDs
            var sensors = await sensorRepository.GetSensorsByEsp32IdAsync(esp32Id, cancellationToken);
            var sensorIds = sensors.Select(s => s.Id).ToList();

            if (!sensorIds.Any())
            {
                _logger.LogDebug("No sensors found for ESP32 {Esp32Id}", esp32Id);
                return new List<SensorAlert>();
            }

            // Get active alerts that haven't been emailed yet
            var unsentAlerts = await sensorAlertRepository.GetUnsentEmailAlertsBySensorsAsync(sensorIds, cancellationToken);

            _logger.LogDebug("Found {Count} unsent email alerts in DB for {SensorCount} sensors of ESP32 {Esp32Id}",
                unsentAlerts.Count, sensorIds.Count, esp32Id);

            return unsentAlerts;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query database for unsent alerts - assuming no pending alerts");
            return new List<SensorAlert>();
        }
    }

    private Template LoadAlertTemplate()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "SensorService.Infrastructure.Templates.alert-body.liquid";
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new InvalidOperationException($"Embedded resource '{resourceName}' not found");
            }

            using var reader = new StreamReader(stream);
            var templateContent = reader.ReadToEnd();
            
            return Template.Parse(templateContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load alert template");
            throw;
        }
    }

    private string RenderAlertTemplate(CriticalAlertData alertData)
    {
        try
        {
            var templateData = new
            {
                esp32Id = alertData.Esp32Id,
                timestamp = alertData.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                manualReadings = alertData.ManualReadings.Select(r => new
                {
                    name = r.Name,
                    value = double.IsNaN(r.Value) ? "N/A" : r.Value.ToString("F2"),
                    threshold = r.Threshold,
                    isAlert = r.IsAlert
                }).ToArray(),
                contextualReadings = alertData.ContextualReadings.Select(r => new
                {
                    name = r.Name,
                    value = r.Value.ToString("F2"),
                    unit = r.Unit
                }).ToArray(),
                hasContextualReadings = alertData.ContextualReadings.Any()
            };

            var hash = Hash.FromAnonymousObject(templateData);
            return _alertTemplate.Render(hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render alert template");
            throw;
        }
    }

    private static string GenerateSubject(CriticalAlertData alertData)
    {
        var alertReadings = alertData.AlertReadings.ToList();
        
        if (alertReadings.Count == 1)
        {
            return $"⚠️ Alerta de sensor - {alertReadings.First().Name} crítico";
        }
        
        return "⚠️ Alerta de sensor - Variables críticas fuera de rango";
    }
}