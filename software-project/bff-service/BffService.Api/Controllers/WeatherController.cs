using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BffService.Application.Interfaces;
using BffService.Domain.DTOs;

namespace BffService.Api.Controllers;

/// <summary>
/// Controller for weather information from OpenWeather API
/// </summary>
[ApiController]
[Route("weather")]
[AllowAnonymous]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(
        IWeatherService weatherService,
        ILogger<WeatherController> logger)
    {
        _weatherService = weatherService;
        _logger = logger;
    }

    /// <summary>
    /// Gets current weather information for Mosquera, Cundinamarca
    /// </summary>
    /// <remarks>
    /// Returns cached data if available (updates every hour at 00 minutes).
    /// Uses OpenWeather API v2.5 (free tier).
    ///
    /// Example response:
    /// {
    ///   "temperature": 14.0,
    ///   "feelsLike": 13.0,
    ///   "humidity": 86,
    ///   "main": "Clouds",
    ///   "description": "muy nuboso",
    ///   "icon": "04d",
    ///   "windSpeed": 2.2,
    ///   "cloudiness": 90,
    ///   "rain1h": 0.0,
    ///   "sunrise": "2025-10-15T06:00:00",
    ///   "sunset": "2025-10-15T18:00:00",
    ///   "lastUpdate": "2025-10-15T09:00:00"
    /// }
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Weather information</returns>
    [HttpGet]
    [ProducesResponseType(typeof(WeatherDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WeatherDto>> GetWeather(CancellationToken cancellationToken)
    {
        try
        {
            var weather = await _weatherService.GetWeatherAsync(cancellationToken);
            return Ok(weather);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weather data");
            return StatusCode(500, new { message = "Error al obtener datos del clima" });
        }
    }
}
