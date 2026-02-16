using HydroEspinaca.Shared.Mongo.Interfaces;
using SensorService.Domain.Entities;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Mappers;

/// <summary>
/// Mapper que convierte entre la entidad de dominio <see cref="SensorAlert"/> y el documento MongoDB <see cref="SensorAlertDocument"/>.
/// </summary>
public class SensorAlertMapper : IEntityMapper<SensorAlert, SensorAlertDocument>
{
    /// <summary>
    /// Convierte un documento MongoDB de alerta de sensor a su entidad de dominio correspondiente.
    /// </summary>
    /// <param name="doc">El documento MongoDB de alerta de sensor.</param>
    /// <returns>La entidad de dominio <see cref="SensorAlert"/>.</returns>
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

    /// <summary>
    /// Convierte una entidad de dominio de alerta de sensor a su documento MongoDB correspondiente.
    /// </summary>
    /// <param name="entity">La entidad de dominio <see cref="SensorAlert"/>.</param>
    /// <returns>El documento MongoDB <see cref="SensorAlertDocument"/>.</returns>
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
