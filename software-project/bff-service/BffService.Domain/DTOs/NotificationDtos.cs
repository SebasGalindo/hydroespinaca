namespace BffService.Domain.DTOs;

// ──────────────── Notification Preferences ────────────────

public class NotificationPreferenceDto
{
    public string? Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<ChannelPreferenceDto> Channels { get; set; } = [];
    public DailySummaryConfigDto? DailySummary { get; set; }
    public WeatherAlertSubscriptionDto? WeatherAlertsSubscription { get; set; }
    public QuietHoursConfigDto? QuietHours { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ChannelPreferenceDto
{
    public string Channel { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string? Target { get; set; }
}

public class DailySummaryConfigDto
{
    public bool Enabled { get; set; }
    public int Hour { get; set; } = 7;
    public int Minute { get; set; }
    public List<string> Channels { get; set; } = ["email"];
    public bool IncludeFuzzyRules { get; set; } = true;
    public bool IncludeSensorAverages { get; set; } = true;
    public bool IncludeActuatorRuntime { get; set; } = true;
    public bool IncludeWeatherForecast { get; set; } = true;
}

public class WeatherAlertSubscriptionDto
{
    public bool Enabled { get; set; } = true;
    public string? FuzzySystemId { get; set; }
    public List<string> AlertTypes { get; set; } = [];
}

public class QuietHoursConfigDto
{
    public bool Enabled { get; set; }
    public int StartHour { get; set; } = 22;
    public int EndHour { get; set; } = 7;
}

public class UpdatePreferencesRequestDto
{
    public List<ChannelPreferenceDto> Channels { get; set; } = [];
    public DailySummaryConfigDto DailySummary { get; set; } = new();
    public WeatherAlertSubscriptionDto WeatherAlertsSubscription { get; set; } = new();
    public QuietHoursConfigDto? QuietHours { get; set; }
}

// ──────────────── Push Subscriptions ────────────────

public class PushSubscriptionDto
{
    public string? Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public bool IsActive { get; set; } = true;
    public int FailureCount { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class RegisterPushRequestDto
{
    public string UserId { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
}

// ──────────────── Notification Log ────────────────

public class NotificationLogDto
{
    public string? Id { get; set; }
    public string? CorrelationId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string? TemplateKey { get; set; }
    public string? Title { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? Error { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
}

// ──────────────── Multi-Channel Send ────────────────

public class SendMultiChannelRequestDto
{
    public string UserId { get; set; } = string.Empty;
    public string TemplateKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string>? Data { get; set; }
}

public class SendMultiChannelResponseDto
{
    public string? CorrelationId { get; set; }
    public List<ChannelResultDto> Results { get; set; } = [];
}

public class ChannelResultDto
{
    public string Channel { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Provider { get; set; }
    public string? Error { get; set; }
}
