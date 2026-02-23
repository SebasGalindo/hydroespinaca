using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatbotService.Domain.Entities;

/// <summary>
/// Fragmento de conocimiento vectorizado guardado en MongoDB y usado en Atlas Vector Search.
/// </summary>
public class KnowledgeChunk : IIdentifiableMutable
{
    /// <summary>
    /// ID nativo en MongoDB.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Fuente de donde se sacó el chunk: fuzzy_rule, fuzzy_system, system_manual.
    /// </summary>
    public required string SourceType { get; set; }

    /// <summary>
    /// Identificador compuesto (con SourceType) para referenciar el registro original en otra colección o el nombre del archivo manual.
    /// </summary>
    public required string SourceId { get; set; }

    /// <summary>
    /// Contenido de texto que detalla las características, formato legible generado que será indexado.
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Vector dimensional del contenido, generado por gemini-embedding-001 (recomendado: 768 dims).
    /// </summary>
    public required float[] Embedding { get; set; }

    /// <summary>
    /// Metadatos flexibles para filtros en query de búsqueda posterior (p.ej., systemName).
    /// </summary>
    [BsonIgnoreIfNull]
    public object? Metadata { get; set; }

    /// <summary>
    /// Versión/Revision de los datos vectorizados. Sube en cada re-vectorización.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Fecha de creación del indexamiento en la base.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha de la última re-vectorización.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Implementación de interfaz base.
    /// </summary>
    /// <param name="id">N/A se usa BSON id de Mongo.</param>
    public void SetId(string id) => Id = id;
}
