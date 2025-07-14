namespace SensorService.Application.DTOs;
public class Esp32NodeDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Location { get; set; } = default!;
    public DateTime LastSeen { get; set; }
    public string Status { get; set; } = default!;
}
