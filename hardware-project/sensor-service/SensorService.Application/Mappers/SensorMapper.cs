using HydroEspinaca.Shared.DTOs.Sensors;
using SensorService.Domain.Entities;
using HydroEspinaca.Shared.Enums;

namespace SensorService.Application.Mappers;

/// <summary>
/// Mapper estático para convertir entre entidades de dominio Sensor y sus DTOs correspondientes.
/// Maneja la conversión de sensores IoT del sistema hidropónico.
/// </summary>
public static class SensorMapper
{
    /// <summary>
    /// Convierte una entidad de dominio Sensor a su DTO de respuesta.
    /// </summary>
    /// <param name="sensor">Entidad de dominio Sensor.</param>
    /// <returns>DTO con los datos del sensor para la respuesta API.</returns>
    public static SensorDto ToDto(Sensor sensor) => new()
    {
        Id = sensor.Id,
        Code = sensor.Code,
        PhysicalId = sensor.PhysicalId,
        Location = sensor.Location,
        Esp32Id = sensor.Esp32Id,
        SamplingFrequency = sensor.SamplingFrequency,
        Variables = sensor.Variables,
        Status = sensor.Status.ToString(),
        AllowMissing = sensor.AllowMissing,
        CreatedAt = sensor.CreatedAt
    };

    /// <summary>
    /// Convierte un DTO de creación de sensor a una entidad de dominio con estado activo por defecto.
    /// </summary>
    /// <param name="dto">DTO con los datos de creación del sensor.</param>
    /// <returns>Nueva entidad de dominio Sensor.</returns>
    public static Sensor ToEntity(SensorCreateDto dto) => new()
    {
        Code = dto.Code,
        PhysicalId = dto.PhysicalId,
        Location = dto.Location,
        Esp32Id = dto.Esp32Id,
        SamplingFrequency = dto.SamplingFrequency,
        Variables = dto.Variables,
        AllowMissing = dto.AllowMissing,
        CreatedAt = DateTime.UtcNow,
        Status = SensorStatus.Active
    };

    /// <summary>
    /// Actualiza una entidad Sensor existente con los valores del DTO de actualización.
    /// Valida y parsea el estado del sensor antes de aplicar los cambios.
    /// </summary>
    /// <param name="dto">DTO con los nuevos datos del sensor.</param>
    /// <param name="existing">Entidad de dominio existente a actualizar.</param>
    public static void MapUpdate(SensorUpdateDto dto, Sensor existing)
    {
        if (!Enum.TryParse<SensorStatus>(dto.Status, true, out var sensorStatus))
            throw new ArgumentException($"Invalid sensor status: '{dto.Status}'.");

        existing.PhysicalId = dto.PhysicalId;
        existing.Location = dto.Location;
        existing.Esp32Id = dto.Esp32Id;
        existing.SamplingFrequency = dto.SamplingFrequency;
        existing.Variables = dto.Variables;
        existing.AllowMissing = dto.AllowMissing;
        existing.Status = sensorStatus;
    }

}
