using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.DTOs;
using NotificationService.Application.UseCases;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Transport;
using HydroEspinaca.Shared.Extensions;

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
    public async Task<IActionResult> SendTest([FromServices] SendEmailUseCase useCase, [FromQuery] string to, [FromQuery] string? template = null, CancellationToken ct = default)
    {
        var req = new SendEmailRequestDto
        {
            To = to,
            Subject = "Prueba NotificationService",
            TemplateKey = string.IsNullOrWhiteSpace(template) ? "layouts/base" : template,
            HtmlBody = "<p>Hola desde NotificationService.</p>"
        };
    var res = await useCase.SendAsync(req, idempotencyKey: null, ct);
        return Accepted(res);
    }

    /// <summary>
    /// Direct send endpoint using specific email provider. Requires notification:diagnostics scope.
    /// </summary>
    /// <param name="provider">Email provider: "smtp" | "resend" | "composite" (default)</param>
    [HttpPost("send-direct")] 
    [Authorize(Policy = PolicyNames.NotificationDiagnostics)]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> SendDirect(
        [FromServices] ResendEmailSender resend,
        [FromServices] SmtpEmailSender smtp,
        [FromServices] IEmailSender composite,
        [FromQuery] string to,
        [FromQuery] string provider = "composite",
        CancellationToken ct = default)
    {
        var msg = new NotificationService.Domain.Entities.EmailMessage
        {
            CorrelationId = Guid.NewGuid().ToString("N"),
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            To = new[] { to },
            Subject = "Diagnóstico NotificationService",
            HtmlBody = "<p>Mensaje de diagnóstico.</p>",
            TemplateKey = "layouts/base"
        };

        IEmailSender sender = provider.ToLowerInvariant() switch
        {
            "smtp" => smtp,
            "resend" => resend,
            _ => composite
        };

        var result = await sender.SendAsync(msg, ct);
        return Ok(new { provider = result.Provider, success = result.Success, error = result.Error });
    }
}
