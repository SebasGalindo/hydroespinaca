using SensorService.Application.DTOs.Variable;
using SensorService.Domain.Entities;

namespace SensorService.Application.Mappers;

public static class VariableMapper
{
    public static VariableDto ToDto(Variable v) => new()
    {
        Id = v.Id,
        Name = v.Name,
        Unit = v.Unit,
        Description = v.Description,
        MinValue = v.MinValue,
        MaxValue = v.MaxValue,
        Type = v.Type,
        LastModified = v.LastModified
    };

    public static Variable ToEntity(VariableCreateDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Unit = dto.Unit,
        Description = dto.Description,
        MinValue = dto.MinValue,
        MaxValue = dto.MaxValue,
        Type = dto.Type,
        LastModified = DateTime.UtcNow
    };

    public static void MapUpdate(VariableUpdateDto dto, Variable entity)
    {
        entity.Name = dto.Name;
        entity.Unit = dto.Unit;
        entity.Description = dto.Description;
        entity.MinValue = dto.MinValue;
        entity.MaxValue = dto.MaxValue;
        entity.Type = dto.Type;
        entity.LastModified = DateTime.UtcNow;
    }
}
