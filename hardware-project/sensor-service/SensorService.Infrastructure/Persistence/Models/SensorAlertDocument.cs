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

    [BsonElement("variableCode")]
    public string VariableCode { get; set; } = default!;

    [BsonElement("value")]
    public double Value { get; set; }

    [BsonElement("lastSeen")]
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    [BsonElement("latestValue")]
    public double? LatestValue { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("message")]
    public string Message { get; set; } = default!;

    [BsonElement("acknowledged")]
    public bool Acknowledged { get; set; } = false;

    [BsonElement("resolvedAt")]
    public DateTime? ResolvedAt { get; set; }

    [BsonElement("emailSentAt")]
    public DateTime? EmailSentAt { get; set; }

    // Legacy fields (not used for new sensor alerts, kept for backward compatibility)
    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    [BsonIgnoreIfNull]
    public AlertType? Type { get; set; }

    [BsonElement("severity")]
    [BsonRepresentation(BsonType.String)]
    [BsonIgnoreIfNull]
    public AlertSeverity? Severity { get; set; }

    public void SetId(string id) => Id = id;
}
