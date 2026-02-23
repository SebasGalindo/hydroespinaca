using ChatbotService.Domain.Entities;

namespace ChatbotService.Domain.Interfaces;

/// <summary>
/// Proporciona métodos para búsqueda vectorial estricta con distancias (cosine similarity).
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Búsqueda semántica usando $vectorSearch contra la base de datos de MongoDB Atlas.
    /// </summary>
    /// <param name="queryEmbedding">Vector de 768 dimensiones como input del cliente (RetrievalQuery).</param>
    /// <param name="topK">Límite de la profundidad de búsqueda (p.ej. los top 5 mejores matches).</param>
    /// <param name="sourceTypeFilter">Filtro adicional al motor (ej. system_manual). BsonDocument parameterization opcional.</param>
    /// <param name="ct">Token cancel.</param>
    /// <returns>Reglas listadas más relativas al vector.</returns>
    Task<List<KnowledgeChunk>> SearchSimilarAsync(float[] queryEmbedding, int topK, string? sourceTypeFilter, CancellationToken ct);
}
