namespace NotificationService.Application.DTOs;

// Contrato de entrada del endpoint de envío de correo.
// Campos opcionales (cc/bcc/attachments) para flexibilidad.
public class SendEmailRequestDto
{
    // Destinatario principal
    public required string To { get; init; }
    // Copia (visible para todos los destinatarios)
    public string[]? Cc { get; init; }
    // Copia oculta (no visible para otros)
    public string[]? Bcc { get; init; }
    // Asunto del correo
    public required string Subject { get; init; }
    // Contenido HTML específico que se incrusta en el layout
    public required string HtmlBody { get; init; }
    // Clave de plantilla para seleccionar el layout (p.ej. "default")
    public string TemplateKey { get; init; } = "default";
    // Adjuntos opcionales
    public List<AttachmentDto>? Attachments { get; init; }
    // Alternativa para idempotencia si no puedes enviar header. Preferimos header "Idempotency-Key".
    public string? MetaIdempotencyKey { get; init; }
}

// Descripción de un adjunto en base64
public class AttachmentDto
{
    public required string FileName { get; init; }
    public required string ContentBase64 { get; init; }
    public required string ContentType { get; init; }
}
