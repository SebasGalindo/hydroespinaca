using HydroEspinaca.Shared.Mongo.Interfaces;
using SensorService.Domain.Entities;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Mappers;

public class SensorAlertMapper : IEntityMapper<SensorAlert, SensorAlertDocument>
{
    public SensorAlert ToEntity(SensorAlertDocument doc)
    {
        var entity = new SensorAlert
        {
            SensorId = doc.SensorId,
            VariableId = doc.VariableId,
            Type = doc.Type,
            Value = doc.Value,
            Threshold = doc.Threshold,
            Count = doc.Count,
            LastSeen = doc.LastSeen,
            LatestValue = doc.LatestValue,
            ResolutionReason = doc.ResolutionReason,
            Timestamp = doc.Timestamp,
            Message = doc.Message,
            Severity = doc.Severity,
            Acknowledged = doc.Acknowledged,
            ResolvedAt = doc.ResolvedAt
        };
        entity.SetId(doc.Id);
        return entity;
    }

    public SensorAlertDocument ToDocument(SensorAlert entity)
    {
        var document = new SensorAlertDocument
        {
            SensorId = entity.SensorId,
            VariableId = entity.VariableId,
            Type = entity.Type,
            Value = entity.Value,
            Threshold = entity.Threshold,
            Count = entity.Count,
            LastSeen = entity.LastSeen,
            LatestValue = entity.LatestValue,
            ResolutionReason = entity.ResolutionReason,
            Timestamp = entity.Timestamp,
            Message = entity.Message,
            Severity = entity.Severity,
            Acknowledged = entity.Acknowledged,
            ResolvedAt = entity.ResolvedAt
        };
        document.SetId(entity.Id);
        return document;
    }
}
