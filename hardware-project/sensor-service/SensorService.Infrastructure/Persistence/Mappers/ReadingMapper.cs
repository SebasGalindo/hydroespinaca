using SensorService.Domain.Entities;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Mappers;

public static class ReadingMapper
{
    public static Reading ToEntity(ReadingDocument doc) => new()
    {
        Id = doc.Id,
        SensorId = doc.SensorId,
        VariableId = doc.VariableId,
        Value = doc.Value,
        Timestamp = doc.Timestamp
    };

    public static ReadingDocument ToDocument(Reading entity) => new()
    {
        Id = entity.Id,
        SensorId = entity.SensorId,
        VariableId = entity.VariableId,
        Value = entity.Value,
        Timestamp = entity.Timestamp
    };
}
