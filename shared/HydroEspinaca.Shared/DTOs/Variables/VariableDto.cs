namespace HydroEspinaca.Shared.DTOs.Variables;

public class VariableDto
{
    public string Id { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Unit { get; set; } = default!;
    public string Description { get; set; } = default!;
    public double PhysicalMin { get; set; }
    public double PhysicalMax { get; set; }
    public double OptimalMin { get; set; }
    public double? OptimalMax { get; set; }
    public string Type { get; set; } = default!;
    public string? RegulationType { get; set; }
    public DateTime LastModified { get; set; }
}
