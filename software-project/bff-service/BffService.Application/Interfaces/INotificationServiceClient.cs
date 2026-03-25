using BffService.Domain.DTOs;

namespace BffService.Application.Interfaces;

/// <summary>
/// Client for communicating with the notification-service microservice.
/// Proxies preferences, push subscriptions, notification history and multi-channel send.
/// </summary>
public interface INotificationServiceClient
{
    // ──────── Preferences ────────
    Task<NotificationPreferenceDto?> GetPreferencesAsync(string userId, string accessToken, CancellationToken ct = default);
    Task<NotificationPreferenceDto> UpdatePreferencesAsync(string userId, UpdatePreferencesRequestDto request, string accessToken, CancellationToken ct = default);

    // ──────── Push Subscriptions ────────
    Task<PushSubscriptionDto> RegisterPushAsync(RegisterPushRequestDto request, string accessToken, CancellationToken ct = default);
    Task UnregisterPushAsync(string subscriptionId, string accessToken, CancellationToken ct = default);
    Task<List<PushSubscriptionDto>> GetPushSubscriptionsAsync(string userId, string accessToken, string? platform = null, CancellationToken ct = default);

    // ──────── Notification History ────────
    Task<List<NotificationLogDto>> GetNotificationHistoryAsync(
        string userId, string accessToken, string? channel = null, DateTime? from = null,
        DateTime? to = null, int limit = 50, CancellationToken ct = default);

    // ──────── Multi-Channel Send ────────
    Task<SendMultiChannelResponseDto> SendMultiChannelAsync(SendMultiChannelRequestDto request, string accessToken, CancellationToken ct = default);
}

