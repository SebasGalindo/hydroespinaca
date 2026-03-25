using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Features.Email.Commands.SendEmail;
using HydroEspinaca.Shared.Extensions;
using HydroEspinaca.Shared.DTOs.Notifications;
using MediatR;

namespace NotificationService.Api.Controllers;

/// <summary>
/// Controller responsible for handling notification-related endpoints, 
/// such as sending email notifications.
/// </summary>
[ApiController]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Sends an email notification. Requires notification:send scope.
    /// </summary>
    /// <param name="dto">The details of the email to be sent, including recipient, subject, and body.</param>
    /// <param name="idemKey">An optional idempotency key to prevent duplicate sends.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A response indicating the status of the email operation, such as "queued" or "sent".</returns>
    [HttpPost("email")]
    [Authorize(Policy = PolicyNames.NotificationSend)]
    public async Task<ActionResult<SendEmailResponseDto>> SendEmail(
        [FromBody] SendEmailRequestDto dto,
        [FromHeader(Name = "Idempotency-Key")] string? idemKey,
        CancellationToken ct)
    {
        var command = new SendEmailCommand(dto, idemKey);
        var result = await _mediator.Send(command, ct);

        if (string.Equals(result.Status, "queued", StringComparison.OrdinalIgnoreCase))
            return Accepted(result);
        return Ok(result);
    }
}
