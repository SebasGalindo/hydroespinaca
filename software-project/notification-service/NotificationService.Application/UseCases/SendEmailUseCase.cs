using HydroEspinaca.Shared.DTOs.Notifications;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Models;

namespace NotificationService.Application.UseCases;

/// <summary>
/// Caso de uso principal: prepara y encola el envío de correo.
/// Soporta dos modos de envío:
/// 1. Directo: Especificando To/Cc/Bcc directamente
/// 2. Por Grupo: Usando grupos de destinatarios almacenados en MongoDB
/// 
/// Flujo completo:
/// 1. Validar modo de envío (Group XOR To)
/// 2. Resolver destinatarios (grupo o directo)
/// 3. Gestionar idempotencia (evitar duplicados con TTL 24h)
/// 4. Sanitizar HTML (prevenir XSS)
/// 5. Renderizar plantilla (layout + contenido)
/// 6. Encolar mensaje (procesamiento asíncrono)
/// 7. Registrar log inicial (estado: Queued)
/// </summary>
public class SendEmailUseCase : IEmailNotificationService
{
    private readonly IEmailQueue _queue;
    private readonly ITemplateRenderer _renderer;
    private readonly IIdempotencyStore _idempotency;
    private readonly IEmailLogRepository _logRepo;
    private readonly ISanitizer _sanitizer;
    private readonly INotificationGroupRepository _groupRepo;
    private readonly ILogger<SendEmailUseCase> _logger;

    public SendEmailUseCase(
        IEmailQueue queue, 
        ITemplateRenderer renderer, 
        IIdempotencyStore idempotency, 
        IEmailLogRepository logRepo, 
        ISanitizer sanitizer,
        INotificationGroupRepository groupRepo,
        ILogger<SendEmailUseCase> logger)
    {
        _queue = queue;
        _renderer = renderer;
        _idempotency = idempotency;
        _logRepo = logRepo;
        _sanitizer = sanitizer;
        _groupRepo = groupRepo;
        _logger = logger;
    }

    public async Task<SendEmailResponseDto> SendAsync(SendEmailRequestDto request, string? idempotencyKey, CancellationToken ct)
    {
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
        string primaryRecipient;

        if (!string.IsNullOrWhiteSpace(request.Group))
        {
            _logger.LogInformation("📬 Resolving recipients for group: {GroupName}", request.Group);
            
            var group = await _groupRepo.GetByGroupNameAsync(request.Group, ct);
            if (group == null)
            {
                _logger.LogWarning("⚠️  Notification group '{GroupName}' not found", request.Group);
                throw new ArgumentException($"Notification group '{request.Group}' not found.");
            }

            if (!group.HasActiveRecipients())
            {
                _logger.LogWarning("⚠️  Notification group '{GroupName}' has no active recipients", request.Group);
                throw new ArgumentException($"Notification group '{request.Group}' has no active recipients.");
            }

            var (groupTo, groupCc, groupBcc) = group.GetRecipientsByType();
            toList = groupTo;
            ccList = groupCc;
            bccList = groupBcc;
            primaryRecipient = $"group:{request.Group}";
            
            _logger.LogInformation("✅ Group resolved: {ToCount} TO, {CcCount} CC, {BccCount} BCC", 
                toList.Length, ccList.Length, bccList.Length);
        }
        else
        {
            toList = new[] { request.To! }.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            ccList = request.Cc?.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray() ?? Array.Empty<string>();
            bccList = request.Bcc?.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray() ?? Array.Empty<string>();
            primaryRecipient = request.To!;
            
            _logger.LogInformation("📧 Direct send mode: To={To}", primaryRecipient);
        }

        if (toList.Length == 0)
        {
            throw new ArgumentException("At least one valid 'to' email address is required.");
        }

        // Gestión de idempotencia
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var raw = $"{primaryRecipient}|{request.Subject}|{request.HtmlBody}|{(request.Attachments?.Count ?? 0)}";
            idempotencyKey = Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(raw)));
            _logger.LogDebug("🔑 Generated idempotency key from payload hash");
        }

        var (acquired, record) = await _idempotency.TryReserveAsync(idempotencyKey!, correlationId, TimeSpan.FromHours(24), ct);
        if (!acquired)
        {
            _logger.LogInformation("🔄 Idempotent request detected. Returning cached result: {Status}", record.Status);
            return new SendEmailResponseDto { Id = record.CorrelationId, Status = record.Status.ToString().ToLowerInvariant() };
        }

        // Sanitización y renderizado
        _logger.LogDebug("🧹 Sanitizing HTML body");
        var safeBody = _sanitizer.Sanitize(request.HtmlBody);
        var finalHtml = await _renderer.RenderAsync("base", safeBody, model: null, ct);

        // Encolar mensaje
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

        // Auditoría
        await _logRepo.InsertAsync(new EmailLog
        {
            Id = correlationId,
            CorrelationId = correlationId,
            To = primaryRecipient,
            Subject = request.Subject,
            Status = EmailDeliveryStatus.Queued
        }, ct);

        await _idempotency.UpdateAsync(idempotencyKey!, rec =>
        {
            rec.Status = IdempotencyStatus.Queued;
            rec.ResponseJson = null;
        }, ct);

        _logger.LogInformation("✅ Email queued successfully. CorrelationId={CorrelationId}, IdempotencyKey={IdempotencyKey}", 
            correlationId, idempotencyKey);

        return new SendEmailResponseDto { Id = correlationId, Status = "queued" };
    }
}
