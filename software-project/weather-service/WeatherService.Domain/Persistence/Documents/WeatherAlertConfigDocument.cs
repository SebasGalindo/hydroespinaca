using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WeatherService.Domain.Persistence.Documents;

/// <summary>
/// MongoDB document representing a user's weather alert configuration.
/// </summary>
public class WeatherAlertConfigDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("fuzzy_system_id")]
    public string FuzzySystemId { get; set; } = string.Empty;

    [BsonElement("fuzzy_system_name")]
    public string FuzzySystemName { get; set; } = string.Empty;

    [BsonElement("alerts")]
    public List<AlertThresholdDocument> Alerts { get; set; } = [];

    [BsonElement("is_active")]
    public bool IsActive { get; set; } = true;

    [BsonElement("max_forecast_days")]
    public int MaxForecastDays { get; set; } = 8;

    [BsonElement("allow_duplicate_alerts")]
    public bool AllowDuplicateAlerts { get; set; } = true;

    [BsonElement("created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    [BsonElement("updated_by")]
    public string UpdatedBy { get; set; } = string.Empty;

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public void SetId(string id) => Id = id;
}

public class AlertThresholdDocument
{
    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    [BsonElement("enabled")]
    public bool Enabled { get; set; } = true;

    [BsonElement("threshold_value")]
    [BsonIgnoreIfNull]
    public double? ThresholdValue { get; set; }

    [BsonElement("comparison")]
    [BsonIgnoreIfNull]
    public string? Comparison { get; set; }

    [BsonElement("recommendation")]
    public string Recommendation { get; set; } = string.Empty;
}
