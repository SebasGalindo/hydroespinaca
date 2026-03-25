namespace HydroEspinaca.Shared.DTOs.Actuator;
public class UpdateActuatorDto
{
    public string Code { get; set; } = default!;
    public string Mode { get; set; } = default!;
    public string Pin { get; set; } = default!;
    public string Location { get; set; } = default!;
    public string Status { get; set; } = default!;
    public decimal PowerConsumptionWatts { get; set; }
}
