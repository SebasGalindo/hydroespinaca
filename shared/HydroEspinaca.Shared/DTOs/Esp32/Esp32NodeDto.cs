using HydroEspinaca.Shared.Enums;

namespace HydroEspinaca.Shared.DTOs.Esp32;

public class Esp32NodeDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Location { get; set; } = default!;
    public DateTime LastSeen { get; set; }
    public Esp32Status Status { get; set; } = default!;
}
