using Microsoft.Extensions.Logging;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.Transport;

// Enrutador con fallback: intenta primario y si falla usa secundario
//
// Comportamiento:
// - Llama al proveedor primario; si Success=false, registra y prueba con el secundario.
// - Si el primario tiene éxito, no invoca el fallback.
// - Esta clase NO define políticas de reintentos; esas se aplican en los proveedores (HttpClient + Polly) o arriba si se desea.
public class CompositeEmailSender : IEmailSender
{
    private readonly IEmailSender _primary;
    private readonly IEmailSender _fallback;
    private readonly ILogger<CompositeEmailSender> _logger;

    public CompositeEmailSender(IEmailSender primary, IEmailSender fallback, ILogger<CompositeEmailSender> logger)
    {
        _primary = primary;
        _fallback = fallback;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var first = await _primary.SendAsync(message, ct);
        if (first.Success) return first;

        _logger.LogWarning("Primary provider failed. Falling back. Error: {Error}", first.Error);
        var second = await _fallback.SendAsync(message, ct);
        if (second.Success)
        {
            _logger.LogInformation("Fallback provider succeeded.");
        }
        else
        {
            _logger.LogWarning("Fallback provider also failed. Error: {Error}", second.Error);
        }
        return second;
    }
}
