using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using System.Threading.Channels;

namespace NotificationService.Infrastructure.Transport;

// Cola en memoria basada en Channel<T>, pensada para un solo proceso.
// En escenarios de múltiples instancias, reemplazar por una cola distribuida.
public class InMemoryEmailQueue : IEmailQueue
{
    private readonly Channel<EmailMessage> _channel = Channel.CreateUnbounded<EmailMessage>();

    public Task EnqueueAsync(EmailMessage message, CancellationToken ct = default)
    {
        _channel.Writer.TryWrite(message);
        return Task.CompletedTask;
    }

    public IAsyncEnumerable<EmailMessage> DequeueAllAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAllAsync(ct);
}
