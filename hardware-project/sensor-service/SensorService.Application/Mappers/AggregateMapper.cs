using SensorService.Application.DTOs.Aggregate;
using SensorService.Domain.Entities;

namespace SensorService.Application.Mappers;

public static class AggregateMapper
{
    public static AggregateDto ToDto(Aggregate entity)
    {
        return new AggregateDto
        {
            SensorId = entity.SensorId,
            VariableId = entity.VariableId,
            Avg = entity.Avg,
            Min = entity.Min,
            Max = entity.Max,
            Count = entity.Count,
            Timestamp = entity.Timestamp
        };
    }
}
