using HydroEspinaca.Shared.Abstractions;
using WeatherService.Domain.DTOs;

namespace WeatherService.Domain.Entities;

/// <summary>
/// Persisted forecast cache in MongoDB (singleton document).
/// Survives service restarts unlike IMemoryCache.
/// </summary>
public class ForecastCache : IIdentifiableMutable
{
    /// <summary>
    /// Fixed ID: "latest_forecast" (singleton pattern)
    /// </summary>
    public string Id { get; set; } = "latest_forecast";

    // When the forecast data was fetched from the external API
    public DateTime FetchedAt { get; set; }

    // When this cache entry expires and should be refreshed
    public DateTime ExpiresAt { get; set; }

    // The actual forecast data
    public CurrentWeatherDto Current { get; set; } = new();

    // List of hourly forecasts (e.g., next 48 hours)
    public List<HourlyForecastDto> Hourly { get; set; } = [];

    // List of daily forecasts (e.g., next 7 days)
    public List<DailyForecastDto> Daily { get; set; } = [];

    // List of government-issued alerts (e.g., IDEAM) active at the time of fetch
    public List<GovernmentAlertDto> GovernmentAlerts { get; set; } = [];

    public void SetId(string id) => Id = id;
}
