using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ActuatorService.Infrastructure.Persistence.Models;

[BsonIgnoreExtraElements]
public class InternalRoutineDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonElement("name")]
    public string Name { get; set; } = default!;

    [BsonElement("description")]
    public string Description { get; set; } = default!;

    [BsonElement("esp32_id")]
    public string Esp32Id { get; set; } = default!;

    [BsonElement("interval")]
    public TimeSpan Interval { get; set; }

    [BsonElement("steps")]
    public List<InternalRoutineStepDocument> Steps { get; set; } = new();

    [BsonElement("is_active")]
    public bool IsActive { get; set; } = true;

    [BsonElement("created_at")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public void SetId(string id) => Id = id;
}

[BsonIgnoreExtraElements]
public class InternalRoutineStepDocument
{
    [BsonElement("output_variable")]
    public string OutputVariable { get; set; } = default!;

    [BsonElement("power")]
    public string? Power { get; set; }

    [BsonElement("duration")]
    public double Duration { get; set; }

    [BsonElement("duty_cycle")]
    public double? DutyCycle { get; set; }

    [BsonElement("mode")]
    public string? Mode { get; set; }
}
