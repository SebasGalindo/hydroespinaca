using HydroEspinaca.Shared.Mongo.Interfaces;
using SensorService.Domain.Entities;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Mappers;

public class Esp32NodeMapper : IEntityMapper<Esp32Node, Esp32NodeDocument>
{
    public Esp32Node ToEntity(Esp32NodeDocument doc) => new()
    {
        Id = doc.Id,
        Name = doc.Name,
        Location = doc.Location,
        LastSeen = doc.LastSeen,
        Status = doc.Status
    };

    public Esp32NodeDocument ToDocument(Esp32Node entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Location = entity.Location,
        LastSeen = entity.LastSeen,
        Status = entity.Status
    };
}
