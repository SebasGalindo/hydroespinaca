using HydroEspinaca.Shared.Mongo.Interfaces;
using SensorService.Domain.Entities;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Mappers;

/// <summary>
/// Mapper que convierte entre la entidad de dominio <see cref="Reading"/> y el documento MongoDB <see cref="ReadingDocument"/>.
/// </summary>
public class ReadingMapper : IEntityMapper<Reading, ReadingDocument>
{
    /// <summary>
    /// Convierte un documento MongoDB de lectura a su entidad de dominio correspondiente.
    /// </summary>
    /// <param name="doc">El documento MongoDB de lectura.</param>
    /// <returns>La entidad de dominio <see cref="Reading"/>.</returns>
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


    /// <summary>
    /// Convierte una entidad de dominio de lectura a su documento MongoDB correspondiente.
    /// </summary>
    /// <param name="entity">La entidad de dominio <see cref="Reading"/>.</param>
    /// <returns>El documento MongoDB <see cref="ReadingDocument"/>.</returns>
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
