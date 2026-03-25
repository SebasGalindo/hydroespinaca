using HydroEspinaca.Shared.Abstractions;

namespace NotificationService.Domain.Entities;

/// <summary>
/// User notification preferences — channels, daily summary config, weather alert subscription.
/// One document per user in notification_preferences collection.
/// </summary>
public class NotificationPreference : IIdentifiableMutable
{
    public string Id { get; set; } = string.Empty;
    public required string UserId { get; set; }
    public List<ChannelPreference> Channels { get; set; } = [];
    public DailySummaryConfig DailySummary { get; set; } = new();
    public WeatherAlertSubscription WeatherAlertsSubscription { get; set; } = new();
    public QuietHoursConfig? QuietHours { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public void SetId(string id) => Id = id;

    public void UpdateTimestamp() => UpdatedAt = DateTime.UtcNow;

    /// <summary>
    /// Returns the list of enabled channel names for this user.
    /// </summary>
    public List<string> GetEnabledChannels()
        => Channels.Where(c => c.Enabled).Select(c => c.Channel).ToList();

    /// <summary>
    /// Checks if a specific channel is enabled.
    /// </summary>
    public bool IsChannelEnabled(string channel)
        => Channels.Any(c => c.Channel == channel && c.Enabled);
}

public class ChannelPreference
{
    /// <summary>
    /// Channel name: "email", "push", "whatsapp", "web_push".
    /// </summary>
    public required string Channel { get; set; }
    public bool Enabled { get; set; }

    /// <summary>
    /// Channel target: email address, phone number, etc.
    /// For email, auto-filled from user profile. For WhatsApp, user must set manually.
    /// </summary>
    public string? Target { get; set; }
}

public class DailySummaryConfig
{
    public bool Enabled { get; set; }

    /// <summary>
    /// Hour (0-23) in local time (UTC-5) to send the summary.
    /// </summary>
    public int Hour { get; set; } = 7;

    /// <summary>
    /// Minute (0-59).
    /// </summary>
    public int Minute { get; set; } = 0;

    /// <summary>
    /// Which channels to use for the daily summary.
    /// </summary>
    public List<string> Channels { get; set; } = ["email"];

    public bool IncludeFuzzyRules { get; set; } = true;
    public bool IncludeSensorAverages { get; set; } = true;
    public bool IncludeActuatorRuntime { get; set; } = true;
    public bool IncludeWeatherForecast { get; set; } = true;
}

public class QuietHoursConfig
{
    public bool Enabled { get; set; }

    /// <summary>
    /// Start hour (e.g., 22 = 10 PM).
    /// </summary>
    public int StartHour { get; set; } = 22;

    /// <summary>
    /// End hour (e.g., 7 = 7 AM).
    /// </summary>
    public int EndHour { get; set; } = 7;
}

public class WeatherAlertSubscription
{
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Fuzzy system to subscribe to. Null = auto-detect active system.
    /// </summary>
    public string? FuzzySystemId { get; set; }

    /// <summary>
    /// Alert types to receive. Empty = all types.
    /// </summary>
    public List<string> AlertTypes { get; set; } = [];
}
