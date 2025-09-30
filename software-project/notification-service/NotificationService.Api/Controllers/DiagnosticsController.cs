using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.DTOs;
using NotificationService.Application.UseCases;
using HydroEspinaca.Shared.Extensions;
using HydroEspinaca.Shared.DTOs.Notifications;

namespace NotificationService.Api.Controllers;

[ApiController]
[Route("api/diagnostics")] 
public class DiagnosticsController : ControllerBase
{
    /// <summary>
    /// Test endpoint for quick email sending. Requires notification:diagnostics scope.
    /// </summary>
    [HttpPost("send-test")] 
    [Authorize(Policy = PolicyNames.NotificationDiagnostics)]
    [ProducesResponseType(typeof(SendEmailResponseDto), 202)]
    public async Task<IActionResult> SendTest([FromServices] SendEmailUseCase useCase, [FromQuery] string to, CancellationToken ct = default)
    {
        var req = new SendEmailRequestDto
        {
            To = to,
            Subject = "Prueba NotificationService",
            HtmlBody = "<p>Hola desde NotificationService.</p>"
        };
    var res = await useCase.SendAsync(req, idempotencyKey: null, ct);
        return Accepted(res);
    }

}
