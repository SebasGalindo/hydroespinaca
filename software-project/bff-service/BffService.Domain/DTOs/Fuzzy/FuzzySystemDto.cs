using System.Text.Json.Serialization;

namespace BffService.Domain.DTOs.Fuzzy;

/// <summary>
/// Operators configuration for a fuzzy system.
/// </summary>
public class OperatorsConfigDto
{
    public string AndMethod { get; set; } = "min";
    public string OrMethod { get; set; } = "max";
    public string AggregationMethod { get; set; } = "max";
    public string DefuzzificationMethod { get; set; } = "centroid";
}

/// <summary>
/// Represents a complete fuzzy system from the fuzzy-service.
/// </summary>
public class FuzzySystemDto
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "DRAFT";
    public string DefuzzificationMethod { get; set; } = "centroid";
    public OperatorsConfigDto Operators { get; set; } = new();
    public List<string> InputVariableIds { get; set; } = new();
    public List<string> OutputVariableIds { get; set; } = new();
    public List<string> RuleIds { get; set; } = new();
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
