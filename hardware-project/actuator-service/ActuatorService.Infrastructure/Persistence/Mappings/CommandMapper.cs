using ActuatorService.Domain.Entities;
using ActuatorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Enums;
using HydroEspinaca.Shared.Mongo.Interfaces;

namespace ActuatorService.Infrastructure.Persistence.Mappers;

public class CommandMapper : IEntityMapper<ActuatorCommand, ActuatorCommandDocument>
{
    public ActuatorCommand ToEntity(ActuatorCommandDocument doc)
    {
        return new ActuatorCommand
        {
            Id = doc.Id,
            ActuatorId = doc.ActuatorId,
            Esp32Id = doc.Esp32Id,
            Action = doc.Action,
            DurationMs = doc.DurationMs,
            Trigger = Enum.Parse<TriggerType>(doc.Trigger),
            Timestamp = doc.Timestamp,
            Acknowledged = doc.Acknowledged,
            RoutineId = doc.RoutineId,
            RoutineStepOrder = doc.RoutineStepOrder,
            UserId = doc.UserId,
            Metadata = doc.Metadata is null ? null : new CommandMetadata
            {
                Source = doc.Metadata.Source,
                FuzzyRule = doc.Metadata.FuzzyRule,
                Inputs = doc.Metadata.Inputs
            }
        };
    }

    public ActuatorCommandDocument ToDocument(ActuatorCommand entity)
    {
        return new ActuatorCommandDocument
        {
            Id = entity.Id,
            ActuatorId = entity.ActuatorId,
            Esp32Id = entity.Esp32Id,
            Action = entity.Action,
            DurationMs = entity.DurationMs,
            Trigger = entity.Trigger.ToString(),
            Timestamp = entity.Timestamp,
            Acknowledged = entity.Acknowledged,
            RoutineId = entity.RoutineId,
            RoutineStepOrder = entity.RoutineStepOrder,
            UserId = entity.UserId,
            Metadata = entity.Metadata is null ? null : new MetadataDocument
            {
                Source = entity.Metadata.Source,
                FuzzyRule = entity.Metadata.FuzzyRule,
                Inputs = entity.Metadata.Inputs
            }
        };
    }
}
