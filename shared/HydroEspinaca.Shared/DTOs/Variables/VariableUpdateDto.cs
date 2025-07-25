using HydroEspinaca.Shared.Enums;

namespace HydroEspinaca.Shared.DTOs.Variables;

public class VariableUpdateDto
{
    public string Name { get; set; } = default!;
    public string Unit { get; set; } = default!;
    public string Description { get; set; } = default!;
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public VariableTypes Type { get; set; } = default!;
}
