using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Features.Email.Commands.SendEmail;
using HydroEspinaca.Shared.Extensions;
using HydroEspinaca.Shared.DTOs.Notifications;
using MediatR;

namespace NotificationService.Api.Controllers;

/// <summary>
/// Controller responsible for handling diagnostic endpoints related to the notification service,
/// such as testing email sending functionality.
/// </summary>  
[ApiController]
[Route("api/diagnostics")]
public class DiagnosticsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DiagnosticsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Test endpoint for quick email sending. Requires notification:diagnostics scope.
    /// </summary>
    /// <param name="to">The email address to which the test email will be sent.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A response indicating the status of the email operation, such as "queued" or "sent".</returns>
    [HttpPost("send-test")]
    [Authorize(Policy = PolicyNames.NotificationDiagnostics)]
    [ProducesResponseType(typeof(SendEmailResponseDto), 202)]
    public async Task<IActionResult> SendTest([FromQuery] string to, CancellationToken ct = default)
    {
        var request = new SendEmailRequestDto
        {
            To = to,
            Subject = "Prueba NotificationService",
            HtmlBody = "<p>Hola desde NotificationService.</p>"
        };

        var command = new SendEmailCommand(request, IdempotencyKey: null);
        var result = await _mediator.Send(command, ct);
        return Accepted(result);
    }
}
