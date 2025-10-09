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
        PhysicalMin = v.PhysicalMin,
        PhysicalMax = v.PhysicalMax,
        OptimalMin = v.OptimalMin,
        OptimalMax = v.OptimalMax,
        Type = v.Type.ToString(),
        RegulationType = v.RegulationType?.ToString(),
        LastModified = v.LastModified
    };

    public static Variable ToEntity(VariableCreateDto dto)
    {
        if (!Enum.TryParse<VariableTypes>(dto.Type, true, out var variableType))
            throw new ArgumentException($"Invalid variable type: '{dto.Type}'.");

        RegulationType? regulationType = null;
        if (!string.IsNullOrEmpty(dto.RegulationType))
        {
            if (!Enum.TryParse<RegulationType>(dto.RegulationType, true, out var parsedRegulationType))
                throw new ArgumentException($"Invalid regulation type: '{dto.RegulationType}'.");
            regulationType = parsedRegulationType;
        }

        var variableEn =  new Variable
        {
            Name = dto.Name,
            Unit = dto.Unit,
            Description = dto.Description,
            PhysicalMin = dto.PhysicalMin,
            PhysicalMax = dto.PhysicalMax,
            OptimalMin = dto.OptimalMin,
            OptimalMax = dto.OptimalMax,
            Type = variableType,
            RegulationType = regulationType,
            LastModified = DateTime.UtcNow
        };
        return variableEn;

    }

    public static void MapUpdate(VariableUpdateDto dto, Variable entity)
    {
        if (!Enum.TryParse<VariableTypes>(dto.Type, true, out var variableType))
            throw new ArgumentException($"Invalid variable type: '{dto.Type}'.");

        RegulationType? regulationType = null;
        if (!string.IsNullOrEmpty(dto.RegulationType))
        {
            if (!Enum.TryParse<RegulationType>(dto.RegulationType, true, out var parsedRegulationType))
                throw new ArgumentException($"Invalid regulation type: '{dto.RegulationType}'.");
            regulationType = parsedRegulationType;
        }

        entity.Name = dto.Name;
        entity.Unit = dto.Unit;
        entity.Description = dto.Description;
        entity.PhysicalMin = dto.PhysicalMin;
        entity.PhysicalMax = dto.PhysicalMax;
        entity.OptimalMin = dto.OptimalMin;
        entity.OptimalMax = dto.OptimalMax;
        entity.Type = variableType;
        entity.RegulationType = regulationType;
        entity.LastModified = DateTime.UtcNow;
    }
}
