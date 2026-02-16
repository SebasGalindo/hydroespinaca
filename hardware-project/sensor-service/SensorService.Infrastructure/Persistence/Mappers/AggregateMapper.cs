using HydroEspinaca.Shared.Mongo.Interfaces;
using SensorService.Domain.Entities;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Mappers;

/// <summary>
/// Mapper que convierte entre la entidad de dominio <see cref="Aggregate"/> y el documento MongoDB <see cref="AggregateDocument"/>.
/// </summary>
public class AggregateMapper : IEntityMapper<Aggregate, AggregateDocument>
{
    /// <summary>
    /// Convierte un documento MongoDB de agregado a su entidad de dominio correspondiente.
    /// </summary>
    /// <param name="doc">El documento MongoDB de agregado.</param>
    /// <returns>La entidad de dominio <see cref="Aggregate"/>.</returns>
    public Aggregate ToEntity(AggregateDocument doc)
    {
        var Aggregate = new Aggregate
        {
            SensorCode = doc.SensorCode,
            VariableCode = doc.VariableCode,
            Avg = doc.Avg,
            Min = doc.Min,
            Max = doc.Max,
            Count = doc.Count,
            Timestamp = doc.Timestamp
        };
        Aggregate.SetId(doc.Id);
        return Aggregate;
    }

    /// <summary>
    /// Convierte una entidad de dominio de agregado a su documento MongoDB correspondiente.
    /// </summary>
    /// <param name="entity">La entidad de dominio <see cref="Aggregate"/>.</param>
    /// <returns>El documento MongoDB <see cref="AggregateDocument"/>.</returns>
    public AggregateDocument ToDocument(Aggregate entity)
    {
        var document = new AggregateDocument
        {
            SensorCode = entity.SensorCode,
            VariableCode = entity.VariableCode,
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
