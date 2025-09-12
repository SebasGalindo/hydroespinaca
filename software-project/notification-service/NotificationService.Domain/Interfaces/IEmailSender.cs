namespace NotificationService.Domain.Interfaces;

using NotificationService.Domain.Entities;

// Abstracción del proveedor de envío (Resend, SendGrid, SMTP, etc.)
public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default);
}
