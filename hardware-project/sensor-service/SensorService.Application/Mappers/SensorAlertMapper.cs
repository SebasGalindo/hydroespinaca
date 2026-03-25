using HydroEspinaca.Shared.DTOs.Alerts;
using SensorService.Domain.Entities;

namespace SensorService.Application.Mappers;

/// <summary>
/// Mapper estático para convertir entre entidades de dominio SensorAlert y sus DTOs correspondientes.
/// Gestiona la conversión de alertas generadas por valores fuera del rango óptimo.
/// </summary>
public static class SensorAlertMapper
{
    /// <summary>
    /// Convierte una entidad de dominio SensorAlert a su DTO de respuesta.
    /// </summary>
    /// <param name="alert">Entidad de dominio SensorAlert.</param>
    /// <returns>DTO con los datos de la alerta para la respuesta API.</returns>
    public static SensorAlertDto ToDto(SensorAlert alert) => new()
    {
        Id = alert.Id,
        VariableCode = alert.VariableCode,
        Value = alert.Value,
        Timestamp = alert.Timestamp,
        Message = alert.Message,
        Acknowledged = alert.Acknowledged
    };

    /// <summary>
    /// Convierte un DTO de alerta de sensor a una entidad de dominio.
    /// </summary>
    /// <param name="dto">DTO con los datos de la alerta.</param>
    /// <returns>Entidad de dominio SensorAlert.</returns>
    public static SensorAlert ToEntity(SensorAlertDto dto)
    {
        var sensorAlert = new SensorAlert
        {
            VariableCode = dto.VariableCode,
            Value = dto.Value,
            Timestamp = dto.Timestamp,
            Message = dto.Message,
            Acknowledged = dto.Acknowledged
        };
        sensorAlert.SetId(dto.Id);
        return sensorAlert;
    }
}
