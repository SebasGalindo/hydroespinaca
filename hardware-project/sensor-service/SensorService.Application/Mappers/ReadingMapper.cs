using SensorService.Application.DTOs.Reading;
using SensorService.Domain.Entities;

namespace SensorService.Application.Mappers;

public static class ReadingMapper
{
    public static Reading ToEntity(ReadingDto dto) => new()
    {
        SensorCode = dto.SensorCode,
        VariableCode = dto.VariableCode,
        Value = dto.Value,
        Timestamp = dto.Timestamp
    };

    public static ReadingDto ToDto(Reading entity) => new()
    {
        SensorCode = entity.SensorCode,
        VariableCode = entity.VariableCode,
        Value = entity.Value,
        Timestamp = entity.Timestamp
    };
}
