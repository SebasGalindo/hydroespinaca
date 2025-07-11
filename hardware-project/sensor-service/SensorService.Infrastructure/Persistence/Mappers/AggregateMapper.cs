using SensorService.Domain.Entities;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Mappers;

public static class AggregateMapper
{
    public static Aggregate ToEntity(AggregateDocument doc) => new()
    {
        Id = doc.Id,
        SensorId = doc.SensorId,
        VariableId = doc.VariableId,
        Avg = doc.Avg,
        Min = doc.Min,
        Max = doc.Max,
        Count = doc.Count,
        Timestamp = doc.Timestamp
    };

    public static AggregateDocument ToDocument(Aggregate entity) => new()
    {
        Id = entity.Id,
        SensorId = entity.SensorId,
        VariableId = entity.VariableId,
        Avg = entity.Avg,
        Min = entity.Min,
        Max = entity.Max,
        Count = entity.Count,
        Timestamp = entity.Timestamp
    };
}
