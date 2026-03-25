using ChatbotService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace ChatbotService.Infrastructure.Providers;

/// <summary>
/// Proveedor de contexto en vivo (datos dinámicos) que consulta los microservicios
/// vía HTTP clients con autenticación M2M. Solo operaciones de lectura (read-only).
/// Resuelve el problema de frescura de datos sin necesidad de vectorizar datos temporales.
/// </summary>
public class LiveContextProvider : ILiveContextProvider
{
    private readonly ISensorServiceClient _sensorServiceClient;
    private readonly IActuatorServiceClient _actuatorServiceClient;
    private readonly IFuzzyServiceClient _fuzzyServiceClient;
    private readonly ILogger<LiveContextProvider> _logger;

    /// <summary>
    /// Inicializa el proveedor de contexto en vivo usando clientes HTTP para cada microservicio.
    /// </summary>
    public LiveContextProvider(
        ISensorServiceClient sensorServiceClient,
        IActuatorServiceClient actuatorServiceClient,
        IFuzzyServiceClient fuzzyServiceClient,
        ILogger<LiveContextProvider> logger)
    {
        _sensorServiceClient = sensorServiceClient;
        _actuatorServiceClient = actuatorServiceClient;
        _fuzzyServiceClient = fuzzyServiceClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GetSensorReadingsSummaryAsync(int lastHours, CancellationToken ct)
    {
        _logger.LogDebug("[LiveContext] → Consultando sensor-service /api/readings/latest (lastHours={LastHours})", lastHours);
        try
        {
            var result = await _sensorServiceClient.GetLatestReadingsSummaryAsync(ct);
            _logger.LogDebug("[LiveContext] ← sensor-service respondió: {Len} chars", result?.Length ?? 0);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[LiveContext] ✗ Error consultando sensor-service");
            return "[Error al obtener lecturas de sensores]";
        }
    }

    /// <inheritdoc />
    public async Task<string> GetRecentEvaluationsSummaryAsync(int lastHours, CancellationToken ct)
    {
        _logger.LogDebug("[LiveContext] → Consultando fuzzy-service /api/fuzzy-evaluations/recent (lastHours={LastHours})", lastHours);
        try
        {
            var result = await _fuzzyServiceClient.GetRecentEvaluationsSummaryAsync(lastHours, ct);
            _logger.LogDebug("[LiveContext] ← fuzzy-service respondió: {Len} chars", result?.Length ?? 0);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[LiveContext] ✗ Error consultando fuzzy-service");
            return "[Error al obtener evaluaciones fuzzy]";
        }
    }

    /// <inheritdoc />
    public async Task<string> GetActuatorStatesSummaryAsync(CancellationToken ct)
    {
        _logger.LogDebug("[LiveContext] → Consultando actuator-service /api/actuators/states");
        try
        {
            var result = await _actuatorServiceClient.GetActuatorStatesSummaryAsync(ct);
            _logger.LogDebug("[LiveContext] ← actuator-service respondió: {Len} chars", result?.Length ?? 0);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[LiveContext] ✗ Error consultando actuator-service");
            return "[Error al obtener estados de actuadores]";
        }
    }
}
