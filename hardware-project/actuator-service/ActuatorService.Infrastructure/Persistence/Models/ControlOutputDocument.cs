using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ActuatorService.Infrastructure.Persistence.Models;

public class ControlOutputDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; private set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Unit { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string ActuatorId { get; set; } = default!;

    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public DateTime LastModified { get; set; }

    public void SetId(string id) => Id = id;
}
