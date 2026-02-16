using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SensorService.Infrastructure.Persistence.Models;

/// <summary>
/// Documento MongoDB que representa una lectura individual de un sensor hidropónico.
/// Contiene el valor medido, el código del sensor, la variable y la marca temporal.
/// </summary>
public class ReadingDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; private set; } = default!;

    [BsonElement("sensorCode")]
    public string SensorCode { get; set; } = default!;

    [BsonElement("variableCode")]
    public string VariableCode { get; set; } = default!;

    [BsonElement("value")]
    public double Value { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }
    public void SetId(string id) => Id = id;
}
