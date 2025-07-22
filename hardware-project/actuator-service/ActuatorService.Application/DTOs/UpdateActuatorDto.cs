namespace ActuatorService.Application.DTOs;
public class UpdateActuatorDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Pin { get; set; } = default!;
    public string Location { get; set; } = default!;
    public string Status { get; set; } = default!;
}
