namespace NotificationService.Domain.Interfaces;

using NotificationService.Domain.Entities;

// Cola de trabajos de emails para envío asíncrono
public interface IEmailQueue
{
    Task EnqueueAsync(EmailMessage message, CancellationToken ct = default);
    IAsyncEnumerable<EmailMessage> DequeueAllAsync(CancellationToken ct = default);
}
