using SensorService.Domain.Entities;
using HydroEspinaca.Shared.Enums;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Mappers;

public static class SensorMapper
{
    public static Sensor ToEntity(SensorDocument doc) => new()
    {
        Id = doc.Id,
        Code = doc.Code,
        PhysicalId = doc.PhysicalId,
        Location = doc.Location,
        Esp32Id = doc.Esp32Id,
        SamplingFrequency = doc.SamplingFrequency,
        Variables = doc.Variables,
        Status = Enum.Parse<SensorStatus>(doc.Status, ignoreCase: true),
        CreatedAt = doc.CreatedAt
    };

    public static SensorDocument ToDocument(Sensor entity) => new()
    {
        Id = entity.Id,
        Code = entity.Code,
        PhysicalId = entity.PhysicalId,
        Location = entity.Location,
        Esp32Id = entity.Esp32Id,
        SamplingFrequency = entity.SamplingFrequency,
        Variables = entity.Variables,
        Status = entity.Status.ToString(),
        CreatedAt = entity.CreatedAt
    };
}
