using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ActuatorService.Infrastructure.Persistence.Models;

/// <summary>
/// MongoDB document for routine_commands collection
/// </summary>
public class RoutineCommandDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; private set; } = default!;

    public string CommandId { get; set; } = default!;
    public string ActuatorCode { get; set; } = default!;
    public string Esp32Id { get; set; } = default!;

    [BsonRepresentation(BsonType.String)]
    public RoutineCommandStatus StatusGeneral { get; set; } = default!;

    public DateTime CreatedAt { get; set; }
    public DateTime? ExtendedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public double? TotalDurationSeconds { get; set; }

    public void SetId(string id) => Id = id;
}