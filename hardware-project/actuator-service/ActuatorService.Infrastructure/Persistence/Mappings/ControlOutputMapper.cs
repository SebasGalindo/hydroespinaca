using ActuatorService.Domain.Entities;
using ActuatorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo.Interfaces;

namespace ActuatorService.Infrastructure.Persistence.Mappings;

public class ControlOutputMapper : IEntityMapper<ControlOutput, ControlOutputDocument>
{
    public ControlOutput ToEntity(ControlOutputDocument doc)
    {
        var controlOutput = new ControlOutput
        {
            Name = doc.Name,
            Description = doc.Description,
            Unit = doc.Unit,
            ActuatorId = doc.ActuatorId,
            MinValue = doc.MinValue,
            MaxValue = doc.MaxValue,
            LastModified = doc.LastModified
        };
        controlOutput.SetId(doc.Id);
        return controlOutput;
    }

    public ControlOutputDocument ToDocument(ControlOutput entity)
    {
        var doc = new ControlOutputDocument
        {
            Name = entity.Name,
            Description = entity.Description,
            Unit = entity.Unit,
            ActuatorId = entity.ActuatorId,
            MinValue = entity.MinValue,
            MaxValue = entity.MaxValue,
            LastModified = entity.LastModified
        };
        doc.SetId(entity.Id);
        return doc;
    }
}
