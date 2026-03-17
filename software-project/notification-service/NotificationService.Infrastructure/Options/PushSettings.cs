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
/// WhatsApp provider selection.
/// </summary>
public enum WhatsAppProvider
{
    Twilio,
    MetaCloud
}

/// <summary>
/// Configuration for WhatsApp integration.
/// Supports Twilio and Meta Cloud API providers, selectable via <see cref="Provider"/>.
/// Bound from appsettings WhatsApp section.
/// </summary>
public class WhatsAppSettings
{
    public bool Enabled { get; set; }

    /// <summary>
    /// Which WhatsApp provider to use: "Twilio" or "MetaCloud".
    /// </summary>
    public WhatsAppProvider Provider { get; set; } = WhatsAppProvider.Twilio;

    public TwilioSettings Twilio { get; set; } = new();
    public MetaCloudSettings MetaCloud { get; set; } = new();
}

public class TwilioSettings
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
}

/// <summary>
/// Configuration for Meta Cloud API (WhatsApp Business Platform).
/// </summary>
public class MetaCloudSettings
{
    public string PhoneNumberId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string BusinessAccountId { get; set; } = string.Empty;
    /// <summary>
    /// Graph API version, e.g. "v21.0".
    /// </summary>
    public string ApiVersion { get; set; } = "v22.0";
}
