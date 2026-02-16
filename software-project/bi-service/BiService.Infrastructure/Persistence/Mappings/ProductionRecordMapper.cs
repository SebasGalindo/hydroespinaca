using BiService.Domain.Entities;
using BiService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo.Interfaces;

namespace BiService.Infrastructure.Persistence.Mappings;

/// <summary>
/// Mapper bidireccional entre la entidad de dominio <see cref="ProductionRecord"/> y su documento MongoDB <see cref="ProductionRecordDocument"/>.
/// </summary>
public class ProductionRecordMapper : IEntityMapper<ProductionRecord, ProductionRecordDocument>
{
    /// <summary>
    /// Convierte un documento MongoDB en la entidad de dominio <see cref="ProductionRecord"/>.
    /// </summary>
    /// <param name="doc">Documento de persistencia a convertir.</param>
    /// <returns>Entidad de dominio con los datos del documento.</returns>
    public ProductionRecord ToEntity(ProductionRecordDocument doc)
    {
        var entity = new ProductionRecord
        {
            CropName = doc.CropName,
            StartDate = doc.StartDate,
            HarvestDate = doc.HarvestDate,
            KilosProduced = doc.KilosProduced,
            PricePerKilo = doc.PricePerKilo,
            Currency = doc.Currency,
            Note = doc.Note,
            CreatedAt = doc.CreatedAt,
            CreatedByUserId = doc.CreatedByUserId
        };
        entity.SetId(doc.Id);
        return entity;
    }

    /// <summary>
    /// Convierte una entidad de dominio <see cref="ProductionRecord"/> en su documento MongoDB.
    /// </summary>
    /// <param name="entity">Entidad de dominio a convertir.</param>
    /// <returns>Documento de persistencia listo para almacenar en MongoDB.</returns>
    public ProductionRecordDocument ToDocument(ProductionRecord entity)
    {
        return new ProductionRecordDocument
        {
            Id = entity.Id,
            CropName = entity.CropName,
            StartDate = entity.StartDate,
            HarvestDate = entity.HarvestDate,
            KilosProduced = entity.KilosProduced,
            PricePerKilo = entity.PricePerKilo,
            Currency = entity.Currency,
            Note = entity.Note,
            CreatedAt = entity.CreatedAt,
            CreatedByUserId = entity.CreatedByUserId
        };
    }
}
