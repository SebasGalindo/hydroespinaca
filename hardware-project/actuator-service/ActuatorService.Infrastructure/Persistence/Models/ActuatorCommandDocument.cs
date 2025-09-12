using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ActuatorService.Infrastructure.Persistence.Models;

public class ActuatorCommandDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonElement("_id")]
    public string Id { get; set; } = default!;

    public string ActuatorId { get; set; } = default!;
    public string Esp32Id { get; set; } = default!;
    public string Action { get; set; } = default!;
    public int? DurationMs { get; set; }
    public string Trigger { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    public bool Acknowledged { get; set; }
    public string? RoutineId { get; set; }
    public int? RoutineStepOrder { get; set; }
    public string? UserId { get; set; }

    public MetadataDocument? Metadata { get; set; }
    public void SetId(string id) => Id = id;
}

public class MetadataDocument
{
    public string Source { get; set; } = default!;
    public string? FuzzyRule { get; set; }

    public Dictionary<string, double>? Inputs { get; set; }
}
