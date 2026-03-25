using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BiService.Infrastructure.Persistence.Models;

/// <summary>
/// Documento MongoDB que representa un registro de producción de cultivo en la colección de persistencia.
/// </summary>
public class ProductionRecordDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("crop_name")]
    public string CropName { get; set; } = string.Empty;

    [BsonElement("start_date")]
    public DateTime StartDate { get; set; }

    [BsonElement("harvest_date")]
    public DateTime HarvestDate { get; set; }

    [BsonElement("kilos_produced")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal KilosProduced { get; set; }

    [BsonElement("price_per_kilo")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal PricePerKilo { get; set; }

    [BsonElement("currency")]
    public string Currency { get; set; } = "COP";

    [BsonElement("note")]
    [BsonIgnoreIfNull]
    public string? Note { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("created_by_user_id")]
    public string CreatedByUserId { get; set; } = string.Empty;

    public void SetId(string id) => Id = id;
}
