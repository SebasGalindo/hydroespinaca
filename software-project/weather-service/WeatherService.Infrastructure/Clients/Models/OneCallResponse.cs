using System.Text.Json.Serialization;

namespace WeatherService.Infrastructure.Clients.Models;

/// <summary>
/// Deserialization models for OpenWeather One Call API 3.0 response
/// Endpoint: https://api.openweathermap.org/data/3.0/onecall
/// </summary>
public class OneCallResponse
{
    // Latitude and longitude of the location for which the forecast is provided
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }

    // Timezone name for the location (e.g., "America/Bogota")
    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    // Timezone offset in seconds from UTC (e.g., -18000 for Colombia UTC-5)
    [JsonPropertyName("timezone_offset")]
    public int TimezoneOffset { get; set; }

    // Weather data blocks
    [JsonPropertyName("current")]
    public CurrentData? Current { get; set; }

    // The "minutely" block provides precipitation data for each minute for the next hour
    [JsonPropertyName("minutely")]
    public List<MinutelyData>? Minutely { get; set; }

    // The "hourly" block provides weather data for each hour for the next 48 hours
    [JsonPropertyName("hourly")]
    public List<HourlyData>? Hourly { get; set; }

    // The "daily" block provides weather data for each day for the next 8 days (including today)
    [JsonPropertyName("daily")]
    public List<DailyData>? Daily { get; set; }

    // The "alerts" block provides government weather alerts for the location, if any are active at the time of the API call
    [JsonPropertyName("alerts")]
    public List<AlertData>? Alerts { get; set; }
}

/// <summary>
/// Models for the "current", "hourly", and "daily" 
/// blocks of the One Call API response.
/// </summary>
public class CurrentData
{
    [JsonPropertyName("dt")]
    public long Dt { get; set; }

    [JsonPropertyName("sunrise")]
    public long Sunrise { get; set; }

    [JsonPropertyName("sunset")]
    public long Sunset { get; set; }

    [JsonPropertyName("temp")]
    public double Temp { get; set; }

    [JsonPropertyName("feels_like")]
    public double FeelsLike { get; set; }

    [JsonPropertyName("pressure")]
    public int Pressure { get; set; }

    [JsonPropertyName("humidity")]
    public int Humidity { get; set; }

    [JsonPropertyName("dew_point")]
    public double DewPoint { get; set; }

    [JsonPropertyName("uvi")]
    public double Uvi { get; set; }

    [JsonPropertyName("clouds")]
    public int Clouds { get; set; }

    [JsonPropertyName("visibility")]
    public int Visibility { get; set; }

    [JsonPropertyName("wind_speed")]
    public double WindSpeed { get; set; }

    [JsonPropertyName("wind_deg")]
    public int WindDeg { get; set; }

    [JsonPropertyName("wind_gust")]
    public double? WindGust { get; set; }

    [JsonPropertyName("weather")]
    public List<WeatherCondition> Weather { get; set; } = new();

    [JsonPropertyName("rain")]
    public PrecipitationData? Rain { get; set; }

    [JsonPropertyName("snow")]
    public PrecipitationData? Snow { get; set; }
}

public class HourlyData
{
    [JsonPropertyName("dt")]
    public long Dt { get; set; }

    [JsonPropertyName("temp")]
    public double Temp { get; set; }

    [JsonPropertyName("feels_like")]
    public double FeelsLike { get; set; }

    [JsonPropertyName("pressure")]
    public int Pressure { get; set; }

    [JsonPropertyName("humidity")]
    public int Humidity { get; set; }

    [JsonPropertyName("dew_point")]
    public double DewPoint { get; set; }

    [JsonPropertyName("uvi")]
    public double Uvi { get; set; }

    [JsonPropertyName("clouds")]
    public int Clouds { get; set; }

    [JsonPropertyName("visibility")]
    public int Visibility { get; set; }

    [JsonPropertyName("wind_speed")]
    public double WindSpeed { get; set; }

    [JsonPropertyName("wind_deg")]
    public int WindDeg { get; set; }

    [JsonPropertyName("wind_gust")]
    public double? WindGust { get; set; }

