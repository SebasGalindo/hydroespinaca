using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Models;

namespace NotificationService.Infrastructure.Workers;

/// <summary>
/// BackgroundService que consume la cola y realiza el envío real de emails.
/// Flujo: Dequeue → Send (Resend/SMTP) → Update Log + Idempotency
/// 
/// ⚠️ COMPORTAMIENTO IMPORTANTE:
/// - Procesa emails de forma secuencial (uno a la vez)
/// - Si un email falla, se marca como Failed pero NO se reintenta automáticamente
/// - Los errores de un email NO bloquean el procesamiento de los siguientes
/// - Para reintentos automáticos, implementar exponential backoff en el futuro
/// </summary>
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
        _logger.LogInformation("📧 EmailDispatcher started. Waiting for messages...");
        
        await foreach (var msg in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("📤 Processing email: CorrelationId={CorrelationId}, To={To}", 
                    msg.CorrelationId, string.Join(", ", msg.To));

                var result = await _sender.SendAsync(msg, stoppingToken);
                
                if (result.Success)
                {
                    await _logRepo.UpdateStatusAsync(msg.CorrelationId, EmailDeliveryStatus.Sent, 
                        result.Provider, result.ProviderMessageId, null, stoppingToken);
                    await _idempotency.UpdateAsync(msg.IdempotencyKey, rec =>
                    {
                        rec.Status = IdempotencyStatus.Sent;
                    }, stoppingToken);
                    
                    _logger.LogInformation("✅ Email sent successfully via {Provider}. MessageId={MessageId}", 
                        result.Provider, result.ProviderMessageId);
                }
                else
                {
                    await _logRepo.UpdateStatusAsync(msg.CorrelationId, EmailDeliveryStatus.Failed, 
                        result.Provider, result.ProviderMessageId, result.Error, stoppingToken);
                    await _idempotency.UpdateAsync(msg.IdempotencyKey, rec =>
                    {
                        rec.Status = IdempotencyStatus.Failed;
                    }, stoppingToken);
                    
                    _logger.LogWarning("❌ Email send failed via {Provider}. Error: {Error}", 
                        result.Provider, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Unexpected error processing email CorrelationId={CorrelationId}. Continuing with next message.", 
                    msg.CorrelationId);
            }
        }
        
        _logger.LogInformation("🛑 EmailDispatcher stopped.");
    }
}
