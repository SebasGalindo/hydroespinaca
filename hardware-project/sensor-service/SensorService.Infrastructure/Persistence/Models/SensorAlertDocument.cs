using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SensorService.Infrastructure.Persistence.Models;
public class SensorAlertDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; private set; } = default!;

    [BsonElement("sensorId")]
    public string SensorId { get; set; } = default!;

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
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
    [BsonRepresentation(BsonType.String)]
    public AlertSeverity Severity { get; set; }

    [BsonElement("acknowledged")]
    public bool Acknowledged { get; set; } = false;

    [BsonElement("resolvedAt")]
    public DateTime? ResolvedAt { get; set; }

    public void SetId(string id) => Id = id;
}