    [JsonPropertyName("weather")]
    public List<WeatherCondition> Weather { get; set; } = new();

    [JsonPropertyName("pop")]
    public double Pop { get; set; }

    [JsonPropertyName("rain")]
    public PrecipitationData? Rain { get; set; }

    [JsonPropertyName("snow")]
    public PrecipitationData? Snow { get; set; }
}

public class DailyData
{
    [JsonPropertyName("dt")]
    public long Dt { get; set; }

    [JsonPropertyName("sunrise")]
    public long Sunrise { get; set; }

    [JsonPropertyName("sunset")]
    public long Sunset { get; set; }

    [JsonPropertyName("moonrise")]
    public long Moonrise { get; set; }

    [JsonPropertyName("moonset")]
    public long Moonset { get; set; }

    [JsonPropertyName("moon_phase")]
    public double MoonPhase { get; set; }

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("temp")]
    public DailyTemp Temp { get; set; } = new();

    [JsonPropertyName("feels_like")]
    public DailyFeelsLike FeelsLike { get; set; } = new();

    [JsonPropertyName("pressure")]
    public int Pressure { get; set; }

    [JsonPropertyName("humidity")]
    public int Humidity { get; set; }

    [JsonPropertyName("dew_point")]
    public double DewPoint { get; set; }

    [JsonPropertyName("wind_speed")]
    public double WindSpeed { get; set; }

    [JsonPropertyName("wind_deg")]
    public int WindDeg { get; set; }

    [JsonPropertyName("wind_gust")]
    public double? WindGust { get; set; }

    [JsonPropertyName("weather")]
    public List<WeatherCondition> Weather { get; set; } = new();

    [JsonPropertyName("clouds")]
    public int Clouds { get; set; }

    [JsonPropertyName("pop")]
    public double Pop { get; set; }

    [JsonPropertyName("rain")]
    public double? Rain { get; set; }

    [JsonPropertyName("snow")]
    public double? Snow { get; set; }

    [JsonPropertyName("uvi")]
    public double Uvi { get; set; }
}

/// <summary>
/// Sub-models for the "temp" and "feels_like" objects in 
/// the "daily" block of the One Call API response.
/// </summary>
public class DailyTemp
{
    [JsonPropertyName("day")]
    public double Day { get; set; }

    [JsonPropertyName("min")]
    public double Min { get; set; }

    [JsonPropertyName("max")]
    public double Max { get; set; }

    [JsonPropertyName("night")]
    public double Night { get; set; }

    [JsonPropertyName("eve")]
    public double Eve { get; set; }

    [JsonPropertyName("morn")]
    public double Morn { get; set; }
}

public class DailyFeelsLike
{
    [JsonPropertyName("day")]
    public double Day { get; set; }

    [JsonPropertyName("night")]
    public double Night { get; set; }

    [JsonPropertyName("eve")]
    public double Eve { get; set; }

    [JsonPropertyName("morn")]
    public double Morn { get; set; }
}

/// <summary>
/// Models for the "minutely" block of the One Call API response, 
/// which provides precipitation data for each minute for the next hour.
/// </summary>
public class MinutelyData
{
    [JsonPropertyName("dt")]
    public long Dt { get; set; }

    [JsonPropertyName("precipitation")]
    public double Precipitation { get; set; }
}

/// <summary>
/// Model for the "weather" array items in the One Call API response, 
/// which provides weather condition codes and descriptions.
/// </summary>
public class WeatherCondition
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("main")]
    public string Main { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;
}

/// <summary>
/// Model for the "rain" and "snow" objects in the One Call API response,
/// </summary>
public class PrecipitationData
{
    [JsonPropertyName("1h")]
    public double OneHour { get; set; }
}

/// <summary>
/// Model for the "alerts" array items in the One Call API response, 
/// which provides government weather alerts for the location.
/// </summary>
public class AlertData
{
    [JsonPropertyName("sender_name")]
    public string SenderName { get; set; } = string.Empty;

    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("start")]
    public long Start { get; set; }

    [JsonPropertyName("end")]
    public long End { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
}
