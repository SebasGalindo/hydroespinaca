using HydroEspinaca.Shared.DTOs.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Entities;

namespace NotificationService.Application.Features.Email.Commands.SendEmail;

/// <summary>
/// Handles the SendEmailCommand by validating the command, resolving recipients, and queuing the email for sending.
/// </summary>
/// <remarks>
/// This handler performs several key functions:
/// 1. Validates that either a recipient email or a notification group is specified, but not both.
/// 2. Resolves the list of recipients based on the specified group or direct email address.
/// 3. Manages idempotency to prevent duplicate email sends in case of retries.
/// 4. Sanitizes and renders the email body using a template renderer.
/// 5. Enqueues the email message for sending and logs the operation in the email log repository.
/// </remarks>
public class SendEmailCommandHandler : IRequestHandler<SendEmailCommand, SendEmailResponseDto>
{
    private readonly IEmailQueue _queue;
    private readonly ITemplateRenderer _renderer;
    private readonly IIdempotencyStore _idempotency;
    private readonly IEmailLogRepository _logRepo;
    private readonly ISanitizer _sanitizer;
    private readonly INotificationGroupRepository _groupRepo;
    private readonly ILogger<SendEmailCommandHandler> _logger;

    public SendEmailCommandHandler(
        IEmailQueue queue,
        ITemplateRenderer renderer,
        IIdempotencyStore idempotency,
        IEmailLogRepository logRepo,
        ISanitizer sanitizer,
        INotificationGroupRepository groupRepo,
        ILogger<SendEmailCommandHandler> logger)
    {
        _queue = queue;
        _renderer = renderer;
        _idempotency = idempotency;
        _logRepo = logRepo;
        _sanitizer = sanitizer;
        _groupRepo = groupRepo;
        _logger = logger;
    }

    /// <summary>
    /// Handles the SendEmailCommand by performing validation, recipient resolution, idempotency management, email sanitization and rendering, and finally enqueuing the email for sending. Returns a response indicating the status of the email operation.
    /// </summary>
    /// <param name="command">The SendEmailCommand containing the email details and optional idempotency key.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A SendEmailResponseDto indicating the status of the email operation, such as "queued" or "sent".</returns>
    public async Task<SendEmailResponseDto> Handle(SendEmailCommand command, CancellationToken ct)
    {
        var request = command.Request;
        var correlationId = Guid.NewGuid().ToString("N");

        // Validate mutual exclusivity: Group XOR To
        if (!string.IsNullOrWhiteSpace(request.Group) && !string.IsNullOrWhiteSpace(request.To))
            throw new ArgumentException("Cannot specify both 'Group' and 'To' fields. Choose one sending mode.");
        if (string.IsNullOrWhiteSpace(request.Group) && string.IsNullOrWhiteSpace(request.To))
            throw new ArgumentException("Either 'Group' or 'To' field must be specified.");

        // Resolve recipients based on mode
        string[] toList, ccList, bccList;
        string primaryRecipient;

        if (!string.IsNullOrWhiteSpace(request.Group))
        {
            // Group mode: fetch recipients from the specified notification group
            var group = await _groupRepo.GetByGroupNameAsync(request.Group, ct);
            if (group == null)
                throw new ArgumentException($"Notification group '{request.Group}' not found.");
            if (!group.HasActiveRecipients())
                throw new ArgumentException($"Notification group '{request.Group}' has no active recipients.");

            var (groupTo, groupCc, groupBcc) = group.GetRecipientsByType();
            toList = groupTo;
            ccList = groupCc;
            bccList = groupBcc;
            primaryRecipient = $"group:{request.Group}";
        }
        else
        {
            // Direct mode: use the provided email addresses
            toList = new[] { request.To! }.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            ccList = request.Cc?.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray() ?? Array.Empty<string>();
            bccList = request.Bcc?.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray() ?? Array.Empty<string>();
            primaryRecipient = request.To!;
        }

        if (toList.Length == 0)
            throw new ArgumentException("At least one valid 'to' email address is required.");

        // Idempotency management
        var idempotencyKey = command.IdempotencyKey;
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            // If no idempotency key is provided, generate one based on the email content to prevent duplicates
            using var sha = System.Security.Cryptography.SHA256.Create();
            var raw = $"{primaryRecipient}|{request.Subject}|{request.HtmlBody}|{(request.Attachments?.Count ?? 0)}";
            idempotencyKey = Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(raw)));
        }

        // Attempt to reserve the idempotency key. If it already exists, return the existing status.
        var (acquired, record) = await _idempotency.TryReserveAsync(idempotencyKey!, correlationId, TimeSpan.FromHours(24), ct);
        if (!acquired)
            return new SendEmailResponseDto { Id = record.CorrelationId, Status = record.Status.ToString().ToLowerInvariant() };

        // Sanitize + render
        var safeBody = _sanitizer.Sanitize(request.HtmlBody);
        var finalHtml = await _renderer.RenderAsync("base", safeBody, model: null, ct);

        // Enqueue
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

        // Audit log
        await _logRepo.InsertAsync(new EmailLog
        {
            Id = correlationId,
            CorrelationId = correlationId,
            To = primaryRecipient,
            Subject = request.Subject,
            Status = EmailDeliveryStatus.Queued
        }, ct);

        // Update idempotency record to reflect that the email has been queued
        await _idempotency.UpdateAsync(idempotencyKey!, rec =>
        {
            rec.Status = IdempotencyStatus.Queued;
            rec.ResponseJson = null;
        }, ct);

        return new SendEmailResponseDto { Id = correlationId, Status = "queued" };
    }
}
