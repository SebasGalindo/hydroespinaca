namespace NotificationService.Application.DTOs;

// Respuesta del endpoint de envío de correo
public class SendEmailResponseDto
{
    // CorrelationId del mensaje (útil para consultar estado en logs)
    public required string Id { get; init; }
    // Estado reportado: queued | sent | failed
    public required string Status { get; init; }
    // Datos del proveedor (si aplica)
    public string? Provider { get; init; }
    public string? ProviderMessageId { get; init; }
}
