using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherService.Application.Features.Alerts.Commands.MarkAlertRead;
using WeatherService.Application.Features.Alerts.Commands.SeedAlertConfig;
using WeatherService.Application.Features.Alerts.Commands.UpdateAlertConfig;
using WeatherService.Application.Features.Alerts.Queries.GetAlertConfig;
using WeatherService.Application.Features.Alerts.Queries.GetAlerts;
using WeatherService.Domain.Entities;

namespace WeatherService.Api.Controllers;

/// <summary>
/// Controller for managing weather alerts and alert configurations.
/// Provides endpoints for retrieving and updating alert configurations, 
/// as well as fetching and marking alerts as read. 
/// </summary>
[ApiController]
[Route("api/weather/alerts")]
[AllowAnonymous] // Phase 2: M2M auth deferred — internal-only service
public class WeatherAlertController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WeatherAlertController> _logger;

    public WeatherAlertController(IMediator mediator, ILogger<WeatherAlertController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets the alert config for a fuzzy system.
    /// </summary>
    [HttpGet("config/{fuzzySystemId}")]
    [ProducesResponseType(typeof(WeatherAlertConfig), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAlertConfig(string fuzzySystemId, CancellationToken cancellationToken)
    {
        var config = await _mediator.Send(new GetAlertConfigQuery(fuzzySystemId), cancellationToken);
        if (config == null)
            return NotFound(new { message = $"No alert config found for fuzzy system '{fuzzySystemId}'" });

        return Ok(config);
    }

    /// <summary>
    /// Updates the alert config for a fuzzy system.
    /// </summary>
    [HttpPut("config/{fuzzySystemId}")]
    [ProducesResponseType(typeof(WeatherAlertConfig), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAlertConfig(
        string fuzzySystemId,
        [FromBody] UpdateAlertConfigRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateAlertConfigCommand(
            fuzzySystemId,
            request.UserId,
            request.IsActive,
            request.Alerts.Select(a => new AlertThresholdDto(
                a.Type, a.Enabled, a.ThresholdValue, a.Comparison, a.Recommendation)).ToList(),
            request.MaxForecastDays,
            request.AllowDuplicateAlerts);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Seeds default alert thresholds for a fuzzy system.
    /// </summary>
    [HttpPost("config/{fuzzySystemId}/seed")]
    [ProducesResponseType(typeof(WeatherAlertConfig), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SeedAlertConfig(
        string fuzzySystemId,
        [FromBody] SeedAlertConfigRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SeedAlertConfigCommand(
            fuzzySystemId,
            request.FuzzySystemName,
            request.UserId);

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAlertConfig), new { fuzzySystemId }, result);
    }

    /// <summary>
    /// Gets weather alerts with optional filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<WeatherAlert>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] string? fuzzySystemId,
        [FromQuery] string? userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool? unreadOnly,
        CancellationToken cancellationToken)
    {
        var query = new GetAlertsQuery(fuzzySystemId, userId, from, to, unreadOnly);
        var alerts = await _mediator.Send(query, cancellationToken);
        return Ok(alerts);
    }

    /// <summary>
    /// Marks an alert as read for a specific user.
    /// </summary>
    [HttpPatch("{alertId}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAlertRead(
        string alertId,
        [FromQuery] string userId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new MarkAlertReadCommand(alertId, userId), cancellationToken);
        return NoContent();
    }
}

// Request DTOs for the controller

public record UpdateAlertConfigRequest
{
    public string UserId { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
    public List<AlertThresholdRequest> Alerts { get; init; } = [];
    public int MaxForecastDays { get; init; } = 8;
    public bool AllowDuplicateAlerts { get; init; } = true;
}

public record AlertThresholdRequest
{
    public string Type { get; init; } = string.Empty;
    public bool Enabled { get; init; } = true;
    public double? ThresholdValue { get; init; }
    public string? Comparison { get; init; }
    public string Recommendation { get; init; } = string.Empty;
}

public record SeedAlertConfigRequest
{
    public string FuzzySystemName { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
}
