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

    public async Task<bool> ShouldSendAlertAsync(IEnumerable<string> variableCodes, CancellationToken cancellationToken = default)
    {
        var variableList = variableCodes.ToList();

        if (!variableList.Any())
            return false;

        // Query database to check for unsent email alerts (source of truth)
        List<SensorAlert> unsentAlerts;
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var sensorAlertRepository = scope.ServiceProvider.GetRequiredService<ISensorAlertRepository>();
            unsentAlerts = await sensorAlertRepository.GetUnsentEmailAlertsByVariablesAsync(variableList, cancellationToken);
        }

        if (!unsentAlerts.Any())
        {
            _logger.LogInformation("🚫 All active alerts already have emails sent → skip email");
            return false;
        }

        _logger.LogInformation("✅ Found {Count} unsent email alerts in database → will send email",
            unsentAlerts.Count);

        return true;
    }

    public async Task MarkAlertAsSentAsync(IEnumerable<string> variableCodes, CancellationToken cancellationToken = default)
    {
        var variableList = variableCodes.ToList();
        var sentAt = DateTime.UtcNow;

        if (!variableList.Any())
            return;

        // Persist email sent timestamp in database
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var sensorAlertRepository = scope.ServiceProvider.GetRequiredService<ISensorAlertRepository>();
            var unsentAlerts = await sensorAlertRepository.GetUnsentEmailAlertsByVariablesAsync(variableList, cancellationToken);

            foreach (var alert in unsentAlerts)
            {
                alert.EmailSentAt = sentAt;
                await sensorAlertRepository.UpdateAsync(alert);
            }

            _logger.LogInformation("💾 Marked {Count} alerts as sent (emailSentAt updated)", unsentAlerts.Count);
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