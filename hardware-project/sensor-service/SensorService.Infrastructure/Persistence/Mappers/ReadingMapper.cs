using HydroEspinaca.Shared.Mongo.Interfaces;
using SensorService.Domain.Entities;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Mappers;

public class ReadingMapper : IEntityMapper<Reading, ReadingDocument>
{
    public Reading ToEntity(ReadingDocument doc)
    {
        var entity = new Reading
        {
            SensorId = doc.SensorId,
            VariableId = doc.VariableId,
            Value = doc.Value,
            Timestamp = doc.Timestamp
        };
        entity.SetId(doc.Id);
        return entity;
    }


    public ReadingDocument ToDocument(Reading entity)
    {
        var document = new ReadingDocument
        {
            SensorId = entity.SensorId,
            VariableId = entity.VariableId,
            Value = entity.Value,
            Timestamp = entity.Timestamp
        };
        document.SetId(entity.Id);
        return document;
    }
}
