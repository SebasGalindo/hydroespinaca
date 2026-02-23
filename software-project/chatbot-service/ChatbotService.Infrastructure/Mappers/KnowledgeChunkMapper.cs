 using ChatbotService.Domain.Entities;
using ChatbotService.Infrastructure.Documents;
using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace ChatbotService.Infrastructure.Mappers;

/// <summary>
/// Mapper bidireccional entre <see cref="KnowledgeChunk"/> (dominio) y <see cref="KnowledgeChunkDocument"/> (MongoDB).
/// </summary>
public class KnowledgeChunkMapper : IEntityMapper<KnowledgeChunk, KnowledgeChunkDocument>
{
    /// <inheritdoc />
    public KnowledgeChunk ToEntity(KnowledgeChunkDocument doc)
    {
        var entity = new KnowledgeChunk
        {
            SourceType = doc.SourceType,
            SourceId = doc.SourceId,
            Content = doc.Content,
            Embedding = doc.Embedding,
            Metadata = doc.Metadata != null
                ? BsonSerializer.Deserialize<object>(doc.Metadata)
                : null,
            Version = doc.Version,
            CreatedAt = doc.CreatedAt,
            UpdatedAt = doc.UpdatedAt
        };
        entity.SetId(doc.Id);
        return entity;
    }

    /// <inheritdoc />
    public KnowledgeChunkDocument ToDocument(KnowledgeChunk entity)
    {
        var doc = new KnowledgeChunkDocument
        {
            SourceType = entity.SourceType,
            SourceId = entity.SourceId,
            Content = entity.Content,
            Embedding = entity.Embedding,
            Metadata = entity.Metadata != null
                ? entity.Metadata.ToBsonDocument()
                : null,
            Version = entity.Version,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
        if (!string.IsNullOrEmpty(entity.Id))
            doc.SetId(entity.Id);
        return doc;
    }
}
