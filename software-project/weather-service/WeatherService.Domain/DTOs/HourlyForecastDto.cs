namespace WeatherService.Domain.DTOs;

/// <summary>
/// Hourly forecast data point from One Call 3.0
/// </summary>
public class HourlyForecastDto
{
    /// <summary>
    /// Forecast datetime (UTC)
    /// </summary>
    public DateTime DateTime { get; set; }

    /// <summary>
    /// Temperature in Celsius
    /// </summary>
    public double Temperature { get; set; }

    /// <summary>
    /// Feels like temperature in Celsius
    /// </summary>
    public double FeelsLike { get; set; }

    /// <summary>
    /// Atmospheric pressure in hPa
    /// </summary>
    public int Pressure { get; set; }

    /// <summary>
    /// Humidity percentage
    /// </summary>
    public int Humidity { get; set; }

    /// <summary>
    /// Dew point in Celsius
    /// </summary>
    public double DewPoint { get; set; }

    /// <summary>
    /// UV index
    /// </summary>
    public double Uvi { get; set; }

    /// <summary>
    /// Cloudiness percentage
    /// </summary>
    public int Cloudiness { get; set; }

    /// <summary>
    /// Visibility in meters
    /// </summary>
    public int Visibility { get; set; }

    /// <summary>
    /// Wind speed in m/s
    /// </summary>
    public double WindSpeed { get; set; }

    /// <summary>
    /// Wind gust in m/s
    /// </summary>
    public double? WindGust { get; set; }

    /// <summary>
    /// Wind direction in degrees
    /// </summary>
    public int WindDeg { get; set; }

    /// <summary>
    /// Probability of precipitation (0-1)
    /// </summary>
    public double Pop { get; set; }

    /// <summary>
    /// Rain volume for the hour in mm
    /// </summary>
    public double? Rain1h { get; set; }

    /// <summary>
    /// Snow volume for the hour in mm
    /// </summary>
    public double? Snow1h { get; set; }

    /// <summary>
    /// Weather condition main
    /// </summary>
    public string Main { get; set; } = string.Empty;

    /// <summary>
    /// Weather description in Spanish
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
}
