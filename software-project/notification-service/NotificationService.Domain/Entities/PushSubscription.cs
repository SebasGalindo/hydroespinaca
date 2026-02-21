using HydroEspinaca.Shared.Abstractions;

namespace NotificationService.Domain.Entities;

/// <summary>
/// Push notification subscription (Expo or Web Push).
/// Stored in push_subscriptions collection.
/// </summary>
public class PushSubscription : IIdentifiableMutable
{
    public string Id { get; set; } = string.Empty;
    public required string UserId { get; set; }

    /// <summary>
    /// Platform: "expo" or "web".
    /// </summary>
    public required string Platform { get; set; }

    /// <summary>
    /// Expo push token (ExponentPushToken[xxx]) or Web Push subscription JSON.
    /// </summary>
    public required string Token { get; set; }

    /// <summary>
    /// Human-readable device name.
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Deactivated after 3 consecutive failures.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Consecutive failure count. Reset on success.
    /// </summary>
    public int FailureCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public void SetId(string id) => Id = id;

    public void RecordSuccess()
    {
        FailureCount = 0;
        LastUsedAt = DateTime.UtcNow;
    }

    public void RecordFailure()
    {
        FailureCount++;
        if (FailureCount >= 3)
            IsActive = false;
    }
}
