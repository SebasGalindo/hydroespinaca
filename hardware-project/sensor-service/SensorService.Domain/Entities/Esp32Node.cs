namespace SensorService.Domain.Entities;

public class Esp32Node
{
    public string Id { get; set; } = default!; // ID = esp32-001
    public string Name { get; set; } = default!; // Optional display name
    public string Location { get; set; } = default!;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "active"; // active, offline, maintenance
}
