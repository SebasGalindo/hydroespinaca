using SensorService.Application.DTOs.Reading;
using SensorService.Domain.Entities;

namespace SensorService.Application.Mappers;

public static class ReadingMapper
{
    public static Reading ToEntity(ReadingDto dto) => new()
    {
        SensorId = dto.SensorId,
        VariableId = dto.VariableId,
        Value = dto.Value,
        Timestamp = dto.Timestamp
    };

    public static ReadingDto ToDto(Reading entity) => new()
    {
        SensorId = entity.SensorId,
        VariableId = entity.VariableId,
        Value = entity.Value,
        Timestamp = entity.Timestamp
    };
}
