using HydroEspinaca.Shared.Mongo.Interfaces;
using SensorService.Domain.Entities;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Mappers;

public class ReadingMapper : IEntityMapper<Reading, ReadingDocument>
{
    public Reading ToEntity(ReadingDocument doc) => new()
    {
        Id = doc.Id,
        SensorId = doc.SensorId,
        VariableId = doc.VariableId,
        Value = doc.Value,
        Timestamp = doc.Timestamp
    };

    public ReadingDocument ToDocument(Reading entity) => new()
    {
        Id = entity.Id,
        SensorId = entity.SensorId,
        VariableId = entity.VariableId,
        Value = entity.Value,
        Timestamp = entity.Timestamp
    };
}
