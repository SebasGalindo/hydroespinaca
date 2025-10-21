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
            Timestamp = doc.Timestamp,
            Message = doc.Message,
            Acknowledged = doc.Acknowledged,
            ResolvedAt = doc.ResolvedAt,
            EmailSentAt = doc.EmailSentAt,
            Type = doc.Type,
            Severity = doc.Severity
        };
        entity.SetId(doc.Id);
        return entity;
    }

    public Esp32AlertDocument ToDocument(Esp32Alert entity)
    {
        var document = new Esp32AlertDocument
        {
            Esp32Id = entity.Esp32Id,
            Timestamp = entity.Timestamp,
            Message = entity.Message,
            Acknowledged = entity.Acknowledged,
            ResolvedAt = entity.ResolvedAt,
            EmailSentAt = entity.EmailSentAt,
            Type = entity.Type,
            Severity = entity.Severity
        };
        document.SetId(entity.Id);
        return document;
    }
}
