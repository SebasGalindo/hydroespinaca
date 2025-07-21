using HydroEspinaca.Shared.DTOs.Sensors;
using SensorService.Domain.Entities;
using HydroEspinaca.Shared.Enums;

namespace SensorService.Application.Mappers;
public static class SensorMapper
{
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
        CreatedAt = sensor.CreatedAt
    };

    public static Sensor ToEntity(SensorCreateDto dto) => new()
    {
        Code = dto.Code,
        PhysicalId = dto.PhysicalId,
        Location = dto.Location,
        Esp32Id = dto.Esp32Id,
        SamplingFrequency = dto.SamplingFrequency,
        Variables = dto.Variables,
        CreatedAt = DateTime.UtcNow,
        Status = SensorStatus.Active
    };

    public static void MapUpdate(SensorUpdateDto dto, Sensor existing)
    {
        existing.PhysicalId = dto.PhysicalId;
        existing.Location = dto.Location;
        existing.Esp32Id = dto.Esp32Id;
        existing.SamplingFrequency = dto.SamplingFrequency;
        existing.Variables = dto.Variables;
        existing.Status = Enum.Parse<SensorStatus>(dto.Status, ignoreCase: true);
    }

}
