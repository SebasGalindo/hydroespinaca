using HydroEspinaca.Shared.Enums;

public class ActuatorDto
{
    public string Id { get; set; } = default!;
    public string Esp32Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public ActuatorType Type { get; set; } = default!;
    public string PhysicalId { get; set; } = default!;
    public string Pin { get; set; } = default!;
    public string Location { get; set; } = default!;
    public ActuatorStatus Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
