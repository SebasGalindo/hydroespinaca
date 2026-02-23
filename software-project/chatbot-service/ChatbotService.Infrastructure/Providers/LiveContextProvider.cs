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
        // El endpoint de sensor-service solo soporta "latest", no por horas. Se ignora lastHours.
        _logger.LogInformation("Consultando resumen de sensores vía sensor-service HTTP client (lastHours={LastHours})", lastHours);
        return await _sensorServiceClient.GetLatestReadingsSummaryAsync(ct);
    }

    /// <inheritdoc />
    public async Task<string> GetRecentEvaluationsSummaryAsync(int lastHours, CancellationToken ct)
    {
        _logger.LogInformation("Consultando evaluaciones fuzzy vía fuzzy-service HTTP client (lastHours={LastHours})", lastHours);
        return await _fuzzyServiceClient.GetRecentEvaluationsSummaryAsync(lastHours, ct);
    }

    /// <inheritdoc />
    public async Task<string> GetActuatorStatesSummaryAsync(CancellationToken ct)
    {
        _logger.LogInformation("Consultando estados de actuadores vía actuator-service HTTP client");
        return await _actuatorServiceClient.GetActuatorStatesSummaryAsync(ct);
    }
}
