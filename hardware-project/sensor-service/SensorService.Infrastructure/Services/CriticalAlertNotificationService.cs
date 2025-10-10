using System.Reflection;
using System.Text;
using System.Text.Json;
using DotLiquid;
using HydroEspinaca.Shared.DTOs.Notifications;
using HydroEspinaca.Shared.Authentication.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SensorService.Domain.Interfaces;
using SensorService.Domain.ValueObjects;

namespace SensorService.Infrastructure.Services;

public class CriticalAlertNotificationService : ICriticalAlertNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly M2MTokenService _m2mTokenService;
    private readonly ILogger<CriticalAlertNotificationService> _logger;
    private readonly string _notificationServiceUrl;
    private readonly Template _alertTemplate;

    // In-memory alert state management (synchronized with database)
    // TODO: In production with multiple replicas, migrate to Redis for distributed deployments
    private readonly Dictionary<string, HashSet<string>> _activeAlerts = new();
    private readonly object _alertStateLock = new();

    // Inject repository to query database state
    private readonly ISensorAlertRepository _sensorAlertRepository;
    private readonly ISensorRepository _sensorRepository;

    public CriticalAlertNotificationService(
        HttpClient httpClient,
        M2MTokenService m2mTokenService,
        IConfiguration configuration,
        ISensorAlertRepository sensorAlertRepository,
        ISensorRepository sensorRepository,
        ILogger<CriticalAlertNotificationService> logger)
    {
        _httpClient = httpClient;
        _m2mTokenService = m2mTokenService;
        _sensorAlertRepository = sensorAlertRepository;
        _sensorRepository = sensorRepository;
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

            // Get M2M access token
            var accessToken = await _m2mTokenService.GetAccessTokenAsync();

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

        lock (_alertStateLock)
        {
            // ✅ MEJORA 1: Log del estado actual en memoria
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

        // ✅ MEJORA 2: Query database to check for active unresolved alerts
        var dbActiveCount = await GetActiveAlertsCountFromDatabaseAsync(esp32Id, variableList, cancellationToken);

        _logger.LogDebug("📊 Database state for ESP32 {Esp32Id}: {DbCount} active alerts in sensor_alerts",
            esp32Id, dbActiveCount);

        // ✅ MEJORA 3: Decision logic with database validation
        lock (_alertStateLock)
        {
            if (!_activeAlerts.TryGetValue(esp32Id, out var activeVariables))
            {
                // No state in memory
                if (dbActiveCount > 0)
                {
                    _logger.LogWarning("⚠️ Memory state lost but DB shows {Count} active alerts. Re-syncing from database.",
                        dbActiveCount);

                    // Re-sync from database
                    _activeAlerts[esp32Id] = new HashSet<string>(variableList);

                    // Don't send email - alerts already exist in DB
                    _logger.LogInformation("🚫 Skipping email - alerts already active in database");
                    return false;
                }

                // No state in memory AND no active alerts in DB → send email
                _logger.LogInformation("✅ No active alerts in memory or DB → will send email");
                return true;
            }

            // Check for new variables not already tracked
            var newAlerts = variableList.Except(activeVariables).ToList();

            if (!newAlerts.Any())
            {
                _logger.LogInformation("🚫 All variables already have active alerts → skip email");
                return false;
            }

            _logger.LogInformation("✅ Found {Count} new variables in alert state → will send email: [{Variables}]",
                newAlerts.Count, string.Join(", ", newAlerts));
            return true;
        }
    }

    public Task MarkAlertAsSentAsync(string esp32Id, IEnumerable<string> alertVariables, CancellationToken cancellationToken = default)
    {
        var variableList = alertVariables.ToList();

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

            _logger.LogInformation("📌 Marked {New}/{Total} variables as sent for ESP32: {Esp32Id}",
                newVariables, variableList.Count, esp32Id);

            _logger.LogDebug("   Now tracking: [{Variables}]",
                string.Join(", ", activeVariables));
        }

        return Task.CompletedTask;
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
                }

                // Remove the ESP32 entry if no active alerts remain
                if (!activeVariables.Any())
                {
                    _activeAlerts.Remove(esp32Id);
                    _logger.LogInformation("✅ All alerts resolved for ESP32: {Esp32Id} → removed from memory",
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
    /// Query database to count active unresolved alerts for the given variables.
    /// This prevents sending duplicate emails when memory state is lost (e.g., after restart).
    /// </summary>
    private async Task<int> GetActiveAlertsCountFromDatabaseAsync(
        string esp32Id,
        IEnumerable<string> variableNames,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get all sensors for this ESP32 to find their IDs
            var sensors = await _sensorRepository.GetSensorsByEsp32IdAsync(esp32Id, cancellationToken);
            var sensorIds = sensors.Select(s => s.Id).ToList();

            if (!sensorIds.Any())
            {
                _logger.LogDebug("No sensors found for ESP32 {Esp32Id}", esp32Id);
                return 0;
            }

            // Count active alerts for these sensors
            var count = await _sensorAlertRepository.CountActiveAlertsBySensorsAsync(sensorIds, cancellationToken);

            _logger.LogDebug("Found {Count} active alerts in DB for {SensorCount} sensors of ESP32 {Esp32Id}",
                count, sensorIds.Count, esp32Id);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query database for active alerts - assuming no active alerts");
            return 0;
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