using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SensorService.Infrastructure.Persistence.Models;
public class SensorDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonElement("code")]
    public string Code { get; set; } = default!; // <- Aquí pones bh1750-001

    [BsonElement("type")]
    public string Type { get; set; } = default!;

    [BsonElement("unit")]
    public string Unit { get; set; } = default!;

    [BsonElement("physicalId")]
    public string PhysicalId { get; set; } = default!;

    [BsonElement("location")]
    public string? Location { get; set; }

    [BsonElement("samplingFrequency")]
    public int SamplingFrequency { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "active";
}
