using Microsoft.Extensions.Logging;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.Channels;

/// <summary>
/// Wraps the existing email pipeline (IEmailSender) as an INotificationChannel.
/// This allows the CompositeNotificationDispatcher to send emails alongside push/WhatsApp
/// using a uniform interface. The existing email queue + worker pipeline remains intact
/// for direct email sends.
/// </summary>
public class EmailChannelAdapter : INotificationChannel
{
    private readonly IEmailSender _emailSender;
    private readonly ITemplateRenderer _renderer;
    private readonly ISanitizer _sanitizer;
    private readonly ILogger<EmailChannelAdapter> _logger;

    public string ChannelType => NotificationChannels.Email;

    // Constructor injects the email sender, template renderer, sanitizer, and logger dependencies
    public EmailChannelAdapter(
        IEmailSender emailSender,
        ITemplateRenderer renderer,
        ISanitizer sanitizer,
        ILogger<EmailChannelAdapter> logger)
    {
        _emailSender = emailSender;
        _renderer = renderer;
        _sanitizer = sanitizer;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
        => Task.FromResult(true); // Email is always available (has Resend + SMTP fallback)

    /// <inheritdoc />
    public async Task<NotificationSendResult> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(message.RecipientEmail))
        {
            return new NotificationSendResult(false, ChannelType, null, null,
                "No recipient email provided");
        }

        try
        {
            string finalHtml;
            if (message.Data != null && message.Data.TryGetValue("html_body", out var customHtml))
            {
                // Use pre-rendered and sanitized custom HTML (e.g., from DailySummaryJob)
                finalHtml = customHtml;
            }
            else
            {
                // Convert plain text newlines to HTML line breaks for generic alerts
                var bodyWithBr = message.Body.Replace("\r\n", "\n").Replace("\n", "<br>");
                var safeBody = _sanitizer.Sanitize(bodyWithBr);
                finalHtml = await _renderer.RenderAsync(message.TemplateKey ?? "base", safeBody, model: null, ct);
            }

            var emailMessage = new EmailMessage
            {
                CorrelationId = message.CorrelationId,
                IdempotencyKey = $"multi_{message.CorrelationId}_{message.UserId}",
                To = [message.RecipientEmail],
                Subject = message.Title,
                HtmlBody = finalHtml,
                TemplateKey = message.TemplateKey
            };

            var result = await _emailSender.SendAsync(emailMessage, ct);

            if (result.Success)
            {
                _logger.LogInformation("Email sent to {Email} via {Provider}",
                    message.RecipientEmail, result.Provider);
                return new NotificationSendResult(true, ChannelType, result.Provider, result.ProviderMessageId, null);
            }

            _logger.LogWarning("Email failed to {Email}: {Error}", message.RecipientEmail, result.Error);
            return new NotificationSendResult(false, ChannelType, result.Provider, null, result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending email to {Email}", message.RecipientEmail);
            return new NotificationSendResult(false, ChannelType, null, null, ex.Message);
        }
    }
}
