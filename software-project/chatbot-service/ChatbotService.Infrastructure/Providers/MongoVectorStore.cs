using ChatbotService.Domain.Interfaces;
using ChatbotService.Domain.Entities;
using ChatbotService.Infrastructure.Configuration;
using ChatbotService.Infrastructure.Documents;
using ChatbotService.Infrastructure.Mappers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ChatbotService.Infrastructure.Providers;

/// <summary>
/// Implementación de búsqueda vectorial utilizando MongoDB Atlas Vector Search.
/// Ejecuta <c>$vectorSearch</c> aggregation pipeline sobre la colección <c>knowledge_chunks</c>.
/// </summary>
public class MongoVectorStore : IVectorStore
{
    private readonly IMongoCollection<KnowledgeChunkDocument> _collection;
    private readonly KnowledgeChunkMapper _mapper;
    private readonly RagSettings _ragSettings;
    private readonly ILogger<MongoVectorStore> _logger;

    /// <summary>
    /// Inicializa el vector store con la conexión a MongoDB.
    /// </summary>
    /// <param name="database">Instancia de la base de datos MongoDB.</param>
    /// <param name="ragSettings">Configuración RAG (nombre del índice).</param>
    /// <param name="logger">Logger.</param>
    public MongoVectorStore(
        IMongoDatabase database,
        IOptions<RagSettings> ragSettings,
        ILogger<MongoVectorStore> logger)
    {
        _collection = database.GetCollection<KnowledgeChunkDocument>("knowledge_chunks");
        _mapper = new KnowledgeChunkMapper();
        _ragSettings = ragSettings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<KnowledgeChunk>> SearchSimilarAsync(
        float[] queryEmbedding, int topK, string? sourceTypeFilter, CancellationToken ct)
    {
        _logger.LogInformation("Ejecutando Atlas Vector Search: topK={TopK}, filtro={Filter}",
            topK, sourceTypeFilter ?? "ninguno");

        // Construimos el pipeline de agregación con $vectorSearch
        var vectorSearchStage = new BsonDocument("$vectorSearch", new BsonDocument
        {
            { "index", _ragSettings.VectorSearchIndexName },
            { "path", "embedding" },
            { "queryVector", new BsonArray(queryEmbedding.Select(f => (BsonValue)(double)f)) },
            { "numCandidates", topK * 10 },
            { "limit", topK }
        });

        // Agregar filtro de source_type si se especifica
        if (!string.IsNullOrEmpty(sourceTypeFilter))
        {
            var searchDoc = vectorSearchStage["$vectorSearch"].AsBsonDocument;
            searchDoc.Add("filter", new BsonDocument("source_type", sourceTypeFilter));
        }

        // Proyección para incluir el score de similitud
        var projectStage = new BsonDocument("$project", new BsonDocument
        {
            { "_id", 1 },
            { "source_type", 1 },
            { "source_id", 1 },
            { "content", 1 },
            { "embedding", 1 },
            { "metadata", 1 },
            { "version", 1 },
            { "created_at", 1 },
            { "updated_at", 1 },
            { "score", new BsonDocument("$meta", "vectorSearchScore") }
        });

        var pipeline = new[] { vectorSearchStage, projectStage };

        try
        {
            var documents = await _collection
                .Aggregate<KnowledgeChunkDocument>(pipeline, cancellationToken: ct)
                .ToListAsync(ct);

            _logger.LogInformation("Vector Search retornó {Count} resultados", documents.Count);

            return documents.Select(d => _mapper.ToEntity(d)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando Atlas Vector Search");
            return [];
        }
    }
}
