using SensorService.Domain.Entities;
using HydroEspinaca.Shared.Enums;
using SensorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo.Interfaces;

namespace SensorService.Infrastructure.Persistence.Mappers;

/// <summary>
/// Mapper que convierte entre la entidad de dominio <see cref="Sensor"/> y el documento MongoDB <see cref="SensorDocument"/>.
/// </summary>
public class SensorMapper : IEntityMapper<Sensor, SensorDocument>
{
    /// <summary>
    /// Convierte un documento MongoDB de sensor a su entidad de dominio correspondiente.
    /// </summary>
    /// <param name="doc">El documento MongoDB de sensor.</param>
    /// <returns>La entidad de dominio <see cref="Sensor"/>.</returns>
    public Sensor ToEntity(SensorDocument doc)
    {
        var Sensor = new Sensor
        {
            Code = doc.Code,
            PhysicalId = doc.PhysicalId,
            Location = doc.Location,
            Esp32Id = doc.Esp32Id,
            SamplingFrequency = doc.SamplingFrequency,
            Variables = doc.Variables,
            Status = doc.Status,
            AllowMissing = doc.AllowMissing,
            CreatedAt = doc.CreatedAt
        };
        Sensor.SetId(doc.Id);
        return Sensor;
    }

    /// <summary>
    /// Convierte una entidad de dominio de sensor a su documento MongoDB correspondiente.
    /// </summary>
    /// <param name="entity">La entidad de dominio <see cref="Sensor"/>.</param>
    /// <returns>El documento MongoDB <see cref="SensorDocument"/>.</returns>
    public SensorDocument ToDocument(Sensor entity)
    {
        var document = new SensorDocument
        {
            Code = entity.Code,
            PhysicalId = entity.PhysicalId,
            Location = entity.Location,
            Esp32Id = entity.Esp32Id,
            SamplingFrequency = entity.SamplingFrequency,
            Variables = entity.Variables,
            Status = entity.Status,
            AllowMissing = entity.AllowMissing,
            CreatedAt = entity.CreatedAt
        };
        document.SetId(entity.Id);
        return document;
    }
}
