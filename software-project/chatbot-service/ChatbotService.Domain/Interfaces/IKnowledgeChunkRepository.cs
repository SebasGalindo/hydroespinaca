using ChatbotService.Domain.Entities;

namespace ChatbotService.Domain.Interfaces;

/// <summary>
/// Provee accesos CRUD e indexing para la colección <c>knowledge_chunks</c>.
/// </summary>
public interface IKnowledgeChunkRepository
{
    /// <summary>
    /// Busca un chunk por su fuente original (sourceId + sourceType forman clave compuesta).
    /// </summary>
    /// <param name="sourceId">ID de la entidad origen.</param>
    /// <param name="sourceType">Tipo de fuente (fuzzy_rule, fuzzy_system, system_manual).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El chunk encontrado o <c>null</c>.</returns>
    Task<KnowledgeChunk?> GetBySourceAsync(string sourceId, string sourceType, CancellationToken ct);

    /// <summary>
    /// Crea o actualiza (upsert) un chunk en la colección.
    /// </summary>
    /// <param name="chunk">El chunk a persistir.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task UpsertAsync(KnowledgeChunk chunk, CancellationToken ct);

    /// <summary>
    /// Elimina un chunk por su fuente original.
    /// </summary>
    /// <param name="sourceId">ID de la entidad origen.</param>
    /// <param name="sourceType">Tipo de fuente.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns><c>true</c> si se eliminó correctamente.</returns>
    Task<bool> DeleteBySourceAsync(string sourceId, string sourceType, CancellationToken ct);

    /// <summary>
    /// Obtiene todos los chunks de un tipo de fuente específico.
    /// </summary>
    /// <param name="sourceType">Tipo de fuente a filtrar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de chunks del tipo indicado.</returns>
    Task<List<KnowledgeChunk>> GetAllBySourceTypeAsync(string sourceType, CancellationToken ct);

    /// <summary>
    /// Obtiene todos los chunks almacenados (para re-indexación).
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista completa de chunks.</returns>
    Task<List<KnowledgeChunk>> GetAllAsync(CancellationToken ct);
}
