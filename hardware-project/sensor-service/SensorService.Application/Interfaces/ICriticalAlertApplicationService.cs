using SensorService.Domain.Entities;

namespace SensorService.Application.Interfaces;

/// <summary>
/// Contrato del servicio de alertas críticas de la capa de aplicación.
/// Orquesta la evaluación y notificación de alertas críticas para variables de regulación manual (pH, EC, nivel de agua).
/// </summary>
public interface ICriticalAlertApplicationService
{
    /// <summary>
    /// Procesa las alertas críticas para un lote de lecturas de un ESP32.
    /// Evalúa las lecturas contra los rangos críticos y envía notificaciones si corresponde.
    /// </summary>
    /// <param name="esp32Id">Identificador del dispositivo ESP32 que envió las lecturas.</param>
    /// <param name="timestamp">Marca de tiempo del lote de lecturas.</param>
    /// <param name="readings">Colección de lecturas a evaluar.</param>
    /// <param name="cancellationToken">Token de cancelación para la operación asíncrona.</param>
    Task ProcessCriticalAlertsAsync(string esp32Id, DateTime timestamp, IEnumerable<Reading> readings, CancellationToken cancellationToken = default);
}