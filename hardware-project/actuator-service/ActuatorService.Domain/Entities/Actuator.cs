using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;

namespace ActuatorService.Domain.Entities;
public class Actuator : IIdentifiableMutable
{
    public string Id { get; private set; } = default!;          // e.g., "led-001"
    public string Esp32Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public ActuatorType Type { get; set; }
    public string PhysicalId { get; set; } = default!;   // ID físico, e.g. "LED-A1"
    public string Pin { get; set; } = default!;
    public string Location { get; set; } = default!;
    public ActuatorStatus Status { get; set; } = ActuatorStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public void SetId(string id) => Id = id;
}