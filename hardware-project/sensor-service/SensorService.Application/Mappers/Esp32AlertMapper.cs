using HydroEspinaca.Shared.DTOs.Alerts;
using HydroEspinaca.Shared.Enums;
using SensorService.Domain.Entities;

namespace SensorService.Application.Mappers;

public static class Esp32AlertMapper
{
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
