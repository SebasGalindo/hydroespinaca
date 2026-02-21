namespace WeatherService.Domain.Settings;

/// <summary>
/// General weather service configuration settings
/// Bound from "Weather" configuration section
/// </summary>
public class WeatherSettings
{
    /// <summary>
    /// How long to cache forecast data in minutes (default: 30)
    /// </summary>
    public int ForecastCacheMinutes { get; set; } = 30;

    /// <summary>
    /// How often the alert evaluation worker runs in minutes (default: 30)
    /// </summary>
    public int AlertEvaluationIntervalMinutes { get; set; } = 30;

    /// <summary>
    /// Deduplication window for alerts in hours (default: 6)
    /// </summary>
    public int AlertDeduplicationHours { get; set; } = 6;

    /// <summary>
    /// Timezone offset in seconds from UTC (default: -18000 for Colombia UTC-5)
    /// </summary>
    public int TimezoneOffsetSeconds { get; set; } = -18000;
}
