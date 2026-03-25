namespace ChatbotService.Domain.Interfaces;

/// <summary>
/// Provee generación de vectores matemáticos consumiendo el API de embeddings.
/// </summary>
public interface IEmbeddingProvider
{
    /// <summary>
    /// Genera array float como vector.
    /// </summary>
    /// <param name="text">Texto legible o documento.</param>
    /// <param name="taskType">Indicador si busca QUERY o indexa DOCUMENT para mejor relevancia.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<float[]> GenerateEmbeddingAsync(string text, EmbeddingTaskType taskType, CancellationToken ct);
}

/// <summary>
/// Google API options mapping for specific TaskType configurations in GenAI C#.
/// </summary>
public enum EmbeddingTaskType
{
    RetrievalQuery,
    RetrievalDocument
}
