using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Api.Contracts.Requests;
using NotificationService.Application.DTOs;
using NotificationService.Application.Features.MultiChannel.Commands.SendMultiChannel;
using HydroEspinaca.Shared.Extensions;
using MediatR;

namespace NotificationService.Api.Controllers;

/// <summary>
/// Controller for multi-channel notification delivery.
/// Sends notifications to users via all their enabled channels.
/// </summary>
[ApiController]
[Route("api/notifications")]
public class MultiNotificationController : ControllerBase
{
    private readonly IMediator _mediator;

    public MultiNotificationController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Sends a notification to all enabled channels for a user.
    /// Used by weather-service to deliver alerts.
    /// </summary>
    [HttpPost("multi")]
    [Authorize(Policy = PolicyNames.NotificationSend)]
    public async Task<ActionResult<SendMultiChannelNotificationResponse>> SendMultiChannel(
        [FromBody] SendMultiChannelRequest request,
        CancellationToken ct)
    {
        var command = new SendMultiChannelNotificationCommand(
            request.UserId,
            request.TemplateKey,
            request.Title,
            request.Body,
            request.Data);

        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }
}
