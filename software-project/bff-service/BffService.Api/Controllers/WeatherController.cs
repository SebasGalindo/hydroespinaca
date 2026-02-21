using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BffService.Application.Interfaces;
using BffService.Domain.DTOs;

namespace BffService.Api.Controllers;

/// <summary>
/// Controller for weather information.
/// Proxies requests to the weather-service microservice (One Call API 3.0).
/// </summary>
[ApiController]
[Route("weather")]
[AllowAnonymous]
public class WeatherController : ControllerBase
{
    private readonly IWeatherServiceClient _weatherServiceClient;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(
        IWeatherServiceClient weatherServiceClient,
        ILogger<WeatherController> logger)
    {
        _weatherServiceClient = weatherServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// Gets current weather information for Mosquera, Cundinamarca.
    /// Backward compatible with existing frontend (same WeatherDto shape).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(WeatherDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WeatherDto>> GetWeather(CancellationToken cancellationToken)
    {
        try
        {
            var weather = await _weatherServiceClient.GetCurrentWeatherAsync(cancellationToken);
            return Ok(weather);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weather data from weather-service");
            return StatusCode(500, new { message = "Error al obtener datos del clima" });
        }
    }

    /// <summary>
    /// Gets full forecast: current + hourly (48h) + daily (8 days) + government alerts.
    /// Uses One Call API 3.0 via weather-service.
    /// </summary>
    [HttpGet("forecast")]
    [ProducesResponseType(typeof(ForecastResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ForecastResponseDto>> GetForecast(CancellationToken cancellationToken)
    {
        try
        {
            var forecast = await _weatherServiceClient.GetForecastAsync(cancellationToken);
            return Ok(forecast);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching forecast from weather-service");
            return StatusCode(500, new { message = "Error al obtener el pronóstico" });
        }
    }

    /// <summary>
    /// Gets daily forecast only (8 days: today + 7).
    /// </summary>
    [HttpGet("forecast/daily")]
    [ProducesResponseType(typeof(List<DailyForecastItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<DailyForecastItemDto>>> GetDailyForecast(CancellationToken cancellationToken)
    {
        try
        {
            var daily = await _weatherServiceClient.GetDailyForecastAsync(cancellationToken);
            return Ok(daily);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching daily forecast from weather-service");
            return StatusCode(500, new { message = "Error al obtener el pronóstico diario" });
        }
    }

    // --- Alert config endpoints ---

    /// <summary>
    /// Gets the alert config for the user's active fuzzy system.
    /// </summary>
    [HttpGet("alerts/config/{fuzzySystemId}")]
    [ProducesResponseType(typeof(WeatherAlertConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAlertConfig(string fuzzySystemId, CancellationToken cancellationToken)
    {
        try
        {
            var config = await _weatherServiceClient.GetAlertConfigAsync(fuzzySystemId, cancellationToken);
            if (config == null)
                return NotFound(new { message = $"No alert config found for fuzzy system '{fuzzySystemId}'" });
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching alert config for {FuzzySystemId}", fuzzySystemId);
            return StatusCode(500, new { message = "Error al obtener configuración de alertas" });
        }
    }

    /// <summary>
    /// Updates the alert config for a fuzzy system.
    /// </summary>
    [HttpPut("alerts/config/{fuzzySystemId}")]
    [ProducesResponseType(typeof(WeatherAlertConfigDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAlertConfig(
        string fuzzySystemId,
        [FromBody] UpdateAlertConfigRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _weatherServiceClient.UpdateAlertConfigAsync(fuzzySystemId, request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating alert config for {FuzzySystemId}", fuzzySystemId);
            return StatusCode(500, new { message = "Error al actualizar configuración de alertas" });
        }
    }

    /// <summary>
    /// Seeds default alert config for a fuzzy system.
    /// </summary>
    [HttpPost("alerts/config/{fuzzySystemId}/seed")]
    [ProducesResponseType(typeof(WeatherAlertConfigDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> SeedAlertConfig(
        string fuzzySystemId,
        [FromBody] SeedAlertConfigRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _weatherServiceClient.SeedAlertConfigAsync(fuzzySystemId, request, cancellationToken);
            return StatusCode(201, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding alert config for {FuzzySystemId}", fuzzySystemId);
            return StatusCode(500, new { message = "Error al crear configuración de alertas" });
        }
    }

    // --- Alert history endpoints ---

    /// <summary>
    /// Gets weather alerts with optional filters.
    /// </summary>
    [HttpGet("alerts")]
    [ProducesResponseType(typeof(List<WeatherAlertDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] string? fuzzySystemId,
        [FromQuery] string? userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool? unreadOnly,
        CancellationToken cancellationToken)
    {
        try
        {
            var alerts = await _weatherServiceClient.GetAlertsAsync(
                fuzzySystemId, userId, from, to, unreadOnly, cancellationToken);
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weather alerts");
            return StatusCode(500, new { message = "Error al obtener alertas meteorológicas" });
        }
    }

    /// <summary>
    /// Marks an alert as read for the current user.
    /// </summary>
    [HttpPatch("alerts/{alertId}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAlertRead(
        string alertId,
        [FromQuery] string userId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _weatherServiceClient.MarkAlertReadAsync(alertId, userId, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking alert {AlertId} as read", alertId);
            return StatusCode(500, new { message = "Error al marcar alerta como leída" });
        }
    }
}
