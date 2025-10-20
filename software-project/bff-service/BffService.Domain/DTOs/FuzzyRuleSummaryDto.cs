namespace BffService.Domain.DTOs;

/// <summary>
/// Summary information for a fuzzy logic rule.
/// Contains basic identification and description of the rule's purpose.
/// </summary>
public class FuzzyRuleSummaryDto
{
    /// <summary>
    /// Unique identifier of the fuzzy rule
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name of the fuzzy rule
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description explaining the rule's logic and purpose
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
