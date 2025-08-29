using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ActuatorService.Infrastructure.Persistence.Models;

public class RoutineCommandDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; private set; } = default!;
    public string CommandId { get; set; } = default!;
    public string RoutineId { get; set; } = default!;
    public string StatusGeneral { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public int? Channel { get; set; }
    public List<RoutineResultDocument>? Results { get; set; }
    public List<string>? ExecutionLogs { get; set; }
    
    public void SetId(string id) => Id = id;
}

public class RoutineResultDocument
{
    public string Pin { get; set; } = default!;
    public string Status { get; set; } = default!;
}