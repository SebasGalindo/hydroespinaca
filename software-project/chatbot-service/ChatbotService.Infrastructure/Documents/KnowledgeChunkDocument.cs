 using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatbotService.Infrastructure.Documents;

/// <summary>
/// Documento MongoDB para la colección <c>knowledge_chunks</c>.
/// Contiene el vector de embeddings utilizado por Atlas Vector Search.
/// </summary>
[BsonIgnoreExtraElements]
public class KnowledgeChunkDocument : IIdentifiableMutable
{
    /// <summary>
    /// ID nativo de MongoDB (ObjectId).
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfDefault]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Tipo de fuente: fuzzy_rule, fuzzy_system, fuzzy_variable, fuzzy_term, system_manual.
    /// </summary>
    [BsonElement("source_type")]
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// ID de la entidad original (clave compuesta con source_type).
    /// </summary>
    [BsonElement("source_id")]
    public string SourceId { get; set; } = string.Empty;

    /// <summary>
    /// Texto descriptivo legible que fue vectorizado.
    /// </summary>
    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Vector de embeddings (768 dimensiones por defecto, gemini-embedding-001).
    /// </summary>
    [BsonElement("embedding")]
    public float[] Embedding { get; set; } = [];

    /// <summary>
    /// Metadatos flexibles para filtrado adicional.
    /// </summary>
    [BsonElement("metadata")]
    [BsonIgnoreIfNull]
    public BsonDocument? Metadata { get; set; }

    /// <summary>
    /// Versión del chunk (incrementa en cada re-vectorización).
    /// </summary>
    [BsonElement("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Fecha de creación.
    /// </summary>
    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha de última re-vectorización.
    /// </summary>
    [BsonElement("updated_at")]
    [BsonIgnoreIfNull]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Puntuación calculada por Atlas Vector Search.
    /// Se ignora al insertar/actualizar.
    /// </summary>
    [BsonElement("score")]
    [BsonIgnoreIfNull]
    public double? Score { get; set; }

    /// <inheritdoc />
    public void SetId(string id) => Id = id;
}
