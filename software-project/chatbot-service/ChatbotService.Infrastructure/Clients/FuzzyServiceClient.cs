using System.Net.Http.Headers;
using System.Text.Json;
using ChatbotService.Domain.Interfaces;
using HydroEspinaca.Shared.Authentication.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatbotService.Infrastructure.Clients;

/// <summary>
/// Cliente HTTP tipado para fuzzy-service.
/// Consume endpoints de evaluaciones, entidades individuales y listados.
/// Usa M2M auth para autenticación entre servicios.
/// </summary>
public class FuzzyServiceClient : IFuzzyServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<FuzzyServiceClient> _logger;
    private readonly string _baseUrl;

    public FuzzyServiceClient(
        HttpClient httpClient,
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration,
        ILogger<FuzzyServiceClient> logger)
    {
        _httpClient = httpClient;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _baseUrl = configuration["Services:FuzzyService:Url"]
            ?? "http://fuzzy-service:8000";
    }

    /// <inheritdoc />
    public async Task<string> GetRecentEvaluationsSummaryAsync(int lastHours, CancellationToken ct)
    {
        try
        {
            await ConfigureAuthAsync(ct);

            var url = $"{_baseUrl}/api/fuzzy-evaluations/recent?hours={lastHours}";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("fuzzy-service retornó {StatusCode} para /api/fuzzy-evaluations/recent",
                    response.StatusCode);
                return "[Error al obtener evaluaciones fuzzy]";
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            return FormatEvaluationsForLlm(content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error consultando fuzzy-service");
            return "[Error al obtener evaluaciones fuzzy]";
        }
    }

    /// <inheritdoc />
    public async Task<JsonElement?> GetEntityByIdAsync(string sourceType, string sourceId, CancellationToken ct)
    {
        try
        {
            await ConfigureAuthAsync(ct);

            var path = GetEntityPath(sourceType);
            if (path == null)
            {
                _logger.LogWarning("Tipo de fuente no soportado para fetch: {SourceType}", sourceType);
                return null;
            }

            var url = $"{_baseUrl}{path}/{sourceId}";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("fuzzy-service retornó {StatusCode} para {Path}/{Id}",
                    response.StatusCode, path, sourceId);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.Clone();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error obteniendo entidad {SourceType}/{SourceId} de fuzzy-service",
                sourceType, sourceId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<JsonElement>> GetAllEntitiesByTypeAsync(string sourceType, CancellationToken ct)
    {
        var result = new List<JsonElement>();
        try
        {
            await ConfigureAuthAsync(ct);

            var path = GetEntityPath(sourceType);
            if (path == null)
            {
                _logger.LogWarning("Tipo de fuente no soportado para listado: {SourceType}", sourceType);
                return result;
            }

            var url = $"{_baseUrl}{path}";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("fuzzy-service retornó {StatusCode} para GET {Path}",
                    response.StatusCode, path);
                return result;
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // La respuesta puede ser un array directo o un objeto con propiedad que contiene el array
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                    result.Add(item.Clone());
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // Buscar la primera propiedad que sea un array (items, data, rules, etc.)
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in prop.Value.EnumerateArray())
                            result.Add(item.Clone());
                        break;
                    }
                }
            }

            _logger.LogInformation("Obtenidas {Count} entidades de tipo {SourceType} del fuzzy-service",
                result.Count, sourceType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error listando entidades {SourceType} de fuzzy-service", sourceType);
        }

        return result;
    }

    /// <summary>
    /// Mapea sourceType al path base del endpoint en fuzzy-service.
    /// </summary>
    private static string? GetEntityPath(string sourceType) => sourceType switch
    {
        "fuzzy_rule" => "/api/fuzzy-rules",
        "fuzzy_system" => "/api/fuzzy-systems",
        "fuzzy_variable" => "/api/fuzzy-variables",
        "fuzzy_term" => "/api/fuzzy-terms",
        _ => null
    };

    /// <summary>
    /// Formatea la respuesta JSON de fuzzy-service como texto legible para el prompt del LLM.
    /// </summary>
    private string FormatEvaluationsForLlm(string jsonContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            var lines = new List<string> { $"🧠 Evaluaciones fuzzy recientes:" };

            // La respuesta es un objeto con array "evaluations" o directamente un array
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("evaluations", out var evals) && evals.ValueKind == JsonValueKind.Array)
            {
                foreach (var eval in evals.EnumerateArray())
                {
                    FormatEvaluation(eval, lines);
                }
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var eval in root.EnumerateArray())
                {
                    FormatEvaluation(eval, lines);
                }
            }
            else
            {
                // Fallback: JSON compacto
                lines.Add(jsonContent.Length > 2000 ? jsonContent[..2000] + "..." : jsonContent);
            }

            if (lines.Count == 1)
                lines.Add("[Sin evaluaciones fuzzy recientes]");

            _logger.LogInformation("Contexto fuzzy: {Count} evaluaciones", lines.Count - 1);
            return string.Join("\n", lines);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Error parseando respuesta de fuzzy-service, usando raw");
            return $"🧠 Evaluaciones fuzzy recientes (raw):\n{(jsonContent.Length > 2000 ? jsonContent[..2000] : jsonContent)}";
        }
    }

    private static void FormatEvaluation(JsonElement eval, List<string> lines)
    {
        var systemName = eval.TryGetProperty("system_name", out var sn) ? sn.GetString() : "Desconocido";
        var evaluatedAt = eval.TryGetProperty("evaluated_at", out var ea) ? ea.GetString() : "N/A";
        var outputs = eval.TryGetProperty("outputs", out var outs) && outs.ValueKind == JsonValueKind.Object
            ? string.Join(", ", outs.EnumerateObject().Select(e => $"{e.Name}={e.Value}") )
            : "";

        lines.Add($"- [{evaluatedAt}] Sistema \"{systemName}\": {outputs}");
    }

    /// <summary>
    /// Configura el header de autorización M2M Bearer token.
    /// </summary>
    private async Task ConfigureAuthAsync(CancellationToken ct)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var m2mTokenService = scope.ServiceProvider.GetRequiredService<M2MTokenService>();
        var accessToken = await m2mTokenService.GetAccessTokenAsync(ct);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
    }
}
