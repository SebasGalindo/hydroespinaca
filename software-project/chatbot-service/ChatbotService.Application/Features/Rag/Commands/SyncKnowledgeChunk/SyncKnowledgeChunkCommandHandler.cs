using ChatbotService.Domain.Interfaces;
using ChatbotService.Application.Features.Rag.Services;
using ChatbotService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatbotService.Application.Features.Rag.Commands.SyncKnowledgeChunk;

/// <summary>
/// Handler que re-vectoriza o elimina un knowledge chunk en respuesta a
/// cambios en entidades del fuzzy-service (vectorización reactiva).
/// Consume fuzzy-service vía HTTP client, sin acceso directo a MongoDB.
/// </summary>
public class SyncKnowledgeChunkCommandHandler(
    IKnowledgeChunkRepository chunkRepository,
    IEmbeddingProvider embeddingProvider,
    IFuzzyServiceClient fuzzyServiceClient,
    ContentSerializerService contentSerializer,
    ILogger<SyncKnowledgeChunkCommandHandler> logger)
    : IRequestHandler<SyncKnowledgeChunkCommand, bool>
{
    /// <inheritdoc />
    public async Task<bool> Handle(SyncKnowledgeChunkCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sincronizando knowledge chunk: {Action} {SourceType}/{SourceId}",
            request.Action, request.SourceType, request.SourceId);

        if (request.Action.Equals("delete", StringComparison.OrdinalIgnoreCase))
        {
            return await chunkRepository.DeleteBySourceAsync(request.SourceId, request.SourceType, cancellationToken);
        }

        // Action = upsert: obtener la entidad del fuzzy-service vía HTTP y re-vectorizar
        var entity = await fuzzyServiceClient.GetEntityByIdAsync(request.SourceType, request.SourceId, cancellationToken);

        if (entity == null)
        {
            logger.LogWarning("Entidad {SourceType}/{SourceId} no encontrada en fuzzy-service",
                request.SourceType, request.SourceId);
            return false;
        }

        // Serializar la entidad JSON a texto descriptivo
        var content = contentSerializer.Serialize(request.SourceType, entity.Value);

        // Generar embedding (taskType = RETRIEVAL_DOCUMENT)
        var embedding = await embeddingProvider.GenerateEmbeddingAsync(
            content, EmbeddingTaskType.RetrievalDocument, cancellationToken);

        // Verificar si ya existe para incrementar versión
        var existing = await chunkRepository.GetBySourceAsync(request.SourceId, request.SourceType, cancellationToken);

        var chunk = new KnowledgeChunk
        {
            SourceType = request.SourceType,
            SourceId = request.SourceId,
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
            request.SourceType, request.SourceId, chunk.Version);

        return true;
    }
}
