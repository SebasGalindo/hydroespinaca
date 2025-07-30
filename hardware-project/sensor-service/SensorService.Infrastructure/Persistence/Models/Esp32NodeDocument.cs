using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SensorService.Infrastructure.Persistence.Models;

public class Esp32NodeDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; private set; } = default!;
    [BsonElement("name")]
    public string Name { get; set; } = default!;

    [BsonElement("location")]
    public string Location { get; set; } = default!;

    [BsonElement("lastSeen")]
    public DateTime LastSeen { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public Esp32Status Status { get; set; }

    public void SetId(string id) => Id = id;
}
