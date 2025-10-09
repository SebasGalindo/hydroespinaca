namespace HydroEspinaca.Shared.DTOs.Notifications;

/// <summary>
/// Contrato de entrada del endpoint de envío de correo.
/// Soporta dos modos: envío directo (To/Cc/Bcc) o envío por grupo (Group).
/// </summary>
public class SendEmailRequestDto
{
    /// <summary>
    /// Opción 1: Envío directo - Destinatario principal
    /// </summary>
    public string? To { get; init; }
    
    /// <summary>
    /// Copia (visible para todos los destinatarios)
    /// </summary>
    public string[]? Cc { get; init; }
    
    /// <summary>
    /// Copia oculta (no visible para otros)
    /// </summary>
    public string[]? Bcc { get; init; }

    /// <summary>
    /// Opción 2: Envío por grupo (resuelve destinatarios desde MongoDB)
    /// </summary>
    public string? Group { get; init; }

    /// <summary>
    /// Asunto del correo
    /// </summary>
    public required string Subject { get; init; }
    
    /// <summary>
    /// Contenido HTML específico que se incrusta en el layout
    /// </summary>
    public required string HtmlBody { get; init; }
    
    /// <summary>
    /// Adjuntos opcionales
    /// </summary>
    public List<AttachmentDto>? Attachments { get; init; }
    
    /// <summary>
    /// Alternativa para idempotencia si no puedes enviar header. 
    /// Preferimos header "Idempotency-Key".
    /// </summary>
    public string? MetaIdempotencyKey { get; init; }
}

/// <summary>
/// Descripción de un adjunto en base64
/// </summary>
public class AttachmentDto
{
    /// <summary>
    /// Nombre del archivo
    /// </summary>
    public required string FileName { get; init; }
    
    /// <summary>
    /// Contenido del archivo codificado en base64
    /// </summary>
    public required string ContentBase64 { get; init; }
    
    /// <summary>
    /// Tipo MIME del archivo
    /// </summary>
    public required string ContentType { get; init; }
}