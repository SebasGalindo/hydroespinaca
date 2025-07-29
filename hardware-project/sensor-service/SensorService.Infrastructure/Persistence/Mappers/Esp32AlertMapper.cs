using HydroEspinaca.Shared.Mongo.Interfaces;
using SensorService.Domain.Entities;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Mappers;

public class Esp32AlertMapper : IEntityMapper<Esp32Alert, Esp32AlertDocument>
{
    public Esp32Alert ToEntity(Esp32AlertDocument doc)
    {
        var entity = new Esp32Alert
        {
            Esp32Id = doc.Esp32Id,
            Type = doc.Type,
            Timestamp = doc.Timestamp,
            Message = doc.Message,
            Severity = doc.Severity,
            Acknowledged = doc.Acknowledged
        };
        entity.SetId(doc.Id);
        return entity;
    }

    public Esp32AlertDocument ToDocument(Esp32Alert entity)
    {
        var document = new Esp32AlertDocument
        {
            Esp32Id = entity.Esp32Id,
            Type = entity.Type,
            Timestamp = entity.Timestamp,
            Message = entity.Message,
            Severity = entity.Severity,
            Acknowledged = entity.Acknowledged
        };
        document.SetId(entity.Id);
        return document;
    }
}
