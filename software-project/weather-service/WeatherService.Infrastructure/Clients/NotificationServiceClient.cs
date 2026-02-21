using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WeatherService.Domain.Interfaces;

namespace WeatherService.Infrastructure.Clients;

/// <summary>
/// HTTP client for notification-service.
/// Used by the alert evaluation worker to resolve subscribers and send notifications.
/// Registered with Polly retry policy (3 retries, exponential backoff).
/// </summary>
public class NotificationServiceClient : INotificationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationServiceClient> _logger;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public NotificationServiceClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<NotificationServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["Services:NotificationService:Url"]
            ?? "http://notification-service:8080";
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <inheritdoc />
    public async Task<List<AlertSubscriberDto>> GetSubscribersForFuzzySystemAsync(
        string fuzzySystemId, CancellationToken ct = default)
    {
        try
        {
            var url = $"{_baseUrl}/api/preferences/subscribers?fuzzySystemId={Uri.EscapeDataString(fuzzySystemId)}";
            _logger.LogDebug("Getting subscribers for fuzzy system {FuzzySystemId}", fuzzySystemId);

            // Note: notification-service returns 200 with empty list if no subscribers, so non-success status codes indicate an error
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "notification-service returned {StatusCode} for subscribers query. Returning empty list.",
                    response.StatusCode);
                return [];
            }

            // Deserialize the response content into a list of AlertSubscriberDto
            var content = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<List<AlertSubscriberDto>>(content, _jsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscribers from notification-service for fuzzy system {FuzzySystemId}",
                fuzzySystemId);
            return [];
        }
    }

    /// <inheritdoc />
    public async Task SendAlertNotificationAsync(
        string userId,
        string templateKey,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default)
    {
        try
        {
            // Construct the request payload
            var url = $"{_baseUrl}/api/notifications/multi";
            var payload = new
            {
                userId,
                templateKey,
                title,
                body,
                data = data ?? new Dictionary<string, string>()
            };

            // Serialize the payload to JSON and create the HTTP content
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending {TemplateKey} notification to user {UserId}", templateKey, userId);

            // Send the POST request to notification-service
            var response = await _httpClient.PostAsync(url, content, ct);

            // Log a warning if the response indicates failure, but don't throw an exception since notification failure should not break the alert evaluation cycle
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "notification-service returned {StatusCode} sending notification to {UserId}: {Error}",
                    response.StatusCode, userId, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
            // Don't throw — notification failure should not break the alert evaluation cycle
        }
    }
}
