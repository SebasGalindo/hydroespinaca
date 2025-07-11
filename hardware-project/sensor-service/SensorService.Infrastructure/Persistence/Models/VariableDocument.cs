using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SensorService.Infrastructure.Persistence.Models;

public class VariableDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)] // Usa el string como id ("lux", "ph", etc.)
    public string Id { get; set; } = default!;

    [BsonElement("name")]
    public string Name { get; set; } = default!;

    [BsonElement("unit")]
    public string Unit { get; set; } = default!;

    [BsonElement("description")]
    public string Description { get; set; } = default!;

    [BsonElement("minValue")]
    public double MinValue { get; set; }

    [BsonElement("maxValue")]
    public double MaxValue { get; set; }

    [BsonElement("type")]
    public string Type { get; set; } = "analog"; // analog, boolean, digital
}
