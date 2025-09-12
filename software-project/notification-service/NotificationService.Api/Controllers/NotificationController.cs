using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;

namespace NotificationService.Api.Controllers;

// Controller principal para notificaciones.
// Flujo detallado:
// Paso 1: Llega el request POST /api/notifications/email con JWT + (opcional) Idempotency-Key.
// Paso 2: El DTO se valida por FluentValidation (registrado en Web/Application).
// Paso 3: Se delega al caso de uso (SendEmailUseCase) con la posible Idempotency-Key del header.
// Paso 4: El caso de uso maneja idempotencia, sanitiza, renderiza y encola el email.
// Paso 5: El endpoint responde 202 Accepted (queued) o 200 (si algún día es síncrono).
[ApiController]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
    private readonly IEmailNotificationService _emailService;

    public NotificationController(IEmailNotificationService emailService) => _emailService = emailService;

    // Requiere autenticación JWT
    //[Authorize]
    [HttpPost("email")]
    public async Task<ActionResult<SendEmailResponseDto>> SendEmail([FromBody] SendEmailRequestDto dto, CancellationToken ct)
    {
        // Paso 1: Obtener la clave de idempotencia del header (preferido). Si no llega, el UseCase generará un hash del payload.
        var idemKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        // Paso 2-4: Ejecutar el caso de uso
    var result = await _emailService.SendAsync(dto, idemKey, ct);
        // Paso 5: Responder según estado
        if (string.Equals(result.Status, "queued", StringComparison.OrdinalIgnoreCase))
            return Accepted(result);
        return Ok(result);
    }
}
