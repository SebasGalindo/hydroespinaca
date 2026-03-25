using HydroEspinaca.Shared.DTOs.Alerts;
using SensorService.Application.DTOs.Alert;

namespace SensorService.Application.Interfaces;

/// <summary>
/// Contrato del servicio de alertas de dispositivos ESP32.
/// Gestiona la consulta y reconocimiento de alertas de conectividad de los microcontroladores.
/// </summary>
public interface IEsp32AlertService
{
    /// <summary>
    /// Obtiene todas las alertas asociadas a un dispositivo ESP32 específico.
    /// </summary>
    /// <param name="esp32Id">Identificador del dispositivo ESP32.</param>
    /// <returns>Lista de alertas del ESP32.</returns>
    Task<List<Esp32AlertDto>> GetByEsp32IdAsync(string esp32Id);

    /// <summary>
    /// Actualiza el estado de reconocimiento de una alerta de ESP32.
    /// </summary>
    /// <param name="id">Identificador de la alerta.</param>
    /// <param name="dto">DTO con el nuevo estado de reconocimiento.</param>
    Task AcknowledgeAsync(string id, Esp32AlertUpdateDto dto);
}
