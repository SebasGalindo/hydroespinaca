using AuthService.Domain.Interfaces;
using HydroEspinaca.Shared.Authentication.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace AuthService.Infrastructure.Services;

/// <summary>
/// HTTP client implementation for notification service
/// </summary>
public class HttpNotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly M2MTokenService _m2mTokenService;
    private readonly ILogger<HttpNotificationService> _logger;
    private readonly string _notificationServiceUrl;

    public HttpNotificationService(
        HttpClient httpClient,
        M2MTokenService m2mTokenService,
        IConfiguration configuration,
        ILogger<HttpNotificationService> logger)
    {
        _httpClient = httpClient;
        _m2mTokenService = m2mTokenService;
        _logger = logger;
        _notificationServiceUrl = configuration["NotificationService:BaseUrl"] ?? 
                                  configuration["NOTIFICATION_SERVICE_URL"] ?? 
                                  "http://notification-service:8080";
    }

    /// <summary>
    /// Sends an email notification
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="htmlBody">HTML email body</param>
    /// <returns>True if email was sent successfully</returns>
    public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody)
    {
        try
        {
            _logger.LogInformation("Sending email notification to {To} with subject '{Subject}'", to, subject);

            // Get M2M token
            var token = await _m2mTokenService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Failed to obtain M2M token for notification service");
                return false;
            }

            // Prepare request
            var requestBody = new
            {
                to = to,
                subject = subject,
                htmlBody = htmlBody
            };

            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add authorization header
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Send request
            var response = await _httpClient.PostAsync($"{_notificationServiceUrl}/api/notifications/email", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email notification sent successfully to {To}", to);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send email notification to {To}. Status: {StatusCode}, Content: {ErrorContent}", 
                    to, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email notification to {To}", to);
            return false;
        }
    }
}