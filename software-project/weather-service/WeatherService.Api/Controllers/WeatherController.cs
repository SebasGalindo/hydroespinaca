using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherService.Application.Features.Weather.Queries.GetCurrentWeather;
using WeatherService.Application.Features.Weather.Queries.GetDailyForecast;
using WeatherService.Application.Features.Weather.Queries.GetForecast;
using WeatherService.Domain.DTOs;

namespace WeatherService.Api.Controllers;

/// <summary>
/// Weather data endpoints using OpenWeather One Call API 3.0.
/// Internal service — only consumed by BFF via Docker network.
/// M2M authentication will be added in Phase 2 alongside the alert worker.
/// </summary>
[ApiController]
[Route("api/weather")]
[AllowAnonymous]
public class WeatherController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(IMediator mediator, ILogger<WeatherController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets current weather conditions for Mosquera, Cundinamarca.
    /// Data from One Call 3.0 current block.
    /// </summary>
    [HttpGet("current")]
    [ProducesResponseType(typeof(CurrentWeatherDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CurrentWeatherDto>> GetCurrentWeather(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting current weather");
        var result = await _mediator.Send(new GetCurrentWeatherQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets full forecast: current + hourly (48h) + daily (8 days) + government alerts.
    /// Single One Call 3.0 request, cached for 30 minutes.
    /// </summary>
    [HttpGet("forecast")]
    [ProducesResponseType(typeof(ForecastResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ForecastResponseDto>> GetForecast(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting full forecast");
        var result = await _mediator.Send(new GetForecastQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets only the daily forecast block (8 days: today + 7).
    /// Includes min/max temps, humidity, wind, UV, precipitation, and AI summary.
    /// </summary>
    [HttpGet("forecast/daily")]
    [ProducesResponseType(typeof(List<DailyForecastDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<DailyForecastDto>>> GetDailyForecast(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting daily forecast");
        var result = await _mediator.Send(new GetDailyForecastQuery(), cancellationToken);
        return Ok(result);
    }
}
