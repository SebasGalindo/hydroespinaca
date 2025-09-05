using SensorService.Domain.Entities;
using SensorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo.Interfaces;

namespace SensorService.Infrastructure.Persistence.Mappers;

public class Esp32NodeMapper : IEntityMapper<Esp32Node, Esp32NodeDocument>
{
    public Esp32Node ToEntity(Esp32NodeDocument doc)
    {
        var entity = new Esp32Node
        {
            Name = doc.Name,
            Location = doc.Location,
            LastSeen = doc.LastSeen,
            Status = doc.Status,
            Uptime = doc.Uptime,
            FreeHeap = doc.FreeHeap
        };
        entity.SetId(doc.Id);
        return entity;
    }

    public Esp32NodeDocument ToDocument(Esp32Node entity)
    {
        var document = new Esp32NodeDocument
        {
            Name = entity.Name,
            Location = entity.Location,
            LastSeen = entity.LastSeen,
            Status = entity.Status,
            Uptime = entity.Uptime,
            FreeHeap = entity.FreeHeap
        };
        document.SetId(entity.Id);
        return document;
    }
}
