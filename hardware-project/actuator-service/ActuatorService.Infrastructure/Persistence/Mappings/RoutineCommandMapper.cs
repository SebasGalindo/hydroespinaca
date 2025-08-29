using ActuatorService.Domain.Entities;
using ActuatorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Enums;
using HydroEspinaca.Shared.Mongo.Interfaces;

namespace ActuatorService.Infrastructure.Persistence.Mappings;

public class RoutineCommandMapper : IEntityMapper<RoutineCommand, RoutineCommandDocument>
{
    public RoutineCommand ToEntity(RoutineCommandDocument doc)
    {
        var entity = new RoutineCommand
        {
            CommandId = doc.CommandId,
            RoutineId = doc.RoutineId,
            StatusGeneral = Enum.Parse<RoutineCommandStatus>(doc.StatusGeneral),
            CreatedAt = doc.CreatedAt,
            FinishedAt = doc.FinishedAt,
            Channel = doc.Channel,
            Results = doc.Results?.Select(r => new RoutineResult
            {
                Pin = r.Pin,
                Status = r.Status
            }).ToList(),
            ExecutionLogs = doc.ExecutionLogs
        };
        entity.SetId(doc.Id);
        return entity;
    }

    public RoutineCommandDocument ToDocument(RoutineCommand entity)
    {
        var doc = new RoutineCommandDocument
        {
            CommandId = entity.CommandId,
            RoutineId = entity.RoutineId,
            StatusGeneral = entity.StatusGeneral.ToString(),
            CreatedAt = entity.CreatedAt,
            FinishedAt = entity.FinishedAt,
            Channel = entity.Channel,
            Results = entity.Results?.Select(r => new RoutineResultDocument
            {
                Pin = r.Pin,
                Status = r.Status
            }).ToList(),
            ExecutionLogs = entity.ExecutionLogs
        };
        doc.SetId(entity.Id);
        return doc;
    }
}