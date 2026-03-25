using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SensorService.Infrastructure.Persistence.Models;

/// <summary>
/// Documento MongoDB que representa un nodo ESP32 del sistema hidropónico IoT.
/// Almacena información del dispositivo como nombre, ubicación, estado y métricas de salud.
/// </summary>
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

    [BsonElement("uptime")]
    public long Uptime { get; set; } = 0;

    [BsonElement("freeHeap")] 
    public long FreeHeap { get; set; } = 0;

    public void SetId(string id) => Id = id;
}
