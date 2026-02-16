namespace BffService.Domain.DTOs.Fuzzy;

/// <summary>
/// Detailed view of a fuzzy system with all its related entities.
/// Used by the BFF to orchestrate data from multiple fuzzy-service endpoints.
/// </summary>
public class FuzzySystemDetailDto
{
    public FuzzySystemDto System { get; set; } = new();
    public List<FuzzyVariableDto> Variables { get; set; } = new();
    public List<FuzzyTermDto> Terms { get; set; } = new();
    public List<FuzzyRuleDto> Rules { get; set; } = new();
}
