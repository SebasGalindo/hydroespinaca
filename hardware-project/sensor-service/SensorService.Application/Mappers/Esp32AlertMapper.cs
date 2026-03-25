using HydroEspinaca.Shared.DTOs.Alerts;
using HydroEspinaca.Shared.Enums;
using SensorService.Domain.Entities;

namespace SensorService.Application.Mappers;

/// <summary>
/// Mapper estático para convertir entre entidades de dominio Esp32Alert y sus DTOs correspondientes.
/// Gestiona la conversión de alertas de conectividad de dispositivos ESP32.
/// </summary>
public static class Esp32AlertMapper
{
    /// <summary>
    /// Convierte una entidad de dominio Esp32Alert a su DTO de respuesta.
    /// </summary>
    /// <param name="alert">Entidad de dominio Esp32Alert.</param>
    /// <returns>DTO con los datos de la alerta ESP32 para la respuesta API.</returns>
    public static Esp32AlertDto ToDto(Esp32Alert alert) => new()
    {
        Id = alert.Id,
        Esp32Id = alert.Esp32Id,
        Type = alert.Type.ToString(),
        Timestamp = alert.Timestamp,
        Message = alert.Message,
        Severity = alert.Severity.ToString(),
        Acknowledged = alert.Acknowledged
    };

    /// <summary>
    /// Convierte un DTO de alerta ESP32 a una entidad de dominio.
    /// Valida y parsea el tipo de alerta y la severidad.
    /// </summary>
    /// <param name="dto">DTO con los datos de la alerta.</param>
    /// <returns>Entidad de dominio Esp32Alert.</returns>
    public static Esp32Alert ToEntity(Esp32AlertDto dto)
    {
        if (!Enum.TryParse<AlertType>(dto.Type, true, out var type))
            throw new ArgumentException($"Invalid alert type: '{dto.Type}'.");

        if (!Enum.TryParse<AlertSeverity>(dto.Severity, true, out var severity))
            throw new ArgumentException($"Invalid alert severity: '{dto.Severity}'.");

        var alert = new Esp32Alert
        {
            Esp32Id = dto.Esp32Id,
            Type = type,
            Timestamp = dto.Timestamp,
            Message = dto.Message,
            Severity = severity,
            Acknowledged = dto.Acknowledged
        };
        alert.SetId(dto.Id);
        return alert;
    }
}
