using HydroEspinaca.Shared.Abstractions;

namespace ChatbotService.Domain.Entities;

/// <summary>
/// Sesión de chat asociada a un usuario, mantiene el historial de la conversación.
/// </summary>
public class ChatSession : IIdentifiableMutable
{
    /// <summary>
    /// Identificador único (autogenerado por MongoDB o provisto como GUID string).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID del usuario dueño de la sesión.
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// Título auto-generado de la sesión basado en la primera pregunta.
    /// </summary>
    public string Title { get; set; } = "Nueva conversación";

    /// <summary>
    /// Lista de mensajes en la sesión.
    /// </summary>
    public List<ChatMessage> Messages { get; set; } = new();

    /// <summary>
    /// Indicador si la sesión ha sido archivada/borrada suavemente.
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Fecha de creación.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha de la última actualización (último mensaje enviado).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Fija el ID interno de la base de datos (requerido por IIdentifiableMutable).
    /// </summary>
    /// <param name="id">El identificador de BSON/String.</param>
    public void SetId(string id) => Id = id;

    /// <summary>
    /// Agrega un nuevo mensaje y actualiza la fecha.
    /// </summary>
    /// <param name="message">El mensaje a agregar.</param>
    public void AddMessage(ChatMessage message)
    {
        Messages.Add(message);
        UpdatedAt = DateTime.UtcNow;
    }
}
