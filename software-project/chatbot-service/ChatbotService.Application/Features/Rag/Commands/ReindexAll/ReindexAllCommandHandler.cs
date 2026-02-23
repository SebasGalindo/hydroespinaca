using ChatbotService.Domain.Interfaces;
using ChatbotService.Application.Features.Rag.Services;
using ChatbotService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ChatbotService.Application.Features.Rag.Commands.ReindexAll;

/// <summary>
/// Handler que re-vectoriza toda la base de conocimientos.
/// Consume fuzzy-service vía HTTP client para obtener todas las entidades.
/// Uso administrativo/manual.
/// </summary>
public class ReindexAllCommandHandler(
    IKnowledgeChunkRepository chunkRepository,
    IEmbeddingProvider embeddingProvider,
    IFuzzyServiceClient fuzzyServiceClient,
    ContentSerializerService contentSerializer,
    ILogger<ReindexAllCommandHandler> logger)
    : IRequestHandler<ReindexAllCommand, int>
{
    /// <inheritdoc />
    public async Task<int> Handle(ReindexAllCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Iniciando re-indexación completa de la base de conocimientos");
        var totalIndexed = 0;

        // Re-indexar reglas fuzzy
        totalIndexed += await ReindexEntitiesAsync("fuzzy_rule", cancellationToken);

        // Re-indexar sistemas fuzzy
        totalIndexed += await ReindexEntitiesAsync("fuzzy_system", cancellationToken);

        // Re-indexar variables fuzzy
        totalIndexed += await ReindexEntitiesAsync("fuzzy_variable", cancellationToken);

        // Re-indexar términos fuzzy
        totalIndexed += await ReindexEntitiesAsync("fuzzy_term", cancellationToken);

        logger.LogInformation("Re-indexación completa: {Total} chunks indexados", totalIndexed);
        return totalIndexed;
    }

    /// <summary>
    /// Re-indexa todas las entidades de un tipo dado obtenidas vía HTTP desde fuzzy-service.
    /// </summary>
    /// <param name="sourceType">Tipo de fuente: fuzzy_rule, fuzzy_system, fuzzy_variable, fuzzy_term.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Cantidad de entidades indexadas.</returns>
    private async Task<int> ReindexEntitiesAsync(string sourceType, CancellationToken ct)
    {
        var entities = await fuzzyServiceClient.GetAllEntitiesByTypeAsync(sourceType, ct);
        var count = 0;

        logger.LogInformation("Re-indexando {SourceType}: {Count} entidades", sourceType, entities.Count);

        foreach (var entity in entities)
        {
            try
            {
                var sourceId = ExtractId(entity);
                if (string.IsNullOrEmpty(sourceId))
                {
                    logger.LogWarning("Entidad de tipo {SourceType} sin ID, omitiendo", sourceType);
                    continue;
                }

                var content = contentSerializer.Serialize(sourceType, entity);

                var embedding = await embeddingProvider.GenerateEmbeddingAsync(
                    content, EmbeddingTaskType.RetrievalDocument, ct);

                var existing = await chunkRepository.GetBySourceAsync(sourceId, sourceType, ct);

                var chunk = new KnowledgeChunk
                {
                    SourceType = sourceType,
                    SourceId = sourceId,
                    Content = content,
                    Embedding = embedding,
                    Version = (existing?.Version ?? 0) + 1,
                    CreatedAt = existing?.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                if (!string.IsNullOrEmpty(existing?.Id))
                    chunk.SetId(existing.Id);

                await chunkRepository.UpsertAsync(chunk, ct);
                count++;

                // Rate limiting: breve pausa para no saturar la API de embeddings
                await Task.Delay(100, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error indexando entidad de tipo {SourceType}", sourceType);
            }
        }

        logger.LogInformation("{SourceType} re-indexado: {Count}/{Total} entidades",
            sourceType, count, entities.Count);
        return count;
    }

    /// <summary>
    /// Extrae el ID de una entidad JSON (campo "id" o "_id").
    /// </summary>
    private static string? ExtractId(JsonElement entity)
    {
        if (entity.TryGetProperty("id", out var id))
            return id.GetString();
        if (entity.TryGetProperty("_id", out var mongoId))
            return mongoId.GetString();
        return null;
    }
}
