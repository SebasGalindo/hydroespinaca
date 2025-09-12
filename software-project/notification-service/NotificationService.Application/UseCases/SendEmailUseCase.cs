using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Models;

namespace NotificationService.Application.UseCases;

// Caso de uso principal: prepara y encola el envío de correo.
// Paso 2 al 4 del flujo general (ver Program/Controller):
// - Genera/Gestiona Idempotency-Key (Mongo)
// - Sanitiza el HTML y renderiza el layout
// - Encola el mensaje
// - Registra EmailLog (Queued)
public class SendEmailUseCase : IEmailNotificationService
{
    private readonly IEmailQueue _queue;
    private readonly ITemplateRenderer _renderer;
    private readonly IIdempotencyStore _idempotency;
    private readonly IEmailLogRepository _logRepo;
    private readonly ISanitizer _sanitizer;

    public SendEmailUseCase(IEmailQueue queue, ITemplateRenderer renderer, IIdempotencyStore idempotency, IEmailLogRepository logRepo, ISanitizer sanitizer)
    {
        _queue = queue;
        _renderer = renderer;
        _idempotency = idempotency;
        _logRepo = logRepo;
        _sanitizer = sanitizer;
    }

    public async Task<SendEmailResponseDto> SendAsync(SendEmailRequestDto request, string? idempotencyKey, CancellationToken ct)
    {
        // Generar un correlationId para trazabilidad entre capas
        var correlationId = Guid.NewGuid().ToString("N");

    // Si no llega header Idempotency-Key, derivarlo de un hash del payload
    if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var raw = $"{request.To}|{request.Subject}|{request.TemplateKey}|{request.HtmlBody}|{(request.Attachments?.Count ?? 0)}";
            idempotencyKey = Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(raw)));
        }

    // Intentar reservar la idempotencia (TTL 24h). Si ya existe, devolvemos el estado previo
        var (acquired, record) = await _idempotency.TryReserveAsync(idempotencyKey!, correlationId, TimeSpan.FromHours(24), ct);
        if (!acquired)
        {
            return new SendEmailResponseDto { Id = record.CorrelationId, Status = record.Status.ToString().ToLowerInvariant() };
        }

    // Sanitizar el HTML para prevenir scripts/atributos peligrosos
        var safeBody = _sanitizer.Sanitize(request.HtmlBody);
    // Renderizar usando el layout de la plantilla (luego se reemplaza por Fluid real)
        var finalHtml = await _renderer.RenderAsync(request.TemplateKey, safeBody, model: null, ct);

        // Normalizar destinatarios (eliminar vacíos) para evitar MimeKit.ParseException "No address found"
        var toList = new[] { request.To }.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        var ccList = request.Cc?.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray() ?? Array.Empty<string>();
        var bccList = request.Bcc?.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray() ?? Array.Empty<string>();
        if (toList.Length == 0)
            throw new ArgumentException("At least one valid 'to' email address is required.");

        // Fallback de template si llega vacío explícito
        var templateKey = string.IsNullOrWhiteSpace(request.TemplateKey) ? "default" : request.TemplateKey;

        // Encolar el mensaje para envío asíncrono (BackgroundService lo consumirá)
        await _queue.EnqueueAsync(new Domain.Entities.EmailMessage
        {
            CorrelationId = correlationId,
            IdempotencyKey = idempotencyKey!,
            To = toList,
            Cc = ccList,
            Bcc = bccList,
            Subject = request.Subject,
            HtmlBody = finalHtml,
            TemplateKey = templateKey,
            Attachments = request.Attachments?.Select(a => new Domain.Entities.EmailAttachment
            {
                FileName = a.FileName,
                ContentBase64 = a.ContentBase64,
                ContentType = a.ContentType
            }).ToList() ?? new()
        }, ct);

    // Guardar log "Queued" para auditoría y seguimiento
        await _logRepo.InsertAsync(new EmailLog
        {
            Id = correlationId,
            CorrelationId = correlationId,
            To = request.To,
            Subject = request.Subject,
            Status = EmailDeliveryStatus.Queued
        }, ct);

    // Actualizar la reserva de idempotencia a estado "Queued"
        await _idempotency.UpdateAsync(idempotencyKey!, rec =>
        {
            rec.Status = IdempotencyStatus.Queued;
            rec.ResponseJson = null;
        }, ct);

    return new SendEmailResponseDto { Id = correlationId, Status = "queued" };
    }
}
