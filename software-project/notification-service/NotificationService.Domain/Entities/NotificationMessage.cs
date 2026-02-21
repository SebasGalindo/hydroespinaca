namespace NotificationService.Domain.Entities;

/// <summary>
/// Generic notification message for multi-channel delivery.
/// Complements the existing EmailMessage (which remains for the email-specific pipeline).
/// </summary>
public class NotificationMessage
{
    public required string CorrelationId { get; set; }
    public required string UserId { get; set; }

    /// <summary>
    /// Target channel: "email", "push", "whatsapp", "web_push".
    /// </summary>
    public required string Channel { get; set; }

    /// <summary>
    /// Template key: "weather_alert", "daily_summary", "system_alert".
    /// </summary>
    public required string TemplateKey { get; set; }

    public required string Title { get; set; }

    /// <summary>
    /// HTML body for email, plain text for push/WhatsApp.
    /// </summary>
    public required string Body { get; set; }

    /// <summary>
    /// Extra payload (deeplink URLs, metadata, etc.).
    /// </summary>
    public Dictionary<string, string> Data { get; set; } = new();

    // Channel-specific targets (only one is used per send)
    public string? RecipientEmail { get; set; }
    public string? RecipientPhone { get; set; }
    public string? ExpoPushToken { get; set; }

    /// <summary>
    /// Web Push subscription JSON (endpoint + keys.p256dh + keys.auth).
    /// </summary>
    public string? WebPushSubscription { get; set; }
}
