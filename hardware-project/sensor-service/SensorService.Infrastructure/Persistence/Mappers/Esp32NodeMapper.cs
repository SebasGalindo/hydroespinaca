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
            FreeHeap = doc.FreeHeap,
            Uptime = doc.Uptime,
            LastHeartbeat = doc.LastHeartbeat
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
            FreeHeap = entity.FreeHeap,
            Uptime = entity.Uptime,
            LastHeartbeat = entity.LastHeartbeat
        };
        document.SetId(entity.Id);
        return document;
    }
}
