namespace BffService.Domain.DTOs;

/// <summary>
/// Weather information for the dashboard
/// Data from OpenWeather API v2.5 (free tier)
/// </summary>
public class WeatherDto
{
    /// <summary>
    /// Current temperature in Celsius
    /// </summary>
    public double Temperature { get; set; }

    /// <summary>
    /// Feels like temperature in Celsius
    /// </summary>
    public double FeelsLike { get; set; }

    /// <summary>
    /// Relative humidity percentage
    /// </summary>
    public int Humidity { get; set; }

    /// <summary>
    /// Weather condition main description (e.g., "Clear", "Clouds", "Rain")
    /// </summary>
    public string Main { get; set; } = string.Empty;

    /// <summary>
    /// Detailed weather description in Spanish
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// OpenWeather icon code
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Wind speed in m/s
    /// </summary>
    public double WindSpeed { get; set; }

    /// <summary>
    /// Cloudiness percentage (0-100%)
    /// </summary>
    public int Cloudiness { get; set; }

    /// <summary>
    /// Rain volume for last hour in mm (null if no rain data available)
    /// </summary>
    public double? Rain1h { get; set; }

    /// <summary>
    /// Sunrise time in local timezone
    /// </summary>
    public DateTime Sunrise { get; set; }

    /// <summary>
    /// Sunset time in local timezone
    /// </summary>
    public DateTime Sunset { get; set; }

    /// <summary>
    /// Last update time in local timezone
    /// </summary>
    public DateTime LastUpdate { get; set; }
}
