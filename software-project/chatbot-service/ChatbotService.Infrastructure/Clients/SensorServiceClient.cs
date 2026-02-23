using System.Net.Http.Headers;
using System.Text.Json;
using ChatbotService.Domain.Interfaces;
using HydroEspinaca.Shared.Authentication.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatbotService.Infrastructure.Clients;

/// <summary>
/// Cliente HTTP tipado para sensor-service.
/// Consume GET /api/readings/latest y formatea el resultado como texto para el LLM.
/// Usa M2M auth para autenticación entre servicios.
/// </summary>
public class SensorServiceClient : ISensorServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<SensorServiceClient> _logger;
    private readonly string _baseUrl;

    public SensorServiceClient(
        HttpClient httpClient,
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration,
        ILogger<SensorServiceClient> logger)
    {
        _httpClient = httpClient;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _baseUrl = configuration["Services:SensorService:Url"]
            ?? "http://sensor-service:8080";
    }

    /// <inheritdoc />
    public async Task<string> GetLatestReadingsSummaryAsync(CancellationToken ct)
    {
        try
        {
            await ConfigureAuthAsync(ct);

            var url = $"{_baseUrl}/api/readings/latest";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("sensor-service retornó {StatusCode} para /api/readings/latest",
                    response.StatusCode);
                return "[Error al obtener lecturas de sensores]";
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            return FormatReadingsForLlm(content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error consultando sensor-service");
            return "[Error al obtener lecturas de sensores]";
        }
    }

    /// <summary>
    /// Formatea la respuesta JSON de sensor-service como texto legible para el prompt del LLM.
    /// </summary>
    private string FormatReadingsForLlm(string jsonContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            var lines = new List<string> { "📊 Últimas lecturas de sensores:" };

            // La respuesta de /api/readings/latest es un objeto enriquecido.
            // Iteramos sobre las propiedades para extraer datos relevantes.
            if (root.ValueKind == JsonValueKind.Object)
            {
                // Si tiene un array "readings" o "sensors" o es directamente un array
                if (root.TryGetProperty("readings", out var readings) && readings.ValueKind == JsonValueKind.Array)
                {
                    FormatReadingsArray(readings, lines);
                }
                else if (root.TryGetProperty("sensors", out var sensors) && sensors.ValueKind == JsonValueKind.Array)
                {
                    FormatReadingsArray(sensors, lines);
                }
                else
                {
                    // Recorrer propiedades de primer nivel que contengan datos de lectura
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Object)
                        {
                            FormatSensorObject(prop.Name, prop.Value, lines);
                        }
                        else if (prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            FormatReadingsArray(prop.Value, lines);
                        }
                    }
                }
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                FormatReadingsArray(root, lines);
            }

            if (lines.Count == 1)
            {
                // No se logró parsear estructura, incluir JSON compacto como fallback
                lines.Add(jsonContent.Length > 2000 ? jsonContent[..2000] + "..." : jsonContent);
            }

            _logger.LogInformation("Contexto de sensores formateado: {LineCount} líneas", lines.Count - 1);
            return string.Join("\n", lines);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Error parseando respuesta de sensor-service, usando raw");
            return $"📊 Últimas lecturas de sensores (raw):\n{(jsonContent.Length > 2000 ? jsonContent[..2000] : jsonContent)}";
        }
    }

    private static void FormatReadingsArray(JsonElement array, List<string> lines)
    {
        foreach (var item in array.EnumerateArray())
        {
            var variable = item.TryGetProperty("variable", out var v) ? v.GetString()
                         : item.TryGetProperty("variableName", out var vn) ? vn.GetString()
                         : item.TryGetProperty("name", out var n) ? n.GetString()
                         : "desconocida";

            var value = item.TryGetProperty("value", out var val) ? val.ToString()
                      : item.TryGetProperty("lastValue", out var lv) ? lv.ToString()
                      : "N/A";

            var unit = item.TryGetProperty("unit", out var u) ? u.GetString() ?? "" : "";

            var timestamp = item.TryGetProperty("timestamp", out var ts) ? ts.GetString() ?? ""
                          : item.TryGetProperty("readAt", out var ra) ? ra.GetString() ?? ""
                          : "";

            lines.Add($"- {variable}: {value}{unit} ({timestamp})");
        }
    }

    private static void FormatSensorObject(string name, JsonElement obj, List<string> lines)
    {
        var value = obj.TryGetProperty("value", out var v) ? v.ToString()
                  : obj.TryGetProperty("lastValue", out var lv) ? lv.ToString()
                  : "N/A";
        var unit = obj.TryGetProperty("unit", out var u) ? u.GetString() ?? "" : "";

        lines.Add($"- {name}: {value}{unit}");
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
