using SensorService.Domain.Entities;
using SensorService.Domain.ValueObjects;

namespace SensorService.Domain.Interfaces;

/// <summary>
/// Servicio de dominio que gestiona el estado de los nodos ESP32,
/// incluyendo detección de nodos offline y gestión de alertas de desconexión.
/// </summary>
public interface IEsp32StatusService
{
    /// <summary>
    /// Obtiene el estado actual de todos los nodos ESP32 registrados,
    /// evaluando si están offline según el umbral configurado.
    /// </summary>
    /// <param name="currentTime">Fecha y hora actual para la evaluación.</param>
    /// <param name="threshold">Umbral de desconexión configurado.</param>
    /// <returns>Colección de estados de todos los nodos ESP32.</returns>
    Task<IEnumerable<Esp32StatusRecord>> GetAllEsp32StatusesAsync(
        DateTime currentTime,
        OfflineThreshold threshold);

    /// <summary>
    /// Crea o actualiza una alerta de desconexión para un nodo ESP32 offline.
    /// </summary>
    /// <param name="status">Estado actual del nodo ESP32.</param>
    /// <param name="timestamp">Momento de la detección de desconexión.</param>
    Task UpsertOfflineAlertAsync(
      Esp32StatusRecord status,
      DateTime timestamp);

    /// <summary>
    /// Resuelve la alerta de desconexión de un nodo ESP32 que se ha reconectado.
    /// </summary>
    /// <param name="esp32Id">Identificador del nodo ESP32 reconectado.</param>
    /// <param name="timestamp">Momento de la reconexión.</param>
    Task ResolveOfflineAlertAsync(string esp32Id, DateTime timestamp);

    /// <summary>
    /// Marca como reconocida la alerta de desconexión activa de un nodo ESP32.
    /// </summary>
    /// <param name="esp32Id">Identificador del nodo ESP32.</param>
    Task AcknowledgeOfflineAlertAsync(string esp32Id);
}
