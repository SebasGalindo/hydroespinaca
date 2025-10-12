using HydroEspinaca.Shared.DTOs.Alerts;
using HydroEspinaca.Shared.Enums;
using SensorService.Domain.Entities;

namespace SensorService.Application.Mappers;

public static class SensorAlertMapper
{
    public static SensorAlertDto ToDto(SensorAlert alert) => new()
    {
        Id = alert.Id,
        SensorCode = alert.SensorCode,
        VariableCode = alert.VariableCode,
        Type = alert.Type.ToString(),
        Value = alert.Value,
        Threshold = alert.Threshold,
        Timestamp = alert.Timestamp,
        Message = alert.Message,
        Severity = alert.Severity.ToString(),
        Acknowledged = alert.Acknowledged
    };

    public static SensorAlert ToEntity(SensorAlertDto dto)
    {
        if (!Enum.TryParse<AlertType>(dto.Type, true, out var alertType))
            throw new ArgumentException($"Invalid alert type: '{dto.Type}'.");

        if (!Enum.TryParse<AlertSeverity>(dto.Severity, true, out var severity))
            throw new ArgumentException($"Invalid alert severity: '{dto.Severity}'.");

        var sensorAlert = new SensorAlert
        {
            SensorCode = dto.SensorCode,
            VariableCode = dto.VariableCode,
            Type = alertType,
            Value = dto.Value,
            Threshold = dto.Threshold,
            Timestamp = dto.Timestamp,
            Message = dto.Message,
            Severity = severity,
            Acknowledged = dto.Acknowledged
        };
        sensorAlert.SetId(dto.Id);
        return sensorAlert;
    }
}
