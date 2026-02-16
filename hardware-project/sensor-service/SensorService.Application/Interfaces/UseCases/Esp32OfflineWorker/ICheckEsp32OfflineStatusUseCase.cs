using SensorService.Application.DTOs.Esp32Node;
using SensorService.Domain.ValueObjects;

namespace SensorService.Application.Interfaces.UseCases.Esp32OfflineWorker;

/// <summary>
/// Contrato del caso de uso para verificar periódicamente el estado de conectividad de los ESP32.
/// Ejecutado por un worker en segundo plano para detectar dispositivos desconectados.
/// </summary>
public interface ICheckEsp32OfflineStatusUseCase
{
    /// <summary>
    /// Verifica el estado de todos los ESP32 y genera/resuelve alertas según el umbral de inactividad.
    /// </summary>
    /// <param name="threshold">Umbral de tiempo de inactividad para considerar un ESP32 como desconectado.</param>
    /// <param name="currentTime">Tiempo actual de referencia (opcional, usa UTC ahora por defecto).</param>
    /// <returns>Resultado con la cantidad de ESP32 verificados, desconectados y alertas creadas.</returns>
    Task<CheckEsp32StatusResult> ExecuteAsync(
    OfflineThreshold threshold,
    DateTime? currentTime = null);
}
