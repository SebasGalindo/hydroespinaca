using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson.Serialization.Attributes;
using WeatherService.Domain.DTOs;

namespace WeatherService.Domain.Persistence.Documents;

/// <summary>
/// MongoDB document for caching the latest weather forecast data.
/// </summary>
public class ForecastCacheDocument : IIdentifiableMutable
{
    /// <summary>
    /// Fixed: "latest_forecast" (singleton)
    /// </summary>
    [BsonId]
    public string Id { get; set; } = "latest_forecast";

    [BsonElement("fetched_at")]
    public DateTime FetchedAt { get; set; }

    [BsonElement("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [BsonElement("current")]
    public CurrentWeatherDto Current { get; set; } = new();

    [BsonElement("hourly")]
    public List<HourlyForecastDto> Hourly { get; set; } = [];

    [BsonElement("daily")]
    public List<DailyForecastDto> Daily { get; set; } = [];

    [BsonElement("government_alerts")]
    public List<GovernmentAlertDto> GovernmentAlerts { get; set; } = [];

    public void SetId(string id) => Id = id;
}
