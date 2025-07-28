using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson.Serialization.Attributes;

namespace ActuatorService.Infrastructure.Persistence.Models;
public class ActuatorDocument : IIdentifiableMutable
{
    [BsonId]
    public string Id { get; set; } = default!;

    public string Esp32Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string PhysicalId { get; set; } = default!;
    public string Pin { get; set; } = default!;
    public string Location { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public void SetId(string id) => Id = id;
}