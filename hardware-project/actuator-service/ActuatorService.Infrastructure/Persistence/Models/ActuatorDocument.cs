using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ActuatorService.Infrastructure.Persistence.Models;
/// <summary>
/// MongoDB document schema for actuator device records.
/// </summary>
public class ActuatorDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; private set; } = default!;
    public string Esp32Id { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Mode { get; set; } = default!;
    public string PhysicalId { get; set; } = default!;
    public string Pin { get; set; } = default!;
    public string Location { get; set; } = default!;
    public string Status { get; set; } = default!;
    [BsonElement("power_consumption_watts")]
    [BsonRepresentation(BsonType.Double)]
    public decimal PowerConsumptionWatts { get; set; }
    public DateTime CreatedAt { get; set; }
    public void SetId(string id) => Id = id;
}