namespace ChatbotService.Domain.Interfaces;

/// <summary>
/// Extrae la información contextual dinámica (el "tiempo real" no vectorizado).
/// Consulta métricas y KPIs de la base para inyectarlos sin gastos extras de embeddings.
/// </summary>
public interface ILiveContextProvider
{
    /// <summary>
    /// Consulta el promedio, max, y min de una variable de "sensor_readings" en base al tiempo.
    /// Funciona usando un Aggregation Pipeline hacia la colección que le concierne a 'sensor-service'.
    /// </summary>
    /// <param name="lastHours">Rango horario por default a buscar. P. ej. 24 u 11.</param>
    /// <param name="ct">Token cancel.</param>
    Task<string> GetSensorReadingsSummaryAsync(int lastHours, CancellationToken ct);

    /// <summary>
    /// Devuelve el resultado del logger fuzzy en 'fuzzy_evaluations' sobre disparadores producidos recientemente.
    /// </summary>
    /// <param name="lastHours">Rango a evaluar.</param>
    /// <param name="ct">Token cancel.</param>
    Task<string> GetRecentEvaluationsSummaryAsync(int lastHours, CancellationToken ct);

    /// <summary>
    /// Realiza query directamente sobre 'actuator_commands' evaluando que dispositivos se encuentran encendidos.
    /// </summary>
    /// <param name="ct">Token cancel.</param>
    Task<string> GetActuatorStatesSummaryAsync(CancellationToken ct);
}
