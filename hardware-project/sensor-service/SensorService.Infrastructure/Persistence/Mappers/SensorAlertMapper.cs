using SensorService.Domain.Entities;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Mappers;

public static class SensorAlertMapper
{
    public static SensorAlert ToEntity(SensorAlertDocument doc) => new()
    {
        Id = doc.Id,
        SensorId = doc.SensorId,
        Type = doc.Type,
        Value = doc.Value,
        Threshold = doc.Threshold,
        Timestamp = doc.Timestamp,
        Message = doc.Message,
        Severity = doc.Severity,
        Acknowledged = doc.Acknowledged
    };

    public static SensorAlertDocument ToDocument(SensorAlert entity) => new()
    {
        Id = entity.Id,
        SensorId = entity.SensorId,
        Type = entity.Type,
        Value = entity.Value,
        Threshold = entity.Threshold,
        Timestamp = entity.Timestamp,
        Message = entity.Message,
        Severity = entity.Severity,
        Acknowledged = entity.Acknowledged
    };
}
