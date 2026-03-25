using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NotificationService.Domain.Persistence.Documents;

/// <summary>
/// MongoDB document representing a user's notification preferences,
/// including channel-specific settings, daily summary configuration,
/// weather alert subscriptions, and quiet hours settings.
/// </summary>
public class NotificationPreferenceDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("user_id")] public string UserId { get; set; } = string.Empty;
    [BsonElement("channels")] public List<ChannelPreferenceDocument> Channels { get; set; } = [];
    [BsonElement("daily_summary")] public DailySummaryConfigDocument DailySummary { get; set; } = new();
    [BsonElement("weather_alerts_subscription")] public WeatherAlertSubscriptionDocument WeatherAlertsSubscription { get; set; } = new();
    [BsonElement("quiet_hours")] public QuietHoursConfigDocument? QuietHours { get; set; }
    [BsonElement("created_at")] public DateTime CreatedAt { get; set; }
    [BsonElement("updated_at")] public DateTime UpdatedAt { get; set; }

    public void SetId(string id) => Id = id;
}

public class ChannelPreferenceDocument
{
    [BsonElement("channel")] public string Channel { get; set; } = string.Empty;
    [BsonElement("enabled")] public bool Enabled { get; set; }
    [BsonElement("target")] public string? Target { get; set; }
}

public class DailySummaryConfigDocument
{
    [BsonElement("enabled")] public bool Enabled { get; set; }
    [BsonElement("hour")] public int Hour { get; set; }
    [BsonElement("minute")] public int Minute { get; set; }
    [BsonElement("channels")] public List<string> Channels { get; set; } = [];
    [BsonElement("include_fuzzy_rules")] public bool IncludeFuzzyRules { get; set; }
    [BsonElement("include_sensor_averages")] public bool IncludeSensorAverages { get; set; }
    [BsonElement("include_actuator_runtime")] public bool IncludeActuatorRuntime { get; set; }
    [BsonElement("include_weather_forecast")] public bool IncludeWeatherForecast { get; set; }
}

public class QuietHoursConfigDocument
{
    [BsonElement("enabled")] public bool Enabled { get; set; }
    [BsonElement("start_hour")] public int StartHour { get; set; }
    [BsonElement("end_hour")] public int EndHour { get; set; }
}

public class WeatherAlertSubscriptionDocument
{
    [BsonElement("enabled")] public bool Enabled { get; set; }
    [BsonElement("fuzzy_system_id")] public string? FuzzySystemId { get; set; }
    [BsonElement("alert_types")] public List<string> AlertTypes { get; set; } = [];
}
