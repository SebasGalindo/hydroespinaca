using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SensorService.Infrastructure.Persistence.Models;

public class AggregateDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)] // Recomendado usar un ID compuesto tipo "sensor-variable-timestamp"
    public string Id { get; set; } = default!;

    [BsonElement("sensorId")]
    public string SensorId { get; set; } = default!;

    [BsonElement("variableId")]
    public string VariableId { get; set; } = default!;

    [BsonElement("avg")]
    public double Avg { get; set; }

    [BsonElement("min")]
    public double Min { get; set; }

    [BsonElement("max")]
    public double Max { get; set; }

    [BsonElement("count")]
    public int Count { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }
}
