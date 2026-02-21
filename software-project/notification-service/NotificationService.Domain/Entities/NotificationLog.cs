using HydroEspinaca.Shared.Abstractions;

namespace NotificationService.Domain.Entities;

/// <summary>
/// Unified notification log entry — tracks all sent notifications across all channels.
/// Replaces the email-only EmailLog for the multi-channel pipeline.
/// Stored in notification_log collection.
/// </summary>
public class NotificationLog : IIdentifiableMutable
{
    public string Id { get; set; } = string.Empty;
    public required string CorrelationId { get; set; }
    public required string UserId { get; set; }

    /// <summary>
    /// Channel: "email", "push", "whatsapp", "web_push".
    /// </summary>
    public required string Channel { get; set; }

    /// <summary>
    /// Template used: "weather_alert", "daily_summary", "system_alert".
    /// </summary>
    public required string TemplateKey { get; set; }

    public required string Title { get; set; }

    /// <summary>
    /// Delivery status: "queued", "sent", "failed", "delivered".
    /// </summary>
    public string Status { get; set; } = "queued";

    /// <summary>
    /// Provider used: "resend", "smtp", "expo", "vapid", "twilio".
    /// </summary>
    public string? Provider { get; set; }

    public string? ProviderMessageId { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public void SetId(string id) => Id = id;

    public void MarkSent(string provider, string? providerMessageId = null)
    {
        Status = "sent";
        Provider = provider;
        ProviderMessageId = providerMessageId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string? provider, string error)
    {
        Status = "failed";
        Provider = provider;
        Error = error;
        UpdatedAt = DateTime.UtcNow;
    }
}
