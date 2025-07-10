using SensorService.Domain.Entities;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Mappers;
public static class SensorMapper
{
    public static Sensor ToEntity(SensorDocument doc) => new()
    {
        Id = doc.Id,
        Code = doc.Code,
        Type = doc.Type,
        Unit = doc.Unit,
        PhysicalId = doc.PhysicalId,
        Location = doc.Location,
        SamplingFrequency = doc.SamplingFrequency,
        Status = doc.Status
    };

    public static SensorDocument ToDocument(Sensor entity)
    {
        var doc = new SensorDocument
        {
            Code = entity.Code,
            Type = entity.Type,
            Unit = entity.Unit,
            PhysicalId = entity.PhysicalId,
            Location = entity.Location,
            SamplingFrequency = entity.SamplingFrequency,
            Status = entity.Status
        };

        if (!string.IsNullOrWhiteSpace(entity.Id))
            doc.Id = entity.Id;

        return doc;
    }

}