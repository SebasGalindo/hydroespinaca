using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Models;

namespace NotificationService.Infrastructure.Workers;

// BackgroundService que consume la cola y realiza el envío real.
// Paso 6-7 del flujo: envío y actualización de estado en Mongo/Idempotency.
public class EmailDispatcherHostedService : BackgroundService
{
    private readonly IEmailQueue _queue;
    private readonly IEmailSender _sender;
    private readonly ILogger<EmailDispatcherHostedService> _logger;
    private readonly IEmailLogRepository _logRepo;
    private readonly IIdempotencyStore _idempotency;

    public EmailDispatcherHostedService(IEmailQueue queue, IEmailSender sender, ILogger<EmailDispatcherHostedService> logger, IEmailLogRepository logRepo, IIdempotencyStore idempotency)
    {
        _queue = queue;
        _sender = sender;
        _logger = logger;
        _logRepo = logRepo;
        _idempotency = idempotency;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Itera todos los mensajes encolados
        await foreach (var msg in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                // Enviar usando el proveedor configurado (siguiente iteración: Resend + SMTP con Polly)
                var result = await _sender.SendAsync(msg, stoppingToken);
                if (result.Success)
                {
                    // Actualizar log y marcar idempotencia como Sent
                    await _logRepo.UpdateStatusAsync(msg.CorrelationId, EmailDeliveryStatus.Sent, result.Provider, result.ProviderMessageId, null, stoppingToken);
                    await _idempotency.UpdateAsync(msg.IdempotencyKey, rec =>
                    {
                        rec.Status = IdempotencyStatus.Sent;
                    }, stoppingToken);
                }
                else
                {
                    // Actualizar log con error y marcar idempotencia como Failed
                    await _logRepo.UpdateStatusAsync(msg.CorrelationId, EmailDeliveryStatus.Failed, result.Provider, result.ProviderMessageId, result.Error, stoppingToken);
                    await _idempotency.UpdateAsync(msg.IdempotencyKey, rec =>
                    {
                        rec.Status = IdempotencyStatus.Failed;
                    }, stoppingToken);
                    _logger.LogWarning("Email send failed: {Error}", result.Error);
                }
            }
            catch (Exception ex)
            {
                // Errores no controlados del proceso de envío
                _logger.LogError(ex, "Unexpected error sending email");
            }
        }
    }
}
