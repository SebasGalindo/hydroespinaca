using WeatherService.Domain.DTOs;

namespace WeatherService.Domain.Interfaces;

/// <summary>
/// Client for fetching weather data from OpenWeather One Call API 3.0
/// </summary>
public interface IOpenWeatherClient
{
    /// <summary>
    /// Gets the full forecast from One Call 3.0 (current + hourly 48h + daily 8 days + government alerts)
    /// Uses IMemoryCache with configurable TTL
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The full forecast response DTO.</returns>
    Task<ForecastResponseDto> GetForecastAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets only current weather (extracted from the One Call 3.0 response)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The current weather DTO.</returns>
    Task<CurrentWeatherDto> GetCurrentWeatherAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets only the daily forecast block (8 days) from One Call 3.0
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>List of daily forecast DTOs.</returns>
    Task<List<DailyForecastDto>> GetDailyForecastAsync(CancellationToken cancellationToken = default);
}
