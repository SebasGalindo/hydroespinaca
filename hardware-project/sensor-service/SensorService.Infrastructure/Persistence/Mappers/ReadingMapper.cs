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
            SensorCode = doc.SensorCode,
            VariableCode = doc.VariableCode,
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
            SensorCode = entity.SensorCode,
            VariableCode = entity.VariableCode,
            Value = entity.Value,
            Timestamp = entity.Timestamp
        };
        document.SetId(entity.Id);
        return document;
    }
}
