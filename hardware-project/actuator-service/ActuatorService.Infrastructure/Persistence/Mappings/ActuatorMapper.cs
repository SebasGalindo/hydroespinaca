using ActuatorService.Domain.Entities;
using ActuatorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Enums;
using HydroEspinaca.Shared.Mongo.Interfaces;

namespace ActuatorService.Infrastructure.Persistence.Mappings;
public class ActuatorMapper : IEntityMapper<Actuator, ActuatorDocument>
{
    public Actuator ToEntity(ActuatorDocument doc)
    {
        var actuator = new Actuator
        {
            Esp32Id = doc.Esp32Id,
            Name = doc.Name,
            Type = Enum.Parse<ActuatorType>(doc.Type),
            PhysicalId = doc.PhysicalId,
            Pin = doc.Pin,
            Location = doc.Location,
            Status = Enum.Parse<ActuatorStatus>(doc.Status),
            CreatedAt = doc.CreatedAt
        };
        actuator.SetId(doc.Id);
        return actuator;
    }

    public ActuatorDocument ToDocument(Actuator entity) => new()
    {
        Id = entity.Id,
        Esp32Id = entity.Esp32Id,
        Name = entity.Name,
        Type = entity.Type.ToString(),
        PhysicalId = entity.PhysicalId,
        Pin = entity.Pin,
        Location = entity.Location,
        Status = entity.Status.ToString(),
        CreatedAt = entity.CreatedAt
    };
}