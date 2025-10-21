using System.Reflection;
using System.Text;
using System.Text.Json;
using DotLiquid;
using HydroEspinaca.Shared.DTOs.Notifications;
using HydroEspinaca.Shared.Authentication.Services;
using HydroEspinaca.Shared.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Infrastructure.Services;

public class Esp32OfflineNotificationService : IEsp32OfflineNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<Esp32OfflineNotificationService> _logger;
    private readonly string _notificationServiceUrl;
    private readonly Template _alertTemplate;

    public Esp32OfflineNotificationService(
        HttpClient httpClient,
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration,
        ILogger<Esp32OfflineNotificationService> logger)
    {
        _httpClient = httpClient;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _notificationServiceUrl = configuration.GetValue<string>("NotificationService:BaseUrl")
            ?? "http://notification-service:8080";

        _alertTemplate = LoadAlertTemplate();
    }

    public async Task SendOfflineAlertAsync(Esp32Alert alert, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending ESP32 offline alert for device: {Esp32Id}", alert.Esp32Id);

            // Render the alert template
            var htmlBody = RenderAlertTemplate(alert);

            // Create the notification request
            var notificationRequest = new SendEmailRequestDto
            {
                Group = "mantenimiento",
                Subject = $"⚠️ ESP32 Desconectado - {alert.Esp32Id}",
                HtmlBody = htmlBody
            };

            // Create a scope to resolve scoped services
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
                _logger.LogInformation("✅ ESP32 offline alert sent successfully for: {Esp32Id}", alert.Esp32Id);
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("❌ Failed to send ESP32 offline alert for: {Esp32Id}. Status: {Status}, Response: {Response}",
                    alert.Esp32Id, response.StatusCode, responseContent);
                throw new HttpRequestException($"Failed to send notification: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error sending ESP32 offline alert for: {Esp32Id}", alert.Esp32Id);
            throw;
        }
    }

    public async Task<bool> ShouldSendAlertAsync(string esp32Id, CancellationToken cancellationToken = default)
    {
        // Query database to check for unsent email alerts
        using var scope = _serviceScopeFactory.CreateScope();
        var alertRepository = scope.ServiceProvider.GetRequiredService<IEsp32AlertRepository>();

        var unsentAlerts = await alertRepository.GetUnsentEmailAlertsByEsp32IdsAsync(new[] { esp32Id }, cancellationToken);

        if (!unsentAlerts.Any())
        {
            _logger.LogInformation("🚫 Offline alert already sent for ESP32: {Esp32Id}", esp32Id);
            return false;
        }

        _logger.LogInformation("✅ Found {Count} unsent offline alerts for ESP32: {Esp32Id} → will send email",
            unsentAlerts.Count, esp32Id);

        return true;
    }

    public async Task MarkAlertAsSentAsync(string alertId, CancellationToken cancellationToken = default)
    {
        var sentAt = DateTime.UtcNow;

        using var scope = _serviceScopeFactory.CreateScope();
        var alertRepository = scope.ServiceProvider.GetRequiredService<IEsp32AlertRepository>();

        await alertRepository.MarkEmailAsSentAsync(alertId, sentAt, cancellationToken);

        _logger.LogInformation("💾 Marked ESP32 alert {AlertId} as sent (emailSentAt updated)", alertId);
    }

    private Template LoadAlertTemplate()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "SensorService.Infrastructure.Templates.esp32-offline-alert.liquid";

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
            _logger.LogError(ex, "Failed to load ESP32 offline alert template");
            throw;
        }
    }

    private string RenderAlertTemplate(Esp32Alert alert)
    {
        try
        {
            // Calculate time since last activity from timestamp
            var timeSinceLastActivity = DateTime.UtcNow - alert.Timestamp;
            var formattedTime = TimeFormatter.FormatInactivity(timeSinceLastActivity);

            var templateData = new
            {
                esp32Id = alert.Esp32Id,
                timestamp = alert.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                lastActivity = alert.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                timeSinceLastActivity = formattedTime
            };

            var hash = Hash.FromAnonymousObject(templateData);
            return _alertTemplate.Render(hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render ESP32 offline alert template");
            throw;
        }
    }
}
