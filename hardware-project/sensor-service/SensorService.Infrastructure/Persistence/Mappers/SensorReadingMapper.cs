using SensorService.Domain.Entities;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Mappers;

public static class SensorReadingMapper
{
    public static SensorReading ToEntity(SensorReadingDocument doc) => new()
    {
        Id = doc.Id,
        SensorId = doc.SensorId,
        Type = doc.Type,
        Value = doc.Value,
        Timestamp = doc.Timestamp
    };

    public static SensorReadingDocument ToDocument(SensorReading entity) => new()
    {
        Id = entity.Id,
        SensorId = entity.SensorId,
        Type = entity.Type,
        Value = entity.Value,
        Timestamp = entity.Timestamp
    };
}