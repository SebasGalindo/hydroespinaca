using BffService.Application.Interfaces;
using BffService.Domain.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BffService.Infrastructure.Services;

/// <summary>
/// HTTP client for notification-service microservice.
/// Proxies preferences, push subscriptions, notification history and multi-channel send.
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

    // ──────────────── Preferences ────────────────

    public async Task<NotificationPreferenceDto?> GetPreferencesAsync(string userId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Calling notification-service GET /api/preferences/{UserId}", userId);
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/preferences/{userId}", ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<NotificationPreferenceDto>(content, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling notification-service GET /api/preferences/{UserId}", userId);
            throw;
        }
    }

    public async Task<NotificationPreferenceDto> UpdatePreferencesAsync(
        string userId, UpdatePreferencesRequestDto request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Calling notification-service PUT /api/preferences/{UserId}", userId);
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{_baseUrl}/api/preferences/{userId}", httpContent, ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<NotificationPreferenceDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize preferences response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling notification-service PUT /api/preferences/{UserId}", userId);
            throw;
        }
    }

    // ──────────────── Push Subscriptions ────────────────

    public async Task<PushSubscriptionDto> RegisterPushAsync(RegisterPushRequestDto request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Calling notification-service POST /api/push/register");
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/push/register", httpContent, ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<PushSubscriptionDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize push subscription response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling notification-service POST /api/push/register");
            throw;
        }
    }

    public async Task UnregisterPushAsync(string subscriptionId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Calling notification-service DELETE /api/push/register/{SubscriptionId}", subscriptionId);
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/push/register/{subscriptionId}", ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling notification-service DELETE /api/push/register/{SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<List<PushSubscriptionDto>> GetPushSubscriptionsAsync(
        string userId, string? platform = null, CancellationToken ct = default)
    {
        try
        {
            var url = $"{_baseUrl}/api/push/subscriptions/{userId}";
            if (!string.IsNullOrEmpty(platform))
                url += $"?platform={Uri.EscapeDataString(platform)}";

            _logger.LogDebug("Calling notification-service GET {Url}", url);
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<List<PushSubscriptionDto>>(content, _jsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling notification-service GET /api/push/subscriptions/{UserId}", userId);
            throw;
        }
    }

    // ──────────────── Notification History ────────────────

    public async Task<List<NotificationLogDto>> GetNotificationHistoryAsync(
        string userId, string? channel = null, DateTime? from = null,
        DateTime? to = null, int limit = 50, CancellationToken ct = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(channel)) queryParams.Add($"channel={Uri.EscapeDataString(channel)}");
            if (from.HasValue) queryParams.Add($"from={from.Value:O}");
            if (to.HasValue) queryParams.Add($"to={to.Value:O}");
            queryParams.Add($"limit={limit}");

            var url = $"{_baseUrl}/api/notifications/log/{userId}";
            if (queryParams.Count > 0) url += "?" + string.Join("&", queryParams);

            _logger.LogDebug("Calling notification-service GET {Url}", url);
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<List<NotificationLogDto>>(content, _jsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling notification-service GET /api/notifications/log/{UserId}", userId);
            throw;
        }
    }

    // ──────────────── Multi-Channel Send ────────────────

    public async Task<SendMultiChannelResponseDto> SendMultiChannelAsync(
        SendMultiChannelRequestDto request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Calling notification-service POST /api/notifications/multi");
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/notifications/multi", httpContent, ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<SendMultiChannelResponseDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize multi-channel response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling notification-service POST /api/notifications/multi");
            throw;
        }
    }
}
