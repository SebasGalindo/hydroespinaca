namespace BffService.Domain.DTOs;

/// <summary>
/// Wrapper response for environmental aggregates
/// Matches frontend expected structure: { variables: [...] }
/// </summary>
public class EnvironmentalAggregatesResponse
{
    public List<EnvironmentalAggregateResponse> Variables { get; set; } = new();
}
