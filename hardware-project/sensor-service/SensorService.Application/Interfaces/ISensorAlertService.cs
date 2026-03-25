using HydroEspinaca.Shared.DTOs.Alerts;
using SensorService.Application.DTOs.Alert;


namespace SensorService.Application.Interfaces;

/// <summary>
/// Contrato del servicio de alertas de sensores hidropónicos.
/// Gestiona la consulta y reconocimiento de alertas generadas por valores fuera de rango.
/// </summary>
public interface ISensorAlertService
{
    /// <summary>
    /// Obtiene todas las alertas asociadas a un sensor específico.
    /// </summary>
    /// <param name="sensorId">Identificador del sensor.</param>
    /// <returns>Lista de alertas del sensor.</returns>
    Task<List<SensorAlertDto>> GetBySensorIdAsync(string sensorId);

    /// <summary>
    /// Actualiza el estado de reconocimiento de una alerta de sensor.
    /// </summary>
    /// <param name="id">Identificador de la alerta.</param>
    /// <param name="dto">DTO con el nuevo estado de reconocimiento.</param>
    Task AcknowledgeAsync(string id, SensorAlertUpdateDto dto);
}