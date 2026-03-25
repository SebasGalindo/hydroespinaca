using HydroEspinaca.Shared.Abstractions;

namespace ChatbotService.Domain.Entities;

/// <summary>
/// Mensaje individual dentro de una sesión de chat.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Rol de quien envió el mensaje (user, model, o system).
    /// </summary>
    public required string Role { get; set; }

    /// <summary>
    /// Contenido de texto del mensaje (puede ser Markdown).
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Fecha y hora del envío del mensaje.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Cantidad de tokens consumidos (solo aplicable a rol 'model').
    /// </summary>
    public int? TokensUsed { get; set; }
}
