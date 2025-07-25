using HydroEspinaca.Shared.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SensorService.Infrastructure.Persistence.Models;

public class Esp32NodeDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = default!;

    [BsonElement("name")]
    public string Name { get; set; } = default!;

    [BsonElement("location")]
    public string Location { get; set; } = default!;

    [BsonElement("lastSeen")]
    public DateTime LastSeen { get; set; }

    [BsonElement("status")]
    public Esp32Status Status { get; set; }
}
