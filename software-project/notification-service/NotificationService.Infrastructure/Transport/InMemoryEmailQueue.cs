using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using System.Threading.Channels;

namespace NotificationService.Infrastructure.Transport;

/// <summary>
/// Cola en memoria basada en Channel&lt;T&gt; para envío asíncrono de emails.
/// ⚠️ ADVERTENCIA DE PRODUCCIÓN:
/// - Esta implementación NO es apta para múltiples instancias (escalado horizontal).
/// - La cola se pierde al reiniciar el servicio (no persistente).
/// - Para producción con alta disponibilidad, reemplazar por:
///   * RabbitMQ / Azure Service Bus / AWS SQS (recomendado para mensajería distribuida)
///   * Redis Streams (alternativa ligera con persistencia)
/// - Aceptable para MVP con instancia única.
/// </summary>
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
