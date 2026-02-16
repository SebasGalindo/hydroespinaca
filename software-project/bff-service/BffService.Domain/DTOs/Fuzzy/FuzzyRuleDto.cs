namespace BffService.Domain.DTOs.Fuzzy;

/// <summary>
/// Represents a condition in a fuzzy rule (IF variable IS/IS_NOT term).
/// </summary>
public class ConditionDto
{
    public string VariableId { get; set; } = string.Empty;
    public string Operator { get; set; } = "IS";
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Represents a consequent (THEN part) of a Mamdani fuzzy rule.
/// </summary>
public class RuleConsequentDto
{
    public string VariableId { get; set; } = string.Empty;
    public List<string> Terms { get; set; } = new();
    public string AggregationMethod { get; set; } = "max";
}

/// <summary>
/// Represents a complete fuzzy rule with conditions, connectors and consequents.
/// </summary>
public class FuzzyRuleDto
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SystemId { get; set; }
    public string? Description { get; set; }
    public List<ConditionDto> Conditions { get; set; } = new();
    public List<string> Connectors { get; set; } = new();
    public List<RuleConsequentDto> Consequents { get; set; } = new();
    public DateTime? CreatedAt { get; set; }
    public string? RuleText { get; set; }
}
