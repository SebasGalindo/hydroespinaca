namespace BffService.Domain.DTOs.Fuzzy;

/// <summary>
/// Represents a fuzzy variable (input or output) in the system.
/// </summary>
public class FuzzyVariableDto
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string VariableType { get; set; } = "input";
    public string? ActuatorType { get; set; }
    public double DefuzzificationThreshold { get; set; } = 50.0;
    public double? UniverseMin { get; set; }
    public double? UniverseMax { get; set; }
    public string? ReferenceCode { get; set; }
    public List<string> Terms { get; set; } = new();
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
