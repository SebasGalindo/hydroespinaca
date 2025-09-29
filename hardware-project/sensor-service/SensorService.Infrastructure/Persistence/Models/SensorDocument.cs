using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SensorService.Infrastructure.Persistence.Models;

public class SensorDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; private set; } = default!;

    [BsonElement("code")]
    public string Code { get; set; } = default!;

    [BsonElement("physicalId")]
    public string PhysicalId { get; set; } = default!;

    [BsonElement("location")]
    public string Location { get; set; } = default!;

    [BsonElement("esp32Id")]
    public string Esp32Id { get; set; } = default!;

    [BsonElement("samplingFrequency")]
    public int SamplingFrequency { get; set; }

    [BsonElement("variables")]
    public List<string> Variables { get; set; } = new();

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public SensorStatus Status { get; set; } = default!;

    [BsonElement("allowMissing")]
    public bool AllowMissing { get; set; } = false;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }
    public void SetId(string id) => Id = id;
}
