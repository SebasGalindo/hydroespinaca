using ChatbotService.Domain.Interfaces;
using ChatbotService.Infrastructure.Configuration;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatbotService.Infrastructure.Providers;

/// <summary>
/// Proveedor de embeddings vectoriales vía Google Gemini <c>gemini-embedding-001</c>.
/// Implementa <see cref="IEmbeddingProvider"/> con cache in-memory para queries repetidos.
/// </summary>
public class GeminiEmbeddingProvider : IEmbeddingProvider
{
    private readonly Client _client;
    private readonly GeminiSettings _settings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GeminiEmbeddingProvider> _logger;

    /// <summary>
    /// Duración del cache de embeddings repetidos (5 minutos).
    /// </summary>
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Inicializa el proveedor de embeddings.
    /// </summary>
    /// <param name="geminiSettings">Configuración del SDK de Gemini.</param>
    /// <param name="cache">Cache in-memory para embeddings repetidos.</param>
    /// <param name="logger">Logger.</param>
    public GeminiEmbeddingProvider(
        IOptions<GeminiSettings> geminiSettings,
        IMemoryCache cache,
        ILogger<GeminiEmbeddingProvider> logger)
    {
        _settings = geminiSettings.Value;
        _cache = cache;
        _logger = logger;
        _client = new Client(apiKey: _settings.ApiKey);
    }

    /// <inheritdoc />
    public async Task<float[]> GenerateEmbeddingAsync(string text, EmbeddingTaskType taskType, CancellationToken ct)
    {
        // Usamos un cache simple para evitar llamadas repetidas a la API de Gemini con el mismo texto y tipo de tarea.
        var cacheKey = $"emb:{taskType}:{text.GetHashCode()}";

        // Intentamos recuperar del cache antes de llamar a Gemini
        if (_cache.TryGetValue(cacheKey, out float[]? cached) && cached != null)
        {
            _logger.LogDebug("Embedding recuperado del cache para taskType={TaskType}", taskType);
            return cached;
        }

        _logger.LogInformation("Generando embedding con {Model}, taskType={TaskType}, {Chars} chars",
            _settings.EmbeddingModel, taskType, text.Length);

        // Configuramos la solicitud de embedding con el tipo de tarea y dimensionalidad
        var config = new EmbedContentConfig
        {
            TaskType = MapTaskType(taskType),
            OutputDimensionality = _settings.EmbeddingDimensions
        };

        // Llamamos a la API de Gemini para generar el embedding
        var result = await _client.Models.EmbedContentAsync(
            model: _settings.EmbeddingModel,
            contents: text,
            config: config);

        // Extraemos el embedding del resultado y lo convertimos a float[]
        var embedding = result?.Embeddings?.FirstOrDefault()?.Values?.Select(v => (float)v).ToArray()
            ?? throw new InvalidOperationException("Gemini API retornó un embedding vacío.");

        _logger.LogInformation("Embedding generado: {Dims} dimensiones", embedding.Length);

        // Guardamos el embedding en el cache para futuras consultas
        _cache.Set(cacheKey, embedding, CacheDuration);
        return embedding;
    }

    /// <summary>
    /// Mapea el enum interno <see cref="EmbeddingTaskType"/> al enum de Google GenAI.
    /// </summary>
    /// <param name="taskType">Tipo de tarea de embedding.</param>
    /// <returns>Task type de Google GenAI.</returns>
    private static string MapTaskType(EmbeddingTaskType taskType) => taskType switch
    {
        EmbeddingTaskType.RetrievalQuery => "RETRIEVAL_QUERY",
        EmbeddingTaskType.RetrievalDocument => "RETRIEVAL_DOCUMENT",
        _ => "RETRIEVAL_QUERY"
    };
}
