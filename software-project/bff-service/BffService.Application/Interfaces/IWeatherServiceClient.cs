using BffService.Domain.DTOs;

namespace BffService.Application.Interfaces;

/// <summary>
/// Client for communicating with the weather-service microservice.
/// Replaces direct OpenWeather API calls — BFF now proxies through weather-service.
/// </summary>
public interface IWeatherServiceClient
{
    /// <summary>
    /// Gets current weather from weather-service (One Call 3.0 current block).
    /// Returns data compatible with the existing WeatherDto for backward compatibility.
    /// </summary>
    Task<WeatherDto> GetCurrentWeatherAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets full forecast: current + hourly (48h) + daily (8 days) + government alerts.
    /// </summary>
    Task<ForecastResponseDto> GetForecastAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets only the daily forecast block (8 days: today + 7).
    /// </summary>
    Task<List<DailyForecastItemDto>> GetDailyForecastAsync(CancellationToken cancellationToken = default);

    // --- Alert config endpoints ---

    /// <summary>
    /// Gets alert config for a fuzzy system.
    /// </summary>
    Task<WeatherAlertConfigDto?> GetAlertConfigAsync(string fuzzySystemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates alert config for a fuzzy system.
    /// </summary>
    Task<WeatherAlertConfigDto> UpdateAlertConfigAsync(
        string fuzzySystemId, UpdateAlertConfigRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds default alert config for a fuzzy system.
    /// </summary>
    Task<WeatherAlertConfigDto> SeedAlertConfigAsync(
        string fuzzySystemId, SeedAlertConfigRequestDto request, CancellationToken cancellationToken = default);

    // --- Alert history endpoints ---

    /// <summary>
    /// Gets alerts with optional filters.
    /// </summary>
    Task<List<WeatherAlertDto>> GetAlertsAsync(
        string? fuzzySystemId = null, string? userId = null,
        DateTime? from = null, DateTime? to = null,
        bool? unreadOnly = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an alert as read for a user.
    /// </summary>
    Task MarkAlertReadAsync(string alertId, string userId, CancellationToken cancellationToken = default);
}
