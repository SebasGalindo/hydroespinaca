namespace NotificationService.Infrastructure.Options;

/// <summary>
/// Configuration for Web Push VAPID keys.
/// Bound from appsettings Push:WebPush section.
/// </summary>
public class WebPushSettings
{
    public bool Enabled { get; set; }
    public string VapidPublicKey { get; set; } = string.Empty;
    public string VapidPrivateKey { get; set; } = string.Empty;
    public string VapidSubject { get; set; } = "mailto:admin@hydroespinaca.online";
}

/// <summary>
/// Configuration for Expo Push.
/// Bound from appsettings Push:Expo section.
/// </summary>
public class ExpoPushSettings
{
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Configuration for Twilio WhatsApp integration.
/// Bound from appsettings WhatsApp section.
/// </summary>
public class WhatsAppSettings
{
    public bool Enabled { get; set; }
    public TwilioSettings Twilio { get; set; } = new();
}

public class TwilioSettings
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
}
