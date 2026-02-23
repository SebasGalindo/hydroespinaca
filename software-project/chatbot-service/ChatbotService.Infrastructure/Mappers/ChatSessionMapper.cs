using ChatbotService.Domain.Entities;
using ChatbotService.Infrastructure.Documents;
using HydroEspinaca.Shared.Mongo.Interfaces;

namespace ChatbotService.Infrastructure.Mappers;

/// <summary>
/// Mapper bidireccional entre <see cref="ChatSession"/> (dominio) y <see cref="ChatSessionDocument"/> (MongoDB).
/// </summary>
public class ChatSessionMapper : IEntityMapper<ChatSession, ChatSessionDocument>
{
    /// <inheritdoc />
    public ChatSession ToEntity(ChatSessionDocument doc)
    {
        var entity = new ChatSession
        {
            UserId = doc.UserId,
            Title = doc.Title,
            Messages = doc.Messages.Select(m => new ChatMessage
            {
                Role = m.Role,
                Content = m.Content,
                Timestamp = m.Timestamp,
                TokensUsed = m.TokensUsed
            }).ToList(),
            IsArchived = doc.IsArchived,
            CreatedAt = doc.CreatedAt,
            UpdatedAt = doc.UpdatedAt
        };
        entity.SetId(doc.Id);
        return entity;
    }

    /// <inheritdoc />
    public ChatSessionDocument ToDocument(ChatSession entity)
    {
        var doc = new ChatSessionDocument
        {
            UserId = entity.UserId,
            Title = entity.Title,
            Messages = entity.Messages.Select(m => new ChatMessageDocument
            {
                Role = m.Role,
                Content = m.Content,
                Timestamp = m.Timestamp,
                TokensUsed = m.TokensUsed
            }).ToList(),
            IsArchived = entity.IsArchived,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
        doc.SetId(entity.Id);
        return doc;
    }
}
