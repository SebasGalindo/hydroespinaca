using SensorService.Application.DTOs.Alert;
using SensorService.Domain.Entities;

namespace SensorService.Application.Mappers;

public static class SensorAlertMapper
{
    public static SensorAlertDto ToDto(SensorAlert alert) => new()
    {
        Id = alert.Id,
        SensorId = alert.SensorId,
        Type = alert.Type,
        Value = alert.Value,
        Threshold = alert.Threshold,
        Timestamp = alert.Timestamp,
        Message = alert.Message,
        Severity = alert.Severity,
        Acknowledged = alert.Acknowledged
    };

    public static SensorAlert ToEntity(SensorAlertDto dto) => new()
    {
        Id = dto.Id,
        SensorId = dto.SensorId,
        Type = dto.Type,
        Value = dto.Value,
        Threshold = dto.Threshold,
        Timestamp = dto.Timestamp,
        Message = dto.Message,
        Severity = dto.Severity,
        Acknowledged = dto.Acknowledged
    };
}
