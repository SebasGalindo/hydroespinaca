using BiService.Domain.Entities;
using BiService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo.Interfaces;

namespace BiService.Infrastructure.Persistence.Mappings;

/// <summary>
/// Mapper bidireccional entre la entidad de dominio <see cref="CostConfigVersion"/> y su documento MongoDB <see cref="CostConfigVersionDocument"/>.
/// </summary>
public class CostConfigVersionMapper : IEntityMapper<CostConfigVersion, CostConfigVersionDocument>
{
    /// <summary>
    /// Convierte un documento MongoDB en la entidad de dominio <see cref="CostConfigVersion"/>.
    /// </summary>
    /// <param name="doc">Documento de persistencia a convertir.</param>
    /// <returns>Entidad de dominio con los datos del documento.</returns>
    public CostConfigVersion ToEntity(CostConfigVersionDocument doc)
    {
        var entity = new CostConfigVersion
        {
            Currency = doc.Currency,
            ElectricityCostPerKwh = doc.ElectricityCostPerKwh,
            WaterCostPerLiter = doc.WaterCostPerLiter,
            NutrientCostPerLiter = doc.NutrientCostPerLiter,
            EffectiveFrom = doc.EffectiveFrom,
            EffectiveTo = doc.EffectiveTo,
            IsActive = doc.IsActive,
            CreatedAt = doc.CreatedAt,
            CreatedByUserId = doc.CreatedByUserId
        };

        entity.SetId(doc.Id);
        return entity;
    }

    /// <summary>
    /// Convierte una entidad de dominio <see cref="CostConfigVersion"/> en su documento MongoDB.
    /// </summary>
    /// <param name="entity">Entidad de dominio a convertir.</param>
    /// <returns>Documento de persistencia listo para almacenar en MongoDB.</returns>
    public CostConfigVersionDocument ToDocument(CostConfigVersion entity)
    {
        return new CostConfigVersionDocument
        {
            Id = entity.Id,
            Currency = entity.Currency,
            ElectricityCostPerKwh = entity.ElectricityCostPerKwh,
            WaterCostPerLiter = entity.WaterCostPerLiter,
            NutrientCostPerLiter = entity.NutrientCostPerLiter,
            EffectiveFrom = entity.EffectiveFrom,
            EffectiveTo = entity.EffectiveTo,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            CreatedByUserId = entity.CreatedByUserId
        };
    }
}
