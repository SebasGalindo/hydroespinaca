using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatbotService.Infrastructure.Documents;

/// <summary>
/// Documento MongoDB para la colección <c>chat_sessions</c>.
/// Mapea 1:1 con <see cref="Domain.Entities.ChatSession"/>.
/// </summary>
public class ChatSessionDocument : IIdentifiableMutable
{
    /// <summary>
    /// ID de la sesión (string UUID, no ObjectId).
    /// </summary>
    [BsonId]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID del usuario dueño de la sesión.
    /// </summary>
    [BsonElement("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Título auto-generado de la sesión.
    /// </summary>
    [BsonElement("title")]
    public string Title { get; set; } = "Nueva conversación";

    /// <summary>
    /// Lista de mensajes de la conversación.
    /// </summary>
    [BsonElement("messages")]
    public List<ChatMessageDocument> Messages { get; set; } = new();

    /// <summary>
    /// Si la sesión está archivada.
    /// </summary>
    [BsonElement("is_archived")]
    public bool IsArchived { get; set; }

    /// <summary>
    /// Fecha de creación.
    /// </summary>
    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha de última actualización.
    /// </summary>
    [BsonElement("updated_at")]
    [BsonIgnoreIfNull]
    public DateTime? UpdatedAt { get; set; }

    /// <inheritdoc />
    public void SetId(string id) => Id = id;
}

/// <summary>
/// Documento embebido para un mensaje individual de chat.
/// </summary>
public class ChatMessageDocument
{
    /// <summary>
    /// Rol del emisor: user, model, system.
    /// </summary>
    [BsonElement("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Contenido del mensaje (texto/Markdown).
    /// </summary>
    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora del mensaje.
    /// </summary>
    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tokens consumidos (solo para rol model).
    /// </summary>
    [BsonElement("tokens_used")]
    [BsonIgnoreIfNull]
    public int? TokensUsed { get; set; }
}
