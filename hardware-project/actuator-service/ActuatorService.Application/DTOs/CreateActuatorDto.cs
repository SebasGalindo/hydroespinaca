namespace ActuatorService.Application.DTOs;
public class CreateActuatorDto
{
    public string Esp32Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string PhysicalId { get; set; } = default!;
    public string Pin { get; set; } = default!;
    public string Location { get; set; } = default!;
}
