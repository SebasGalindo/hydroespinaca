namespace ChatbotService.Infrastructure.Configuration;

/// <summary>
/// Configuración del SDK de Google Gemini para generación de texto y embeddings.
/// Se mapea desde la sección "Gemini" de appsettings/env vars.
/// </summary>
public class GeminiSettings
{
    /// <summary>
    /// Nombre de la sección en la configuración.
    /// </summary>
    public const string SectionName = "Gemini";

    /// <summary>
    /// API Key de Google AI Studio para autenticación.
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// Modelo de generación de texto (ej. gemini-3-flash-preview).
    /// </summary>
    public string TextModel { get; set; } = "gemini-3-flash-preview";

    /// <summary>
    /// Modelo de embeddings (ej. gemini-embedding-001).
    /// </summary>
    public string EmbeddingModel { get; set; } = "gemini-embedding-001";

    /// <summary>
    /// Dimensionalidad del vector de embeddings (recomendado: 768).
    /// </summary>
    public int EmbeddingDimensions { get; set; } = 768;
}
