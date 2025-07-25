using HydroEspinaca.Shared.Enums;

namespace HydroEspinaca.Shared.DTOs.Esp32;

public class Esp32NodeUpdateStatusDto
{
    public Esp32Status Status { get; set; } = default!;
}
