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
            Code = doc.Code,
            Type = Enum.Parse<ActuatorType>(doc.Type),
            Mode = Enum.Parse<ActuatorMode>(doc.Mode),
            PhysicalId = doc.PhysicalId,
            Pin = doc.Pin,
            Location = doc.Location,
            Status = Enum.Parse<ActuatorStatus>(doc.Status),
            CreatedAt = doc.CreatedAt
        };
        actuator.SetId(doc.Id);
        return actuator;
    }

    public ActuatorDocument ToDocument(Actuator entity)
    {
        var doc = new ActuatorDocument
        {
            Esp32Id = entity.Esp32Id,
            Code = entity.Code,
            Type = entity.Type.ToString(),
            Mode = entity.Mode.ToString(),
            PhysicalId = entity.PhysicalId,
            Pin = entity.Pin,
            Location = entity.Location,
            Status = entity.Status.ToString(),
            CreatedAt = entity.CreatedAt
        };
        doc.SetId(entity.Id);
        return doc;
    }
}