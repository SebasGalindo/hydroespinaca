using System.Runtime.CompilerServices;
using ChatbotService.Domain.Interfaces;
using ChatbotService.Domain.Entities;
using ChatbotService.Infrastructure.Configuration;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatbotService.Infrastructure.Providers;

/// <summary>
/// Proveedor de generación de texto vía Google Gemini API con streaming.
/// Implementa <see cref="ILlmProvider"/> utilizando el SDK oficial <c>Google.GenAI</c>.
/// </summary>
public class GeminiLlmProvider : ILlmProvider
{
    private readonly Client _client;
    private readonly GeminiSettings _settings;
    private readonly RagSettings _ragSettings;
    private readonly ILogger<GeminiLlmProvider> _logger;

    /// <summary>
    /// Inicializa el proveedor con la configuración de Gemini.
    /// </summary>
    /// <param name="geminiSettings">Configuración del SDK de Gemini.</param>
    /// <param name="ragSettings">Configuración de parámetros RAG.</param>
    /// <param name="logger">Logger.</param>
    public GeminiLlmProvider(
        IOptions<GeminiSettings> geminiSettings,
        IOptions<RagSettings> ragSettings,
        ILogger<GeminiLlmProvider> logger)
    {
        _settings = geminiSettings.Value;
        _ragSettings = ragSettings.Value;
        _logger = logger;

        _client = new Client(apiKey: _settings.ApiKey);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string systemInstruction,
        List<ChatMessage> history,
        string userMessage,
        [EnumeratorCancellation] CancellationToken ct)
    {
        _logger.LogInformation("[Gemini] Iniciando generación streaming con modelo {Model}, historial={HistoryCount} msgs, instrucción={InstructionLen} chars",
            _settings.TextModel, history.Count, systemInstruction.Length);

        var contents = BuildContents(history, userMessage);

        var config = new GenerateContentConfig
        {
            SystemInstruction = new Content
            {
                Parts = [new Part { Text = systemInstruction }]
            },
            Temperature = _ragSettings.Temperature,
            MaxOutputTokens = _ragSettings.MaxTokensPerResponse
        };

        _logger.LogDebug("[Gemini] Config: temperature={Temp}, maxTokens={MaxTokens}",
            _ragSettings.Temperature, _ragSettings.MaxTokensPerResponse);

        var streamResponse = _client.Models.GenerateContentStreamAsync(
            _settings.TextModel, contents, config);

        var chunkCount = 0;
        await foreach (var chunk in streamResponse.WithCancellation(ct))
        {
            if (chunk.Candidates is { Count: > 0 } candidates)
            {
                var text = candidates[0].Content?.Parts?.FirstOrDefault()?.Text;
                if (!string.IsNullOrEmpty(text))
                {
                    chunkCount++;
                    if (chunkCount <= 3)
                        _logger.LogDebug("[Gemini] Chunk #{Num}: '{Text}'", chunkCount,
                            text.Length > 80 ? text[..80] + "..." : text);
                    yield return text;
                }
            }
        }

        _logger.LogInformation("[Gemini] Generación streaming completada: {ChunkCount} chunks emitidos", chunkCount);
    }

    /// <summary>
    /// Construye la lista de contenidos (historial + mensaje actual) para la API de Gemini.
    /// </summary>
    /// <param name="history">Historial de mensajes previos de la sesión.</param>
    /// <param name="userMessage">Mensaje actual del usuario.</param>
    /// <returns>Lista de <see cref="Content"/> formateados para Gemini.</returns>
    private static List<Content> BuildContents(List<ChatMessage> history, string userMessage)
    {
        var contents = new List<Content>();

        foreach (var msg in history)
        {
            var role = msg.Role switch
            {
                "user" => "user",
                "model" => "model",
                _ => "user"
            };

            contents.Add(new Content
            {
                Role = role,
                Parts = [new Part { Text = msg.Content }]
            });
        }

        // Agregar el mensaje actual del usuario
        contents.Add(new Content
        {
            Role = "user",
            Parts = [new Part { Text = userMessage }]
        });

        return contents;
    }
}
