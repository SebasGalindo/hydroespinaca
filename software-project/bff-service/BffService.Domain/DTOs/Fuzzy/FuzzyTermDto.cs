namespace BffService.Domain.DTOs.Fuzzy;

/// <summary>
/// Represents a fuzzy term (linguistic label) with its membership function.
/// </summary>
public class FuzzyTermDto
{
    public string? Id { get; set; }
    public string? VariableId { get; set; }
    public string Label { get; set; } = string.Empty;
    public MembershipFunctionDto MembershipFunction { get; set; } = new();
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
