using BiService.Domain.Entities;
using BiService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo.Interfaces;

namespace BiService.Infrastructure.Persistence.Mappings;

/// <summary>
/// Mapper bidireccional entre la entidad de dominio <see cref="ManualConsumptionEntry"/> y su documento MongoDB <see cref="ManualConsumptionEntryDocument"/>.
/// </summary>
public class ManualConsumptionEntryMapper : IEntityMapper<ManualConsumptionEntry, ManualConsumptionEntryDocument>
{
    /// <summary>
    /// Convierte un documento MongoDB en la entidad de dominio <see cref="ManualConsumptionEntry"/>.
    /// </summary>
    /// <param name="doc">Documento de persistencia a convertir.</param>
    /// <returns>Entidad de dominio con los datos del documento.</returns>
    public ManualConsumptionEntry ToEntity(ManualConsumptionEntryDocument doc)
    {
        var entity = new ManualConsumptionEntry
        {
            Date = doc.Date,
            Type = doc.Type,
            Amount = doc.Amount,
            UnitCostSnapshot = doc.UnitCostSnapshot,
            CurrencySnapshot = doc.CurrencySnapshot,
            CostConfigVersionId = doc.CostConfigVersionId,
            CostAmount = doc.CostAmount,
            Note = doc.Note,
            CreatedAt = doc.CreatedAt,
            CreatedByUserId = doc.CreatedByUserId
        };

        entity.SetId(doc.Id);
        return entity;
    }

    /// <summary>
    /// Convierte una entidad de dominio <see cref="ManualConsumptionEntry"/> en su documento MongoDB.
    /// </summary>
    /// <param name="entity">Entidad de dominio a convertir.</param>
    /// <returns>Documento de persistencia listo para almacenar en MongoDB.</returns>
    public ManualConsumptionEntryDocument ToDocument(ManualConsumptionEntry entity)
    {
        return new ManualConsumptionEntryDocument
        {
            Id = entity.Id,
            Date = entity.Date,
            Type = entity.Type,
            Amount = entity.Amount,
            UnitCostSnapshot = entity.UnitCostSnapshot,
            CurrencySnapshot = entity.CurrencySnapshot,
            CostConfigVersionId = entity.CostConfigVersionId,
            CostAmount = entity.CostAmount,
            Note = entity.Note,
            CreatedAt = entity.CreatedAt,
            CreatedByUserId = entity.CreatedByUserId
        };
    }
}
