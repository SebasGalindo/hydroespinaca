namespace WeatherService.Domain.DTOs;

/// <summary>
/// Full forecast response from One Call API 3.0
/// Contains current, hourly (48h), daily (8 days), and government alerts
/// </summary>
public class ForecastResponseDto
{
    /// <summary>
    /// Current weather conditions
    /// </summary>
    public CurrentWeatherDto Current { get; set; } = new();

    /// <summary>
    /// Hourly forecast for the next 48 hours
    /// </summary>
    public List<HourlyForecastDto> Hourly { get; set; } = new();

    /// <summary>
    /// Daily forecast for 8 days (today + 7)
    /// </summary>
    public List<DailyForecastDto> Daily { get; set; } = new();

    /// <summary>
    /// Government weather alerts (e.g., IDEAM Colombia)
    /// </summary>
    public List<GovernmentAlertDto> GovernmentAlerts { get; set; } = new();

    /// <summary>
    /// When this forecast was fetched from OpenWeather
    /// </summary>
    public DateTime FetchedAt { get; set; }

    /// <summary>
    /// Timezone name (e.g., "America/Bogota")
    /// </summary>
    public string Timezone { get; set; } = string.Empty;

    /// <summary>
    /// Timezone offset in seconds from UTC
    /// </summary>
    public int TimezoneOffset { get; set; }
}
