using NotificationService.Application.DTOs;

namespace NotificationService.Application.Interfaces;

// Fachada de aplicación para envío de emails.
// Permite desacoplar el controlador del caso de uso concreto y facilitar sustituciones / decoradores.
public interface IEmailNotificationService
{
    Task<SendEmailResponseDto> SendAsync(SendEmailRequestDto request, string? idempotencyKey, CancellationToken ct);
}
