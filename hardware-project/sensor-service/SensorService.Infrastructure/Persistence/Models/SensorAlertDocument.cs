using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using HydroEspinaca.Shared.Enums;

namespace SensorService.Infrastructure.Persistence.Models;
public class SensorAlertDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonElement("sensorId")]
    public string SensorId { get; set; } = default!;

    [BsonElement("type")]
    public AlertType Type { get; set; } = default!;

    [BsonElement("value")]
    public double Value { get; set; }

    [BsonElement("threshold")]
    public double Threshold { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("message")]
    public string Message { get; set; } = default!;

    [BsonElement("severity")]
    public AlertSeverity Severity { get; set; }

    [BsonElement("acknowledged")]
    public bool Acknowledged { get; set; } = false;
}
