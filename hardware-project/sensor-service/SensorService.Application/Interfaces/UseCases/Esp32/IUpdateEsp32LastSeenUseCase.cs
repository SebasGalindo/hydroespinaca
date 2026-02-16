namespace SensorService.Application.Interfaces.UseCases.Esp32;

/// <summary>
/// Contrato del caso de uso para actualizar la última actividad conocida de un dispositivo ESP32.
/// Se ejecuta cada vez que se recibe un lote de lecturas válido de un ESP32.
/// </summary>
public interface IUpdateEsp32LastSeenUseCase
{
    /// <summary>
    /// Actualiza la marca de tiempo de última actividad del ESP32 especificado.
    /// </summary>
    /// <param name="esp32Id">Identificador del dispositivo ESP32.</param>
    /// <param name="timestamp">Marca de tiempo de la última actividad detectada.</param>
    Task ExecuteAsync(string esp32Id, DateTime timestamp);
}