namespace ChatbotService.Infrastructure.Configuration;

/// <summary>
/// Configuración de la estrategia RAG (Retrieval-Augmented Generation).
/// Se mapea desde la sección "Rag" de appsettings/env vars.
/// </summary>
public class RagSettings
{
    /// <summary>
    /// Nombre de la sección en la configuración.
    /// </summary>
    public const string SectionName = "Rag";

    /// <summary>
    /// Tokens máximos permitidos por respuesta del LLM.
    /// </summary>
    public int MaxTokensPerResponse { get; set; } = 2048;

    /// <summary>
    /// Temperatura de generación del LLM (0.0 = determinista, 1.0 = creativo).
    /// </summary>
    public float Temperature { get; set; } = 0.3f;

    /// <summary>
    /// Cantidad de chunks similares a recuperar en la búsqueda vectorial.
    /// </summary>
    public int TopKRetrieval { get; set; } = 5;

    /// <summary>
    /// Horas por defecto para consultar datos en vivo (sensores, evaluaciones).
    /// </summary>
    public int LiveContextDefaultHours { get; set; } = 24;

    /// <summary>
    /// Nombre del índice de Atlas Vector Search.
    /// </summary>
    public string VectorSearchIndexName { get; set; } = "knowledge_vector_index";

    /// <summary>
    /// Cantidad máxima de mensajes del historial a incluir en el prompt.
    /// </summary>
    public int MaxHistoryMessages { get; set; } = 20;
}
