using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SensorService.Infrastructure.Persistence.Models;


public class AggregateDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; private set; } = default!;

    [BsonElement("sensorCode")]
    public string SensorCode { get; set; } = default!;

    [BsonElement("variableCode")]
    public string VariableCode { get; set; } = default!;

    [BsonElement("avg")]
    public double Avg { get; set; }

    [BsonElement("min")]
    public double Min { get; set; }

    [BsonElement("max")]
    public double Max { get; set; }

    [BsonElement("count")]
    public int Count { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    public void SetId(string id) => Id = id;
}
