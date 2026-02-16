using SensorService.Domain.Entities;
using SensorService.Domain.ValueObjects;

namespace SensorService.Domain.Interfaces;

/// <summary>
/// Servicio de dominio que evalúa las lecturas críticas de variables de regulación manual
/// y recopila lecturas contextuales de variables automáticas para generar datos de alertas.
/// </summary>
public interface ICriticalReadingEvaluationService
{
    /// <summary>
    /// Evalúa las lecturas recibidas de un ESP32 contra las variables de regulación manual,
    /// determinando cuáles están fuera de rango y recopilando lecturas contextuales automáticas.
    /// </summary>
    /// <param name="esp32Id">Identificador del nodo ESP32 origen.</param>
    /// <param name="timestamp">Momento de la evaluación.</param>
    /// <param name="readings">Lecturas recibidas del ESP32.</param>
    /// <param name="sensors">Sensores registrados en el sistema.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Datos consolidados con lecturas críticas manuales y lecturas contextuales automáticas.</returns>
    Task<CriticalAlertData> EvaluateCriticalReadingsAsync(string esp32Id, DateTime timestamp, IEnumerable<Reading> readings, IEnumerable<Sensor> sensors, CancellationToken cancellationToken = default);
}