using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Api.Contracts.Requests;
using NotificationService.Application.Features.PushSubscriptions.Commands.RegisterPush;
using NotificationService.Application.Features.PushSubscriptions.Commands.UnregisterPush;
using NotificationService.Application.Features.PushSubscriptions.Queries.GetSubscriptions;
using NotificationService.Application.Features.NotificationHistory.Queries.GetHistory;
using NotificationService.Domain.Entities;
using HydroEspinaca.Shared.Extensions;
using MediatR;

namespace NotificationService.Api.Controllers;

/// <summary>
/// Controller for managing push subscriptions and viewing notification history.
/// </summary>
[ApiController]
public class PushController : ControllerBase
{
    private readonly IMediator _mediator;

    public PushController(IMediator mediator) => _mediator = mediator;

    // ──────────────── Push Subscriptions ────────────────

    /// <summary>
    /// Registers a push subscription (Expo or Web Push) for a user.
    /// </summary>
    [HttpPost("api/push/register")]
    [Authorize(Policy = PolicyNames.NotificationManage)]
    public async Task<ActionResult<PushSubscription>> RegisterPush(
        [FromBody] RegisterPushRequest request,
        CancellationToken ct)
    {
        var command = new RegisterPushCommand(
            request.UserId,
            request.Platform,
            request.Token,
            request.DeviceName);

        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(
            nameof(GetSubscriptions),
            new { userId = request.UserId },
            result);
    }

    /// <summary>
    /// Unregisters a push subscription by its ID.
    /// </summary>
    [HttpDelete("api/push/register/{subscriptionId}")]
    [Authorize(Policy = PolicyNames.NotificationManage)]
    public async Task<IActionResult> UnregisterPush(
        string subscriptionId, CancellationToken ct)
    {
        await _mediator.Send(new UnregisterPushCommand(subscriptionId), ct);
        return NoContent();
    }

    /// <summary>
    /// Gets all active push subscriptions for a user, optionally filtered by platform.
    /// </summary>
    [HttpGet("api/push/subscriptions/{userId}")]
    [Authorize(Policy = PolicyNames.NotificationRead)]
    public async Task<ActionResult<IEnumerable<PushSubscription>>> GetSubscriptions(
        string userId, [FromQuery] string? platform, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetSubscriptionsQuery(userId, platform), ct);
        return Ok(result);
    }

    // ──────────────── Notification History ────────────────

    /// <summary>
    /// Gets notification history (log) for a user, with optional filters.
    /// </summary>
    [HttpGet("api/notifications/log/{userId}")]
    [Authorize(Policy = PolicyNames.NotificationRead)]
    public async Task<ActionResult<IEnumerable<NotificationLog>>> GetNotificationHistory(
        string userId,
        [FromQuery] string? channel,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetNotificationHistoryQuery(userId, channel, from, to, limit), ct);
        return Ok(result);
    }
}
