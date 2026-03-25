using SensorService.Domain.Entities;
using SensorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo.Interfaces;

namespace SensorService.Infrastructure.Persistence.Mappers;

/// <summary>
/// Mapper que convierte entre la entidad de dominio <see cref="Esp32Node"/> y el documento MongoDB <see cref="Esp32NodeDocument"/>.
/// </summary>
public class Esp32NodeMapper : IEntityMapper<Esp32Node, Esp32NodeDocument>
{
    /// <summary>
    /// Convierte un documento MongoDB de nodo ESP32 a su entidad de dominio correspondiente.
    /// </summary>
    /// <param name="doc">El documento MongoDB de nodo ESP32.</param>
    /// <returns>La entidad de dominio <see cref="Esp32Node"/>.</returns>
    public Esp32Node ToEntity(Esp32NodeDocument doc)
    {
        var entity = new Esp32Node
        {
            Name = doc.Name,
            Location = doc.Location,
            LastSeen = doc.LastSeen,
            Status = doc.Status,
            Uptime = doc.Uptime,
            FreeHeap = doc.FreeHeap
        };
        entity.SetId(doc.Id);
        return entity;
    }

    /// <summary>
    /// Convierte una entidad de dominio de nodo ESP32 a su documento MongoDB correspondiente.
    /// </summary>
    /// <param name="entity">La entidad de dominio <see cref="Esp32Node"/>.</param>
    /// <returns>El documento MongoDB <see cref="Esp32NodeDocument"/>.</returns>
    public Esp32NodeDocument ToDocument(Esp32Node entity)
    {
        var document = new Esp32NodeDocument
        {
            Name = entity.Name,
            Location = entity.Location,
            LastSeen = entity.LastSeen,
            Status = entity.Status,
            Uptime = entity.Uptime,
            FreeHeap = entity.FreeHeap
        };
        document.SetId(entity.Id);
        return document;
    }
}
