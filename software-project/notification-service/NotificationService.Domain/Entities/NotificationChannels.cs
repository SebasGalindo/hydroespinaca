namespace NotificationService.Domain.Entities;

/// <summary>
/// Defines available notification channel names.
/// </summary>
public static class NotificationChannels
{
    public const string Email = "email";
    public const string Push = "push";
    public const string WebPush = "web_push";
    public const string WhatsApp = "whatsapp";

    public static readonly string[] All = [Email, Push, WebPush, WhatsApp];
}
