using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BiService.Infrastructure.Persistence.Models;

/// <summary>
/// Documento MongoDB que representa una versión de configuración de costos unitarios en la colección de persistencia.
/// </summary>
public class CostConfigVersionDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("currency")]
    public string Currency { get; set; } = "COP";

    [BsonElement("electricity_cost_per_kwh")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal ElectricityCostPerKwh { get; set; }

    [BsonElement("water_cost_per_liter")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal WaterCostPerLiter { get; set; }

    [BsonElement("nutrient_cost_per_liter")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal NutrientCostPerLiter { get; set; }

    [BsonElement("effective_from")]
    public DateTime EffectiveFrom { get; set; }

    [BsonElement("effective_to")]
    [BsonIgnoreIfNull]
    public DateTime? EffectiveTo { get; set; }

    [BsonElement("is_active")]
    public bool IsActive { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("created_by_user_id")]
    public string CreatedByUserId { get; set; } = string.Empty;

    public void SetId(string id) => Id = id;
}
