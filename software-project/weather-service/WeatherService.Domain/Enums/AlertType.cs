namespace WeatherService.Domain.Enums;

/// <summary>
/// Known alert type constants for weather thresholds.
/// </summary>
public static class AlertTypes
{
    public const string ExtremeHeat = "extreme_heat";
    public const string ExtremeCold = "extreme_cold";
    public const string HighHumidity = "high_humidity";
    public const string LowHumidity = "low_humidity";
    public const string HeavyRain = "heavy_rain";
    public const string Thunderstorm = "thunderstorm";
    public const string HighCloudiness = "high_cloudiness";
    public const string StrongWind = "strong_wind";
    public const string ExtremeUv = "extreme_uv";
    public const string Government = "government";

    public static readonly string[] All =
    [
        ExtremeHeat, ExtremeCold, HighHumidity, LowHumidity,
        HeavyRain, Thunderstorm, HighCloudiness, StrongWind, ExtremeUv
    ];
}
