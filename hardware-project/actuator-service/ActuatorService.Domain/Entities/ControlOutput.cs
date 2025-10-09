using HydroEspinaca.Shared.Abstractions;

namespace ActuatorService.Domain.Entities;

public class ControlOutput : IIdentifiableMutable
{
    public string Id { get; private set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Unit { get; set; }
    public string ActuatorId { get; set; } = default!;
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public void SetId(string id) => Id = id;
}
