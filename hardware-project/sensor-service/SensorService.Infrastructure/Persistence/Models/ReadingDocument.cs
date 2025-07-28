using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SensorService.Infrastructure.Persistence.Models;

public class ReadingDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; private set; } = default!;

    [BsonElement("sensorId")]
    public string SensorId { get; set; } = default!;

    [BsonElement("variableId")]
    public string VariableId { get; set; } = default!;

    [BsonElement("value")]
    public double Value { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }
    public void SetId(string id) => Id = id;
}
