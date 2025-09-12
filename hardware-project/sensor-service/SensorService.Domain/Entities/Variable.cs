using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;

public class Variable : IIdentifiableMutable
{
    public string Id { get; private set; } = default!;
    public string Name { get; set; } = default!;
    public string Unit { get; set; } = default!;
    public string Description { get; set; } = default!;
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public VariableTypes Type { get; set; }
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public void SetId(string id) => Id = id;
}