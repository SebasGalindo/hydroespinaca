using SensorService.Application.DTOs.Aggregate;
using SensorService.Domain.Entities;

namespace SensorService.Application.Mappers;

/// <summary>
/// Mapper estático para convertir entidades de dominio Aggregate a sus DTOs de respuesta.
/// Transforma los datos estadísticos agregados de sensores hidropónicos para la API.
/// </summary>
public static class AggregateMapper
{
    /// <summary>
    /// Convierte una entidad de dominio Aggregate a su DTO de respuesta.
    /// </summary>
    /// <param name="entity">Entidad de dominio Aggregate con datos estadísticos.</param>
    /// <returns>DTO con los datos agregados para la respuesta API.</returns>
    public static AggregateDto ToDto(Aggregate entity)
    {
        return new AggregateDto
        {
            SensorCode = entity.SensorCode,
            VariableCode = entity.VariableCode,
            Avg = entity.Avg,
            Min = entity.Min,
            Max = entity.Max,
            Count = entity.Count,
            Timestamp = entity.Timestamp
        };
    }
}
