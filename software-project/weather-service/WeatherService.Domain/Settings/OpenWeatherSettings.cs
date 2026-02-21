namespace WeatherService.Domain.Settings;

/// <summary>
/// Configuration settings for OpenWeather API
/// Bound from "ExternalApis:OpenWeather" configuration section
/// </summary>
public class OpenWeatherSettings
{
    /// <summary>
    /// Base URL for OpenWeather API (e.g., "https://api.openweathermap.org/data/3.0")
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.openweathermap.org/data/3.0";

    /// <summary>
    /// OpenWeather API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Latitude for weather location (default: Mosquera, Colombia)
    /// </summary>
    public double Latitude { get; set; } = 4.7002001;

    /// <summary>
    /// Longitude for weather location (default: Mosquera, Colombia)
    /// </summary>
    public double Longitude { get; set; } = -74.2385058;
}
