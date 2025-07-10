using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SensorService.Infrastructure.Persistence.Models;
public class SensorReadingDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonElement("sensorId")]
    public string SensorId { get; set; } = default!;

    [BsonElement("type")]
    public string Type { get; set; } = default!;

    [BsonElement("value")]
    public double Value { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }
}