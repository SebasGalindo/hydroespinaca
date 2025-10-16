using HydroEspinaca.Shared.DTOs.Alerts;
using SensorService.Domain.Entities;

namespace SensorService.Application.Mappers;

public static class SensorAlertMapper
{
    public static SensorAlertDto ToDto(SensorAlert alert) => new()
    {
        Id = alert.Id,
        VariableCode = alert.VariableCode,
        Value = alert.Value,
        Timestamp = alert.Timestamp,
        Message = alert.Message,
        Acknowledged = alert.Acknowledged
    };

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
