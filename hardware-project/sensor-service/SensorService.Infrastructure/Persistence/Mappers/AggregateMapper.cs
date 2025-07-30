using HydroEspinaca.Shared.Mongo.Interfaces;
using SensorService.Domain.Entities;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Mappers;
public class AggregateMapper : IEntityMapper<Aggregate, AggregateDocument>
{
    public Aggregate ToEntity(AggregateDocument doc)
    {
        var Aggregate = new Aggregate
        {
            SensorId = doc.SensorId,
            VariableId = doc.VariableId,
            Avg = doc.Avg,
            Min = doc.Min,
            Max = doc.Max,
            Count = doc.Count,
            Timestamp = doc.Timestamp
        };
        Aggregate.SetId(doc.Id);
        return Aggregate;
    }

    public AggregateDocument ToDocument(Aggregate entity)
    {
        var document = new AggregateDocument
        {
            SensorId = entity.SensorId,
            VariableId = entity.VariableId,
            Avg = entity.Avg,
            Min = entity.Min,
            Max = entity.Max,
            Count = entity.Count,
            Timestamp = entity.Timestamp
        };
        document.SetId(entity.Id);
        return document;
    }
}
