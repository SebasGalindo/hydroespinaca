using BffService.Domain.DTOs;

namespace BffService.Application.Interfaces;

/// <summary>
/// Client for communicating with the notification-service microservice.
/// Proxies preferences, push subscriptions, notification history and multi-channel send.
/// </summary>
public interface INotificationServiceClient
{
    // ──────── Preferences ────────
    Task<NotificationPreferenceDto?> GetPreferencesAsync(string userId, CancellationToken ct = default);
    Task<NotificationPreferenceDto> UpdatePreferencesAsync(string userId, UpdatePreferencesRequestDto request, CancellationToken ct = default);

    // ──────── Push Subscriptions ────────
    Task<PushSubscriptionDto> RegisterPushAsync(RegisterPushRequestDto request, CancellationToken ct = default);
    Task UnregisterPushAsync(string subscriptionId, CancellationToken ct = default);
    Task<List<PushSubscriptionDto>> GetPushSubscriptionsAsync(string userId, string? platform = null, CancellationToken ct = default);

    // ──────── Notification History ────────
    Task<List<NotificationLogDto>> GetNotificationHistoryAsync(
        string userId, string? channel = null, DateTime? from = null,
        DateTime? to = null, int limit = 50, CancellationToken ct = default);

    // ──────── Multi-Channel Send ────────
    Task<SendMultiChannelResponseDto> SendMultiChannelAsync(SendMultiChannelRequestDto request, CancellationToken ct = default);
}
