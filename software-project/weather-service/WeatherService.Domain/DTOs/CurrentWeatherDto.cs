namespace WeatherService.Domain.DTOs;

/// <summary>
/// Current weather information, compatible with the existing BFF WeatherDto
/// </summary>
public class CurrentWeatherDto
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
    /// Atmospheric pressure in hPa
    /// </summary>
    public int Pressure { get; set; }

    /// <summary>
    /// Dew point in Celsius
    /// </summary>
    public double DewPoint { get; set; }

    /// <summary>
    /// UV index
    /// </summary>
    public double Uvi { get; set; }

    /// <summary>
    /// Cloudiness percentage (0-100%)
    /// </summary>
    public int Cloudiness { get; set; }

    /// <summary>
    /// Visibility in meters (max 10km)
    /// </summary>
    public int Visibility { get; set; }

    /// <summary>
    /// Wind speed in m/s
    /// </summary>
    public double WindSpeed { get; set; }

    /// <summary>
    /// Wind gust in m/s (nullable)
    /// </summary>
    public double? WindGust { get; set; }

    /// <summary>
    /// Wind direction in degrees
    /// </summary>
    public int WindDeg { get; set; }

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
    /// OpenWeather weather condition ID
    /// </summary>
    public int WeatherId { get; set; }

    /// <summary>
    /// Rain volume for last hour in mm (null if no rain)
    /// </summary>
    public double? Rain1h { get; set; }

    /// <summary>
    /// Snow volume for last hour in mm (null if no snow)
    /// </summary>
    public double? Snow1h { get; set; }

    /// <summary>
    /// Sunrise time (UTC)
    /// </summary>
    public DateTime Sunrise { get; set; }

    /// <summary>
    /// Sunset time (UTC)
    /// </summary>
    public DateTime Sunset { get; set; }

    /// <summary>
    /// Data calculation time (UTC)
    /// </summary>
    public DateTime LastUpdate { get; set; }
}
