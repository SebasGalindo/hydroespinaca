namespace ChatbotService.Domain.Interfaces;

/// <summary>
/// Cliente HTTP para consultar estados de actuadores desde actuator-service.
/// Reemplaza las queries directas a MongoDB para respetar las fronteras de microservicios.
/// </summary>
public interface IActuatorServiceClient
{
    /// <summary>
    /// Obtiene el estado actual de todos los actuadores desde la state machine in-memory.
    /// Consume: GET /api/actuators/states (actuator-service).
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Resumen formateado de los estados para inyectar en el prompt del LLM.</returns>
    Task<string> GetActuatorStatesSummaryAsync(CancellationToken ct);
}
