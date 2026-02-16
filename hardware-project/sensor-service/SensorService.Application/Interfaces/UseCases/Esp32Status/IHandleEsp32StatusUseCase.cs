using SensorService.Application.DTOs.Esp32Status;

namespace SensorService.Application.Interfaces.UseCases.Esp32Status;

/// <summary>
/// Contrato del caso de uso para gestionar los cambios de estado de los dispositivos ESP32.
/// Maneja transiciones entre estados online, offline y running recibidos vía MQTT (LWT).
/// </summary>
public interface IHandleEsp32StatusUseCase
{
    /// <summary>
    /// Procesa la transición a estado online de un ESP32, resolviendo alertas activas si existen.
    /// </summary>
    /// <param name="esp32Id">Identificador del dispositivo ESP32.</param>
    /// <param name="timestamp">Marca de tiempo del evento de conexión.</param>
    /// <param name="freeHeap">Memoria heap libre en bytes (opcional).</param>
    /// <param name="uptime">Tiempo de actividad en segundos (opcional).</param>
    Task HandleOnlineAsync(string esp32Id, DateTime timestamp, long? freeHeap = null, long? uptime = null);

    /// <summary>
    /// Procesa la desconexión de un ESP32, creando una alerta de desconexión si no existe una activa.
    /// </summary>
    /// <param name="esp32Id">Identificador del dispositivo ESP32.</param>
    /// <param name="timestamp">Marca de tiempo del evento de desconexión.</param>
    Task HandleOfflineAsync(string esp32Id, DateTime timestamp);

    /// <summary>
    /// Procesa un payload completo de estado del ESP32 y delega al handler apropiado según el tipo de estado.
    /// </summary>
    /// <param name="payload">DTO con el payload completo de estado del ESP32.</param>
    Task HandleStatusPayloadAsync(Esp32StatusPayloadDto payload);
}