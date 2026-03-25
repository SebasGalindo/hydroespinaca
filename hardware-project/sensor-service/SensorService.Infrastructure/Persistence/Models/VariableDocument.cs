using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SensorService.Infrastructure.Persistence.Models;

/// <summary>
/// Documento MongoDB que representa una variable ambiental o de cultivo hidropónico.
/// Define los rangos físicos y óptimos, la unidad de medida y el tipo de regulación.
/// </summary>
public class VariableDocument : IIdentifiableMutable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; private set; } = default!;

    [BsonElement("code")]
    public string Code { get; set; } = default!;

    [BsonElement("name")]
    public string Name { get; set; } = default!;

    [BsonElement("unit")]
    public string Unit { get; set; } = default!;

    [BsonElement("description")]
    public string Description { get; set; } = default!;

    [BsonElement("physicalMin")]
    public double PhysicalMin { get; set; }

    [BsonElement("physicalMax")]
    public double PhysicalMax { get; set; }

    [BsonElement("optimalMin")]
    public double OptimalMin { get; set; }

    [BsonElement("optimalMax")]
    public double? OptimalMax { get; set; }

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public VariableTypes Type { get; set; }

    [BsonElement("regulationType")]
    [BsonRepresentation(BsonType.String)]
    public RegulationType? RegulationType { get; set; }

    [BsonElement("lastModified")]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public void SetId(string id) => Id = id;
}
