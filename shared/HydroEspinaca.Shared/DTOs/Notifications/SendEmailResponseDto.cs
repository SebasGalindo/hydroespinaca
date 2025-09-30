namespace HydroEspinaca.Shared.DTOs.Notifications;

/// <summary>
/// Respuesta del endpoint de envío de correo
/// </summary>
public class SendEmailResponseDto
{
    /// <summary>
    /// CorrelationId del mensaje (útil para consultar estado en logs)
    /// </summary>
    public required string Id { get; init; }
    
    /// <summary>
    /// Estado reportado: queued | sent | failed
    /// </summary>
    public required string Status { get; init; }
    
    /// <summary>
    /// Proveedor de correo utilizado (opcional)
    /// </summary>
    public string? Provider { get; init; }
    
    /// <summary>
    /// ID del mensaje en el proveedor (opcional)
    /// </summary>
    public string? ProviderMessageId { get; init; }
}