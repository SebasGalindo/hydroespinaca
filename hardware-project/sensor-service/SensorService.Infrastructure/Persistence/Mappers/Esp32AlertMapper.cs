using HydroEspinaca.Shared.Mongo.Interfaces;
using SensorService.Domain.Entities;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Mappers;

/// <summary>
/// Mapper que convierte entre la entidad de dominio <see cref="Esp32Alert"/> y el documento MongoDB <see cref="Esp32AlertDocument"/>.
/// </summary>
public class Esp32AlertMapper : IEntityMapper<Esp32Alert, Esp32AlertDocument>
{
    /// <summary>
    /// Convierte un documento MongoDB de alerta ESP32 a su entidad de dominio correspondiente.
    /// </summary>
    /// <param name="doc">El documento MongoDB de alerta ESP32.</param>
    /// <returns>La entidad de dominio <see cref="Esp32Alert"/>.</returns>
    public Esp32Alert ToEntity(Esp32AlertDocument doc)
    {
        var entity = new Esp32Alert
        {
            Esp32Id = doc.Esp32Id,
            Timestamp = doc.Timestamp,
            Message = doc.Message,
            Acknowledged = doc.Acknowledged,
            ResolvedAt = doc.ResolvedAt,
            EmailSentAt = doc.EmailSentAt
        };
        entity.SetId(doc.Id);
        return entity;
    }

    /// <summary>
    /// Convierte una entidad de dominio de alerta ESP32 a su documento MongoDB correspondiente.
    /// </summary>
    /// <param name="entity">La entidad de dominio <see cref="Esp32Alert"/>.</param>
    /// <returns>El documento MongoDB <see cref="Esp32AlertDocument"/>.</returns>
    public Esp32AlertDocument ToDocument(Esp32Alert entity)
    {
        var document = new Esp32AlertDocument
        {
            Esp32Id = entity.Esp32Id,
            Timestamp = entity.Timestamp,
            Message = entity.Message,
            Acknowledged = entity.Acknowledged,
            ResolvedAt = entity.ResolvedAt,
            EmailSentAt = entity.EmailSentAt
        };
        document.SetId(entity.Id);
        return document;
    }
}
