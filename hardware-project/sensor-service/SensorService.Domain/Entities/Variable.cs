using HydroEspinaca.Shared.Enums;

public class Variable
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Unit { get; set; } = default!;
    public string Description { get; set; } = default!;
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public VariableTypes Type { get; set; }
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

}