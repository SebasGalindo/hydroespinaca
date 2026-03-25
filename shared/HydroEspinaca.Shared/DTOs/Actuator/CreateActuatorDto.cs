namespace HydroEspinaca.Shared.DTOs.Actuator;
public class CreateActuatorDto
{
    public string Esp32Id { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Mode { get; set; } = default!;
    public string PhysicalId { get; set; } = default!;
    public string Pin { get; set; } = default!;
    public string Location { get; set; } = default!;
    public decimal PowerConsumptionWatts { get; set; }
}
