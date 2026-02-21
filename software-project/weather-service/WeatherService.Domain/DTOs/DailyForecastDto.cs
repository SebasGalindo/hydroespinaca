namespace WeatherService.Domain.DTOs;

/// <summary>
/// Daily forecast data point from One Call 3.0 (8 days: today + 7)
/// </summary>
public class DailyForecastDto
{
    /// <summary>
    /// Forecast date (UTC)
    /// </summary>
    public DateTime DateTime { get; set; }

    /// <summary>
    /// Sunrise time (UTC)
    /// </summary>
    public DateTime Sunrise { get; set; }

    /// <summary>
    /// Sunset time (UTC)
    /// </summary>
    public DateTime Sunset { get; set; }

    /// <summary>
    /// Moonrise time (UTC)
    /// </summary>
    public DateTime Moonrise { get; set; }

    /// <summary>
    /// Moonset time (UTC)
    /// </summary>
    public DateTime Moonset { get; set; }

    /// <summary>
    /// Moon phase (0-1)
    /// </summary>
    public double MoonPhase { get; set; }

    // Temperature block
    public double TempMin { get; set; }
    public double TempMax { get; set; }
    public double TempMorn { get; set; }
    public double TempDay { get; set; }
    public double TempEve { get; set; }
    public double TempNight { get; set; }

    // Feels like block
    public double FeelsLikeMorn { get; set; }
    public double FeelsLikeDay { get; set; }
    public double FeelsLikeEve { get; set; }
    public double FeelsLikeNight { get; set; }

    /// <summary>
    /// Humidity percentage
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
    /// Cloudiness percentage
    /// </summary>
    public int Cloudiness { get; set; }

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
    /// Maximum UV index for the day
    /// </summary>
    public double Uvi { get; set; }

    /// <summary>
    /// Probability of precipitation (0-1)
    /// </summary>
    public double Pop { get; set; }

    /// <summary>
    /// Rain volume in mm (total for the day)
    /// </summary>
    public double? Rain { get; set; }

    /// <summary>
    /// Snow volume in mm (total for the day)
    /// </summary>
    public double? Snow { get; set; }

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

    /// <summary>
    /// AI-generated summary of the day's weather (from One Call 3.0)
    /// </summary>
    public string Summary { get; set; } = string.Empty;
}
