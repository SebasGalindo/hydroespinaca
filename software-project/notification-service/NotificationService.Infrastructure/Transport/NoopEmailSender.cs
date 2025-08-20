using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.Transport;

// Sender de no-op para pruebas: simula envío exitoso.
// Próximo paso: reemplazar por ResendEmailSender + SmtpEmailSender (fallback).
public class NoopEmailSender : IEmailSender
{
    public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default)
        => Task.FromResult(new EmailSendResult(true, "noop", Guid.NewGuid().ToString("N"), null));
}
