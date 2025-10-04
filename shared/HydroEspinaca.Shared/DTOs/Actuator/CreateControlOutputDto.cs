namespace HydroEspinaca.Shared.DTOs.Actuator;

public class CreateControlOutputDto
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Unit { get; set; }
    public string ActuatorId { get; set; } = default!;
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
}
