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
            VariableCode = doc.VariableCode,
            Value = doc.Value,
            LastSeen = doc.LastSeen,
            LatestValue = doc.LatestValue,
            Timestamp = doc.Timestamp,
            Message = doc.Message,
            Acknowledged = doc.Acknowledged,
            ResolvedAt = doc.ResolvedAt,
            EmailSentAt = doc.EmailSentAt,
            Type = doc.Type,
            Severity = doc.Severity
        };
        entity.SetId(doc.Id);
        return entity;
    }

    public SensorAlertDocument ToDocument(SensorAlert entity)
    {
        var document = new SensorAlertDocument
        {
            VariableCode = entity.VariableCode,
            Value = entity.Value,
            LastSeen = entity.LastSeen,
            LatestValue = entity.LatestValue,
            Timestamp = entity.Timestamp,
            Message = entity.Message,
            Acknowledged = entity.Acknowledged,
            ResolvedAt = entity.ResolvedAt,
            EmailSentAt = entity.EmailSentAt,
            Type = entity.Type,
            Severity = entity.Severity
        };
        document.SetId(entity.Id);
        return document;
    }
}
