using SensorService.Application.DTOs.Reading;
using SensorService.Domain.Entities;

namespace SensorService.Application.Mappers;

/// <summary>
/// Mapper estático para convertir entre entidades de dominio Reading y sus DTOs correspondientes.
/// Gestiona la conversión bidireccional de lecturas de sensores hidropónicos.
/// </summary>
public static class ReadingMapper
{
    /// <summary>
    /// Convierte un DTO de lectura a una entidad de dominio.
    /// </summary>
    /// <param name="dto">DTO con los datos de la lectura.</param>
    /// <returns>Entidad de dominio Reading.</returns>
    public static Reading ToEntity(ReadingDto dto) => new()
    {
        SensorCode = dto.SensorCode,
        VariableCode = dto.VariableCode,
        Value = dto.Value,
        Timestamp = dto.Timestamp
    };

    /// <summary>
    /// Convierte una entidad de dominio Reading a su DTO de respuesta.
    /// </summary>
    /// <param name="entity">Entidad de dominio Reading.</param>
    /// <returns>DTO con los datos de la lectura para la respuesta API.</returns>
    public static ReadingDto ToDto(Reading entity) => new()
    {
        SensorCode = entity.SensorCode,
        VariableCode = entity.VariableCode,
        Value = entity.Value,
        Timestamp = entity.Timestamp
    };
}
