using ChatbotService.Domain.Interfaces;
using ChatbotService.Domain.Entities;
using ChatbotService.Infrastructure.Documents;
using ChatbotService.Infrastructure.Mappers;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace ChatbotService.Infrastructure.Repositories;

/// <summary>
/// Repositorio MongoDB para la colección <c>knowledge_chunks</c>.
/// Implementa <see cref="IKnowledgeChunkRepository"/> con operaciones de upsert
/// para la vectorización reactiva.
/// </summary>
public class MongoKnowledgeChunkRepository : IKnowledgeChunkRepository
{
    private readonly IMongoCollection<KnowledgeChunkDocument> _collection;
    private readonly KnowledgeChunkMapper _mapper;
    private readonly ILogger<MongoKnowledgeChunkRepository> _logger;

    /// <summary>
    /// Inicializa el repositorio de knowledge chunks.
    /// </summary>
    /// <param name="database">Instancia de la base de datos MongoDB.</param>
    /// <param name="logger">Logger.</param>
    public MongoKnowledgeChunkRepository(IMongoDatabase database, ILogger<MongoKnowledgeChunkRepository> logger)
    {
        _collection = database.GetCollection<KnowledgeChunkDocument>("knowledge_chunks");
        _mapper = new KnowledgeChunkMapper();
        _logger = logger;

        CreateIndexes();
    }

    /// <inheritdoc />
    public async Task<KnowledgeChunk?> GetBySourceAsync(string sourceId, string sourceType, CancellationToken ct)
    {
        var filter = Builders<KnowledgeChunkDocument>.Filter.Eq(d => d.SourceId, sourceId)
                   & Builders<KnowledgeChunkDocument>.Filter.Eq(d => d.SourceType, sourceType);

        var doc = await _collection.Find(filter).FirstOrDefaultAsync(ct);
        return doc != null ? _mapper.ToEntity(doc) : null;
    }

    /// <inheritdoc />
    public async Task UpsertAsync(KnowledgeChunk chunk, CancellationToken ct)
    {
        var filter = Builders<KnowledgeChunkDocument>.Filter.Eq(d => d.SourceId, chunk.SourceId)
                   & Builders<KnowledgeChunkDocument>.Filter.Eq(d => d.SourceType, chunk.SourceType);

        var doc = _mapper.ToDocument(chunk);
        doc.UpdatedAt = DateTime.UtcNow;

        var options = new ReplaceOptions { IsUpsert = true };
        var result = await _collection.ReplaceOneAsync(filter, doc, options, ct);

        _logger.LogInformation("Knowledge chunk upsert: source={SourceType}/{SourceId}, matched={Matched}, modified={Modified}",
            chunk.SourceType, chunk.SourceId, result.MatchedCount, result.ModifiedCount);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteBySourceAsync(string sourceId, string sourceType, CancellationToken ct)
    {
        var filter = Builders<KnowledgeChunkDocument>.Filter.Eq(d => d.SourceId, sourceId)
                   & Builders<KnowledgeChunkDocument>.Filter.Eq(d => d.SourceType, sourceType);

        var result = await _collection.DeleteOneAsync(filter, ct);
        _logger.LogInformation("Knowledge chunk eliminado: source={SourceType}/{SourceId}, deleted={Deleted}",
            sourceType, sourceId, result.DeletedCount > 0);
        return result.DeletedCount > 0;
    }

    /// <inheritdoc />
    public async Task<List<KnowledgeChunk>> GetAllBySourceTypeAsync(string sourceType, CancellationToken ct)
    {
        var filter = Builders<KnowledgeChunkDocument>.Filter.Eq(d => d.SourceType, sourceType);
        var docs = await _collection.Find(filter).ToListAsync(ct);
        return docs.Select(d => _mapper.ToEntity(d)).ToList();
    }

    /// <inheritdoc />
    public async Task<List<KnowledgeChunk>> GetAllAsync(CancellationToken ct)
    {
        var docs = await _collection.Find(_ => true).ToListAsync(ct);
        return docs.Select(d => _mapper.ToEntity(d)).ToList();
    }

    /// <summary>
    /// Crea los índices necesarios para la colección <c>knowledge_chunks</c>.
    /// </summary>
    private void CreateIndexes()
    {
        try
        {
            // Índice compuesto único para source_id + source_type
            var compoundIndex = new CreateIndexModel<KnowledgeChunkDocument>(
                Builders<KnowledgeChunkDocument>.IndexKeys
                    .Ascending(d => d.SourceId)
                    .Ascending(d => d.SourceType),
                new CreateIndexOptions { Name = "idx_source_compound", Unique = true }
            );

            // Índice para filtrar por source_type
            var sourceTypeIndex = new CreateIndexModel<KnowledgeChunkDocument>(
                Builders<KnowledgeChunkDocument>.IndexKeys.Ascending(d => d.SourceType),
                new CreateIndexOptions { Name = "idx_source_type" }
            );

            _collection.Indexes.CreateMany([compoundIndex, sourceTypeIndex]);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error creando índices de knowledge_chunks (pueden ya existir)");
        }
    }
}
