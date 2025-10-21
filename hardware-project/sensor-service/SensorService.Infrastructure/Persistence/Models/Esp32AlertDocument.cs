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
    public void SetId(string id) => Id = id;
}
