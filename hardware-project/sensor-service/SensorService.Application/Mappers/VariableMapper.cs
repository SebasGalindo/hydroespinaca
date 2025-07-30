using HydroEspinaca.Shared.DTOs.Variables;
using HydroEspinaca.Shared.Enums;
using MongoDB.Driver;

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
        Type = v.Type.ToString(),
        LastModified = v.LastModified
    };

    public static Variable ToEntity(VariableCreateDto dto)
    {
        if (!Enum.TryParse<VariableTypes>(dto.Type, true, out var variableType))
            throw new ArgumentException($"Invalid variable type: '{dto.Type}'.");

        var variableEn =  new Variable
        {
            Name = dto.Name,
            Unit = dto.Unit,
            Description = dto.Description,
            MinValue = dto.MinValue,
            MaxValue = dto.MaxValue,
            Type = variableType,
            LastModified = DateTime.UtcNow
        };
        return variableEn;

    }

    public static void MapUpdate(VariableUpdateDto dto, Variable entity)
    {
        if (!Enum.TryParse<VariableTypes>(dto.Type, true, out var variableType))
            throw new ArgumentException($"Invalid variable type: '{dto.Type}'.");

        entity.Name = dto.Name;
        entity.Unit = dto.Unit;
        entity.Description = dto.Description;
        entity.MinValue = dto.MinValue;
        entity.MaxValue = dto.MaxValue;
        entity.Type = variableType;
        entity.LastModified = DateTime.UtcNow;
    }
}
