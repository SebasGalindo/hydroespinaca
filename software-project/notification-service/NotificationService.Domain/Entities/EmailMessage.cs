namespace NotificationService.Domain.Entities;

// Entidad de dominio que representa el mensaje de correo a enviar
public class EmailMessage
{
    // Identificador de correlación para trazabilidad
    public required string CorrelationId { get; init; }
    // Clave de idempotencia usada para evitar duplicados
    public required string IdempotencyKey { get; init; }
    public required string[] To { get; init; }
    public string[] Cc { get; init; } = Array.Empty<string>();
    public string[] Bcc { get; init; } = Array.Empty<string>();
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
    public required string TemplateKey { get; init; }
    public List<EmailAttachment> Attachments { get; init; } = new();
}

// Descripción de un adjunto
public class EmailAttachment
{
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required string ContentBase64 { get; init; }
}

// Resultado del envío con metadatos del proveedor
public record EmailSendResult(bool Success, string? Provider, string? ProviderMessageId, string? Error);
