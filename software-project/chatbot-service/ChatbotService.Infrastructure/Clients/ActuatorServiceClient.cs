using System.Net.Http.Headers;
using System.Text.Json;
using ChatbotService.Domain.Interfaces;
using HydroEspinaca.Shared.Authentication.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatbotService.Infrastructure.Clients;

/// <summary>
/// Cliente HTTP tipado para actuator-service.
/// Consume GET /api/actuators/states y formatea el resultado como texto para el LLM.
/// Usa M2M auth para autenticación entre servicios.
/// </summary>
public class ActuatorServiceClient : IActuatorServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ActuatorServiceClient> _logger;
    private readonly string _baseUrl;

    public ActuatorServiceClient(
        HttpClient httpClient,
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration,
        ILogger<ActuatorServiceClient> logger)
    {
        _httpClient = httpClient;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _baseUrl = configuration["Services:ActuatorService:Url"]
            ?? "http://actuator-service:8080";
    }

    /// <inheritdoc />
    public async Task<string> GetActuatorStatesSummaryAsync(CancellationToken ct)
    {
        try
        {
            await ConfigureAuthAsync(ct);

            var url = $"{_baseUrl}/api/actuators/states";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("actuator-service retornó {StatusCode} para /api/actuators/states",
                    response.StatusCode);
                return "[Error al obtener estados de actuadores]";
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            return FormatStatesForLlm(content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error consultando actuator-service");
            return "[Error al obtener estados de actuadores]";
        }
    }

    /// <summary>
    /// Formatea la respuesta JSON de actuator-service como texto legible para el prompt del LLM.
    /// </summary>
    private string FormatStatesForLlm(string jsonContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            var lines = new List<string> { "⚙️ Estado actual de actuadores:" };

            // Respuesta esperada: { count: N, states: [ { actuatorId, state, mode, ... } ] }
            if (root.TryGetProperty("states", out var states) && states.ValueKind == JsonValueKind.Array)
            {
                foreach (var actuator in states.EnumerateArray())
                {
                    var id = actuator.TryGetProperty("actuatorId", out var aid) ? aid.GetString() : "N/A";
                    var state = actuator.TryGetProperty("state", out var s) ? s.GetString() : "desconocido";
                    var mode = actuator.TryGetProperty("mode", out var m) ? m.GetString() : "N/A";
                    var pin = actuator.TryGetProperty("pin", out var p) ? p.ToString() : "N/A";
                    var dutyCycle = actuator.TryGetProperty("dutyCycle", out var dc) ? dc.ToString() : null;
                    var lastUpdated = actuator.TryGetProperty("lastUpdated", out var lu) ? lu.GetString() : "";

                    var detail = $"- {id}: estado={state}, modo={mode}, pin={pin}";
                    if (!string.IsNullOrEmpty(dutyCycle) && dutyCycle != "null")
                        detail += $", dutyCycle={dutyCycle}";
                    if (!string.IsNullOrEmpty(lastUpdated))
                        detail += $", última actualización={lastUpdated}";

                    lines.Add(detail);
                }
            }

            var count = root.TryGetProperty("count", out var c) ? c.GetInt32() : 0;

            if (lines.Count == 1)
            {
                if (count == 0)
                    return "[Sin actuadores registrados]";

                // Fallback: JSON compacto
                lines.Add(jsonContent.Length > 2000 ? jsonContent[..2000] + "..." : jsonContent);
            }

            _logger.LogInformation("Contexto de actuadores: {Count} actuadores", count);
            return string.Join("\n", lines);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Error parseando respuesta de actuator-service, usando raw");
            return $"⚙️ Estado de actuadores (raw):\n{(jsonContent.Length > 2000 ? jsonContent[..2000] : jsonContent)}";
        }
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
