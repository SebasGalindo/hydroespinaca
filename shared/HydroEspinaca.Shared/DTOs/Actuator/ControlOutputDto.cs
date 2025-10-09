namespace HydroEspinaca.Shared.DTOs.Actuator;

public class ControlOutputDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Unit { get; set; }
    public string ActuatorId { get; set; } = default!;
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public DateTime LastModified { get; set; }
}
