using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ActuatorService.Infrastructure.Persistence.Models;

/// <summary>
/// MongoDB document schema for actuator cooldown state records.
/// </summary>
[BsonIgnoreExtraElements]
public class ActuatorCooldownDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonElement("actuator_id")]
    public string ActuatorId { get; set; } = default!;

    [BsonElement("reason")]
    public string Reason { get; set; } = default!;

    [BsonElement("started_at")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("expires_at")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ExpiresAt { get; set; }

    [BsonElement("is_active")]
    public bool IsActive { get; set; } = true;

    public void SetId(string id) => Id = id;
}
