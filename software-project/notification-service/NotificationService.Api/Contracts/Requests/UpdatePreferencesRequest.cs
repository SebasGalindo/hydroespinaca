namespace NotificationService.Api.Contracts.Requests;

public class UpdatePreferencesRequest
{
    public List<ChannelPreferenceRequest> Channels { get; set; } = [];
    public DailySummaryConfigRequest DailySummary { get; set; } = new();
    public WeatherAlertSubscriptionRequest WeatherAlertsSubscription { get; set; } = new();
    public QuietHoursConfigRequest? QuietHours { get; set; }
}

public class ChannelPreferenceRequest
{
    public required string Channel { get; set; }
    public bool Enabled { get; set; }
    public string? Target { get; set; }
}

public class DailySummaryConfigRequest
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

public class WeatherAlertSubscriptionRequest
{
    public bool Enabled { get; set; } = true;
    public string? FuzzySystemId { get; set; }
    public List<string> AlertTypes { get; set; } = [];
}

public class QuietHoursConfigRequest
{
    public bool Enabled { get; set; }
    public int StartHour { get; set; } = 22;
    public int EndHour { get; set; } = 7;
}
