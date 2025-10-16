using BffService.Domain.DTOs;

namespace BffService.Application.Interfaces;

/// <summary>
/// Service for fetching weather information from external weather API
/// </summary>
public interface IWeatherService
{
    /// <summary>
    /// Gets current weather and daily forecast for Mosquera, Cundinamarca
    /// Uses internal cache with 1-hour TTL to optimize API usage
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Weather information</returns>
    Task<WeatherDto> GetWeatherAsync(CancellationToken cancellationToken = default);
}
