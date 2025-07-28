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
            Type = doc.Type,
            Value = doc.Value,
            Threshold = doc.Threshold,
            Timestamp = doc.Timestamp,
            Message = doc.Message,
            Severity = doc.Severity,
            Acknowledged = doc.Acknowledged
        };
        entity.SetId(doc.Id);
        return entity;
    }

    public SensorAlertDocument ToDocument(SensorAlert entity)
    {
        var document = new SensorAlertDocument
        {
            SensorId = entity.SensorId,
            Type = entity.Type,
            Value = entity.Value,
            Threshold = entity.Threshold,
            Timestamp = entity.Timestamp,
            Message = entity.Message,
            Severity = entity.Severity,
            Acknowledged = entity.Acknowledged
        };
        document.SetId(entity.Id);
        return document;
    }
}
