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
    IFuzzyEntityHydratorService hydrator,
    ContentSerializerService contentSerializer,
    ILogger<ReindexAllCommandHandler> logger)
    : IRequestHandler<ReindexAllCommand, int>
{
    /// <inheritdoc />
    public async Task<int> Handle(ReindexAllCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Iniciando re-indexación completa de la base de conocimientos con hidratación profunda");
        var totalIndexed = 0;

        // Re-indexar sistemas fuzzy
        totalIndexed += await ReindexEntitiesAsync("fuzzy_system", cancellationToken);

        // Re-indexar variables fuzzy (estos consolidarán automáticamente a sus términos)
        totalIndexed += await ReindexEntitiesAsync("fuzzy_variable", cancellationToken);

        // Re-indexar reglas fuzzy
        totalIndexed += await ReindexEntitiesAsync("fuzzy_rule", cancellationToken);

        // Los términos fuzzy ya no se indexan individualmente, por lo que los omitimos.
        logger.LogInformation("Re-indexación completa: {Total} chunks indexados (Términos omitidos al ser consolidados)", totalIndexed);
        
        return totalIndexed;
    }

    /// <summary>
    /// Re-indexa todas las entidades de un tipo dado obtenidas vía HTTP desde fuzzy-service,
    /// hidratándolas para formar descripciones en lenguaje natural.
    /// </summary>
    /// <param name="sourceType">Tipo de fuente: fuzzy_rule, fuzzy_system, fuzzy_variable.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Cantidad de entidades indexadas.</returns>
    private async Task<int> ReindexEntitiesAsync(string sourceType, CancellationToken ct)
    {
        var entities = await fuzzyServiceClient.GetAllEntitiesByTypeAsync(sourceType, ct);
        var count = 0;

        logger.LogInformation("Re-indexando {SourceType}: {Count} entidades", sourceType, entities.Count);

        foreach (var entity in entities)
        {
            var success = false;
            var attempts = 0;
            const int maxAttempts = 3;

            while (!success && attempts < maxAttempts)
            {
                attempts++;
                try
                {
                    var sourceId = ExtractId(entity);
                    if (string.IsNullOrEmpty(sourceId))
                    {
                        logger.LogWarning("Entidad de tipo {SourceType} sin ID, omitiendo", sourceType);
                        break; // Move to next entity
                    }

                    string content = string.Empty;
                    
                    if (sourceType == "fuzzy_system")
                    {
                        var sys = await hydrator.GetHydratedSystemAsync(sourceId, ct);
                        if (sys != null) content = contentSerializer.SerializeSystem(sys);
                    }
                    else if (sourceType == "fuzzy_variable")
                    {
                        var variable = await hydrator.GetHydratedVariableAsync(sourceId, ct);
                        if (variable != null) content = contentSerializer.SerializeVariable(variable);
                    }
                    else if (sourceType == "fuzzy_rule")
                    {
                        var rule = await hydrator.GetHydratedRuleAsync(sourceId, ct);
                        if (rule != null) content = contentSerializer.SerializeRule(rule);
                    }

                    if (string.IsNullOrWhiteSpace(content))
                    {
                        logger.LogWarning("Contenido vacío tras hidratar entidad {SourceType}/{SourceId}, omitiendo.", sourceType, sourceId);
                        break; // Move to next entity
                    }

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
                    success = true;

                    // Rate limiting: pausa para no saturar la API de embeddings (Free tier limit is low)
                    await Task.Delay(250, ct); 
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error indexando entidad de tipo {SourceType} (Intento {Attempt}/{MaxAttempts})", sourceType, attempts, maxAttempts);
                    if (attempts >= maxAttempts)
                    {
                        logger.LogError("Fallo definitivo indexando entidad de tipo {SourceType} despues de {MaxAttempts} intentos.", sourceType, maxAttempts);
                    }
                    else
                    {
                        // Esperar más tiempo si falló (ej. Quota exceeded)
                        await Task.Delay(2000 * attempts, ct);
                    }
                }
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
