using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SensorService.Infrastructure.Persistence.Models;

public class SensorDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

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
    public string Status { get; set; } = default!; // se guarda como string: "Active", "Inactive", etc.

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }
}
