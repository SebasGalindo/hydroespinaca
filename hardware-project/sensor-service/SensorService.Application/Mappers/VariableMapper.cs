using SensorService.Domain.Entities;
using HydroEspinaca.Shared.DTOs.Variables;
using HydroEspinaca.Shared.Enums;
using MongoDB.Driver;

namespace SensorService.Application.Mappers;

/// <summary>
/// Mapper estático para convertir entre entidades de dominio Variable y sus DTOs correspondientes.
/// Maneja la conversión bidireccional incluyendo la validación de tipos de variable y regulación.
/// </summary>
public static class VariableMapper
{
    /// <summary>
    /// Convierte una entidad de dominio Variable a su DTO de respuesta.
    /// </summary>
    /// <param name="v">Entidad de dominio Variable.</param>
    /// <returns>DTO con los datos de la variable para la respuesta API.</returns>
    public static VariableDto ToDto(Variable v) => new()
    {
        Id = v.Id,
        Code = v.Code,
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

    /// <summary>
    /// Convierte un DTO de creación de variable a una entidad de dominio.
    /// Valida y parsea el tipo de variable y el tipo de regulación.
    /// </summary>
    /// <param name="dto">DTO con los datos de creación de la variable.</param>
    /// <returns>Nueva entidad de dominio Variable.</returns>
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
            Code = dto.Code,
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

    /// <summary>
    /// Actualiza una entidad Variable existente con los valores del DTO de actualización.
    /// Valida y parsea el tipo de variable y el tipo de regulación antes de aplicar los cambios.
    /// </summary>
    /// <param name="dto">DTO con los nuevos datos de la variable.</param>
    /// <param name="entity">Entidad de dominio existente a actualizar.</param>
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

        entity.Code = dto.Code;
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
