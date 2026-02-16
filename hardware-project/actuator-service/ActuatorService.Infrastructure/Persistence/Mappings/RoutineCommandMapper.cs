using ActuatorService.Domain.Entities;
using ActuatorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo.Interfaces;

namespace ActuatorService.Infrastructure.Persistence.Mappings;

/// <summary>
/// Mapper for converting between routine command domain entities and MongoDB documents.
/// </summary>
public class RoutineCommandMapper : IEntityMapper<RoutineCommand, RoutineCommandDocument>
{
    public RoutineCommand ToEntity(RoutineCommandDocument doc)
    {
        var entity = new RoutineCommand
        {
            CommandId = doc.CommandId,
            ActuatorCode = doc.ActuatorCode,
            Esp32Id = doc.Esp32Id,
            StatusGeneral = doc.StatusGeneral,
            CreatedAt = doc.CreatedAt,
            ExtendedAt = doc.ExtendedAt,
            FinishedAt = doc.FinishedAt,
            TotalDurationSeconds = doc.TotalDurationSeconds
        };
        entity.SetId(doc.Id);
        return entity;
    }

    public RoutineCommandDocument ToDocument(RoutineCommand entity)
    {
        var doc = new RoutineCommandDocument
        {
            CommandId = entity.CommandId,
            ActuatorCode = entity.ActuatorCode,
            Esp32Id = entity.Esp32Id,
            StatusGeneral = entity.StatusGeneral,
            CreatedAt = entity.CreatedAt,
            ExtendedAt = entity.ExtendedAt,
            FinishedAt = entity.FinishedAt,
            TotalDurationSeconds = entity.TotalDurationSeconds
        };
        doc.SetId(entity.Id);
        return doc;
    }
}