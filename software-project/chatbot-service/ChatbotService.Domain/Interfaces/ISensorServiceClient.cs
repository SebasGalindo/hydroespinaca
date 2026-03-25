namespace ChatbotService.Domain.Interfaces;

/// <summary>
/// Cliente HTTP para consultar datos de sensores desde sensor-service.
/// Reemplaza las queries directas a MongoDB para respetar las fronteras de microservicios.
/// </summary>
public interface ISensorServiceClient
{
    /// <summary>
    /// Obtiene las últimas lecturas enriquecidas de todos los sensores.
    /// Consume: GET /api/readings/latest (sensor-service).
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Resumen formateado de las lecturas para inyectar en el prompt del LLM.</returns>
    Task<string> GetLatestReadingsSummaryAsync(CancellationToken ct);
}
