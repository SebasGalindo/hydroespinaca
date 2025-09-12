namespace HydroEspinaca.Shared.DTOs.Actuator;
public class UpdateActuatorDto
{
    public string Name { get; set; } = default!;
    public string Pin { get; set; } = default!;
    public string Location { get; set; } = default!;
    public string Status { get; set; } = default!;
}
