using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;

namespace SensorService.Domain.Entities;

public class Esp32Node : IIdentifiableMutable
{
    public string Id { get; private set; } = default!;
    public string Name { get; set; } = default!; // Optional display name
    public string Location { get; set; } = default!;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public Esp32Status Status { get; set; }

    public void SetId(string id) => Id = id;
}
