using BiService.Domain.Enums;
using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BiService.Infrastructure.Persistence.Models;

/// <summary>
/// Documento MongoDB que representa un registro de consumo manual de recurso en la colección de persistencia.
/// </summary>
public class ManualConsumptionEntryDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("date")]
    public DateTime Date { get; set; }

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public ConsumptionType Type { get; set; }

    [BsonElement("amount")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Amount { get; set; }

    [BsonElement("unit_cost_snapshot")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal UnitCostSnapshot { get; set; }

    [BsonElement("currency_snapshot")]
    public string CurrencySnapshot { get; set; } = "COP";

    [BsonElement("cost_config_version_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CostConfigVersionId { get; set; } = string.Empty;

    [BsonElement("cost_amount")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal CostAmount { get; set; }

    [BsonElement("note")]
    [BsonIgnoreIfNull]
    public string? Note { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("created_by_user_id")]
    public string CreatedByUserId { get; set; } = string.Empty;

    public void SetId(string id) => Id = id;
}
