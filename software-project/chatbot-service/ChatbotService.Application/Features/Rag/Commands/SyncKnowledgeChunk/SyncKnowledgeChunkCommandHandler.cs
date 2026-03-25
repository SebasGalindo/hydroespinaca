using ChatbotService.Domain.Interfaces;
using ChatbotService.Application.Features.Rag.Services;
using ChatbotService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatbotService.Application.Features.Rag.Commands.SyncKnowledgeChunk;

/// <summary>
/// Handler que re-vectoriza o elimina un knowledge chunk en respuesta a
/// cambios en entidades del fuzzy-service (vectorización reactiva).
/// Consume fuzzy-service vía HTTP client e hidrata las entidades para
/// generar descripciones ricas en lenguaje natural.
/// </summary>
public class SyncKnowledgeChunkCommandHandler(
    IKnowledgeChunkRepository chunkRepository,
    IEmbeddingProvider embeddingProvider,
    IFuzzyServiceClient fuzzyServiceClient,
    IFuzzyEntityHydratorService hydrator,
    ContentSerializerService contentSerializer,
    ILogger<SyncKnowledgeChunkCommandHandler> logger)
    : IRequestHandler<SyncKnowledgeChunkCommand, bool>
{
    /// <inheritdoc />
    public async Task<bool> Handle(SyncKnowledgeChunkCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sincronizando knowledge chunk: {Action} {SourceType}/{SourceId}",
            request.Action, request.SourceType, request.SourceId);

        var sourceType = request.SourceType;
        var sourceId = request.SourceId;

        // Si es un término fuzzy, necesitamos actualizar su variable padre en su lugar
        // ya que los términos ahora se consolidan dentro de los chunks de las variables.
        if (sourceType == "fuzzy_term")
        {
            var termJson = await fuzzyServiceClient.GetEntityByIdAsync("fuzzy_term", sourceId, cancellationToken);
            if (termJson != null && termJson.Value.TryGetProperty("variable_id", out var varIdProp))
            {
                sourceType = "fuzzy_variable";
                sourceId = varIdProp.GetString() ?? "";
                logger.LogInformation("Término interceptado. Redirigiendo sincronización a la variable padre: {VariableId}", sourceId);
            }
            else
            {
                // Si el término ya fue eliminado o no tiene variable, no podemos actualizar el padre de forma reactiva.
                // Reindexación completa corregirá esto eventualmente.
                logger.LogWarning("No se pudo obtener el variable_id para el término {TermId}. Se omite la actualización.", sourceId);
                return true;
            }
        }

        if (request.Action.Equals("delete", StringComparison.OrdinalIgnoreCase))
        {
            return await chunkRepository.DeleteBySourceAsync(sourceId, sourceType, cancellationToken);
        }

        // Action = upsert: obtener la entidad hidratada y generar el contenido natural
        string content = string.Empty;

        try
        {
            if (sourceType == "fuzzy_system")
            {
                var sys = await hydrator.GetHydratedSystemAsync(sourceId, cancellationToken);
                if (sys == null) return false;
                content = contentSerializer.SerializeSystem(sys);
            }
            else if (sourceType == "fuzzy_variable")
            {
                var variable = await hydrator.GetHydratedVariableAsync(sourceId, cancellationToken);
                if (variable == null) return false;
                content = contentSerializer.SerializeVariable(variable);
            }
            else if (sourceType == "fuzzy_rule")
            {
                var rule = await hydrator.GetHydratedRuleAsync(sourceId, cancellationToken);
                if (rule == null) return false;
                content = contentSerializer.SerializeRule(rule);
            }
            else
            {
                logger.LogWarning("SourceType {SourceType} no soportado para hidratación.", sourceType);
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al hidratar o serializar la entidad {SourceType}/{SourceId}", sourceType, sourceId);
            return false;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            logger.LogWarning("El contenido generado está vacío para {SourceType}/{SourceId}", sourceType, sourceId);
            return false;
        }

        // Generar embedding
        var embedding = await embeddingProvider.GenerateEmbeddingAsync(
            content, EmbeddingTaskType.RetrievalDocument, cancellationToken);

        // Verificar si ya existe para incrementar versión
        var existing = await chunkRepository.GetBySourceAsync(sourceId, sourceType, cancellationToken);

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

        await chunkRepository.UpsertAsync(chunk, cancellationToken);

        logger.LogInformation("Knowledge chunk sincronizado: {SourceType}/{SourceId} v{Version}",
            sourceType, sourceId, chunk.Version);

        return true;
    }
}
