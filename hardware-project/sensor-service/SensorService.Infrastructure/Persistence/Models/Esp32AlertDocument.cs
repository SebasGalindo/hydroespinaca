using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SensorService.Infrastructure.Persistence.Models;

public class Esp32AlertDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; private set; } = default!;

    [BsonElement("esp32Id")]
    public string Esp32Id { get; set; } = default!;

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public AlertType Type { get; set; } = AlertType.Esp32Offline;

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("message")]
    public string Message { get; set; } = default!;

    [BsonElement("severity")]
    [BsonRepresentation(BsonType.String)]
    public AlertSeverity Severity { get; set; } = AlertSeverity.Critical;

    [BsonElement("acknowledged")]
    public bool Acknowledged { get; set; } = false;

    public void SetId(string id) => Id = id;
}
