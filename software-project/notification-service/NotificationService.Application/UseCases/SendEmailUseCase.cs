using HydroEspinaca.Shared.DTOs.Notifications;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Models;

namespace NotificationService.Application.UseCases;

// Caso de uso principal: prepara y encola el envío de correo.
// Soporta envío directo (To/Cc/Bcc) o por grupo (Group).
// Paso 2 al 4 del flujo general (ver Program/Controller):
// - Genera/Gestiona Idempotency-Key (Mongo)
// - Resuelve destinatarios (directo o grupo)
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
    private readonly INotificationGroupRepository _groupRepo;

    public SendEmailUseCase(
        IEmailQueue queue, 
        ITemplateRenderer renderer, 
        IIdempotencyStore idempotency, 
        IEmailLogRepository logRepo, 
        ISanitizer sanitizer,
        INotificationGroupRepository groupRepo)
    {
        _queue = queue;
        _renderer = renderer;
        _idempotency = idempotency;
        _logRepo = logRepo;
        _sanitizer = sanitizer;
        _groupRepo = groupRepo;
    }

    public async Task<SendEmailResponseDto> SendAsync(SendEmailRequestDto request, string? idempotencyKey, CancellationToken ct)
    {
        // Generar un correlationId para trazabilidad entre capas
        var correlationId = Guid.NewGuid().ToString("N");

        // Validar que solo una opción esté presente (Group o To)
        if (!string.IsNullOrWhiteSpace(request.Group) && !string.IsNullOrWhiteSpace(request.To))
        {
            throw new ArgumentException("Cannot specify both 'Group' and 'To' fields. Choose one sending mode.");
        }
        
        if (string.IsNullOrWhiteSpace(request.Group) && string.IsNullOrWhiteSpace(request.To))
        {
            throw new ArgumentException("Either 'Group' or 'To' field must be specified.");
        }

        // Resolver destinatarios basado en el modo
        string[] toList, ccList, bccList;
        string primaryRecipient; // Para logging y idempotencia

        if (!string.IsNullOrWhiteSpace(request.Group))
        {
            // Modo grupo: resolver destinatarios desde MongoDB
            var group = await _groupRepo.GetByGroupNameAsync(request.Group, ct);
            if (group == null)
            {
                throw new ArgumentException($"Notification group '{request.Group}' not found.");
            }

            if (!group.HasActiveRecipients())
            {
                throw new ArgumentException($"Notification group '{request.Group}' has no active recipients.");
            }

            var (groupTo, groupCc, groupBcc) = group.GetRecipientsByType();
            toList = groupTo;
            ccList = groupCc;
            bccList = groupBcc;
            primaryRecipient = $"group:{request.Group}";
        }
        else
        {
            // Modo directo: usar campos To/Cc/Bcc
            toList = new[] { request.To! }.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            ccList = request.Cc?.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray() ?? Array.Empty<string>();
            bccList = request.Bcc?.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray() ?? Array.Empty<string>();
            primaryRecipient = request.To!;
        }

        if (toList.Length == 0)
        {
            throw new ArgumentException("At least one valid 'to' email address is required.");
        }

        // Si no llega header Idempotency-Key, derivarlo de un hash del payload
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var raw = $"{primaryRecipient}|{request.Subject}|{request.HtmlBody}|{(request.Attachments?.Count ?? 0)}";
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
        // Renderizar usando el layout base (simplificado)
        var finalHtml = await _renderer.RenderAsync("base", safeBody, model: null, ct);


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
            TemplateKey = "base",
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
            To = primaryRecipient, // Usar primaryRecipient (directo o grupo)
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
