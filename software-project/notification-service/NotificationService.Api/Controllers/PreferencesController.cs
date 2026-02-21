using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Api.Contracts.Requests;
using NotificationService.Application.DTOs;
using NotificationService.Application.Features.Preferences.Queries.GetPreferences;
using NotificationService.Application.Features.Preferences.Queries.GetSubscribers;
using NotificationService.Application.Features.Preferences.Commands.UpdatePreferences;
using NotificationService.Domain.Entities;
using HydroEspinaca.Shared.Extensions;
using MediatR;

namespace NotificationService.Api.Controllers;

/// <summary>
/// Controller for managing user notification preferences.
/// </summary>
[ApiController]
[Route("api/preferences")]
public class PreferencesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PreferencesController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Gets notification preferences for a user.
    /// </summary>
    [HttpGet("{userId}")]
    [Authorize(Policy = PolicyNames.NotificationRead)]
    public async Task<ActionResult<NotificationPreference>> GetPreferences(
        string userId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPreferencesQuery(userId), ct);
        if (result == null)
            return NotFound(new { message = $"No preferences found for user '{userId}'" });
        return Ok(result);
    }

    /// <summary>
    /// Updates (or creates) notification preferences for a user.
    /// </summary>
    [HttpPut("{userId}")]
    [Authorize(Policy = PolicyNames.NotificationManage)]
    public async Task<ActionResult<NotificationPreference>> UpdatePreferences(
        string userId,
        [FromBody] UpdatePreferencesRequest request,
        CancellationToken ct)
    {
        var command = new UpdatePreferencesCommand(
            userId,
            request.Channels.Select(c => new ChannelPreferenceDto(c.Channel, c.Enabled, c.Target)).ToList(),
            new DailySummaryConfigDto(
                request.DailySummary.Enabled,
                request.DailySummary.Hour,
                request.DailySummary.Minute,
                request.DailySummary.Channels,
                request.DailySummary.IncludeFuzzyRules,
                request.DailySummary.IncludeSensorAverages,
                request.DailySummary.IncludeActuatorRuntime,
                request.DailySummary.IncludeWeatherForecast),
            new WeatherAlertSubscriptionDto(
                request.WeatherAlertsSubscription.Enabled,
                request.WeatherAlertsSubscription.FuzzySystemId,
                request.WeatherAlertsSubscription.AlertTypes),
            request.QuietHours != null
                ? new QuietHoursConfigDto(
                    request.QuietHours.Enabled,
                    request.QuietHours.StartHour,
                    request.QuietHours.EndHour)
                : null);

        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets all users subscribed to weather alerts for a specific fuzzy system.
    /// Used by weather-service to resolve notification targets.
    /// </summary>
    [HttpGet("subscribers")]
    [Authorize(Policy = PolicyNames.NotificationSend)]
    public async Task<ActionResult<IEnumerable<SubscriberDto>>> GetSubscribers(
        [FromQuery] string fuzzySystemId, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetSubscribersByFuzzySystemQuery(fuzzySystemId), ct);
        return Ok(result);
    }
}
