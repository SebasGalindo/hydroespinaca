namespace BffService.Domain.DTOs;

/// <summary>
/// Full forecast response from weather-service (One Call API 3.0).
/// Contains current, hourly (48h), daily (8 days), and government alerts.
/// </summary>
public class ForecastResponseDto
{
    public ForecastCurrentDto Current { get; set; } = new();
    public List<ForecastHourlyDto> Hourly { get; set; } = new();
    public List<DailyForecastItemDto> Daily { get; set; } = new();
    public List<GovernmentAlertDto> GovernmentAlerts { get; set; } = new();
    public DateTime FetchedAt { get; set; }
    public string Timezone { get; set; } = string.Empty;
    public int TimezoneOffset { get; set; }
}

/// <summary>
/// Current weather from One Call 3.0 (superset of existing WeatherDto)
/// </summary>
public class ForecastCurrentDto
{
    public double Temperature { get; set; }
    public double FeelsLike { get; set; }
    public int Humidity { get; set; }
    public int Pressure { get; set; }
    public double DewPoint { get; set; }
    public double Uvi { get; set; }
    public int Cloudiness { get; set; }
    public int Visibility { get; set; }
    public double WindSpeed { get; set; }
    public double? WindGust { get; set; }
    public int WindDeg { get; set; }
    public string Main { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int WeatherId { get; set; }
    public double? Rain1h { get; set; }
    public double? Snow1h { get; set; }
    public DateTime Sunrise { get; set; }
    public DateTime Sunset { get; set; }
    public DateTime LastUpdate { get; set; }
}

/// <summary>
/// Hourly forecast data point from One Call 3.0
/// </summary>
public class ForecastHourlyDto
{
    public DateTime DateTime { get; set; }
    public double Temperature { get; set; }
    public double FeelsLike { get; set; }
    public int Pressure { get; set; }
    public int Humidity { get; set; }
    public double DewPoint { get; set; }
    public double Uvi { get; set; }
    public int Cloudiness { get; set; }
    public int Visibility { get; set; }
    public double WindSpeed { get; set; }
    public double? WindGust { get; set; }
    public int WindDeg { get; set; }
    public double Pop { get; set; }
    public double? Rain1h { get; set; }
    public double? Snow1h { get; set; }
    public string Main { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int WeatherId { get; set; }
}

/// <summary>
/// Daily forecast data point from One Call 3.0 (8 days: today + 7)
/// </summary>
public class DailyForecastItemDto
{
    public DateTime DateTime { get; set; }
    public DateTime Sunrise { get; set; }
    public DateTime Sunset { get; set; }
    public DateTime Moonrise { get; set; }
    public DateTime Moonset { get; set; }
    public double MoonPhase { get; set; }
    public double TempMin { get; set; }
    public double TempMax { get; set; }
    public double TempMorn { get; set; }
    public double TempDay { get; set; }
    public double TempEve { get; set; }
    public double TempNight { get; set; }
    public double FeelsLikeMorn { get; set; }
    public double FeelsLikeDay { get; set; }
    public double FeelsLikeEve { get; set; }
    public double FeelsLikeNight { get; set; }
    public int Humidity { get; set; }
    public int Pressure { get; set; }
    public double DewPoint { get; set; }
    public int Cloudiness { get; set; }
    public double WindSpeed { get; set; }
    public double? WindGust { get; set; }
    public int WindDeg { get; set; }
    public double Uvi { get; set; }
    public double Pop { get; set; }
    public double? Rain { get; set; }
    public double? Snow { get; set; }
    public string Main { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int WeatherId { get; set; }
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Government weather alert from One Call 3.0 (e.g., IDEAM Colombia)
/// </summary>
public class GovernmentAlertDto
{
    public string SenderName { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
}
