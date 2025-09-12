namespace HydroEspinaca.Shared.DTOs.Variables;

public class VariableDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Unit { get; set; } = default!;
    public string Description { get; set; } = default!;
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public string Type { get; set; } = default!;
    public DateTime LastModified { get; set; }
}
