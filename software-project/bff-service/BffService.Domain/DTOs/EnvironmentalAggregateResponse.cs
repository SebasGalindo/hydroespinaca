namespace BffService.Domain.DTOs;

public class EnvironmentalAggregateResponse
{
    public string VariableCode { get; set; } = default!;
    public string VariableName { get; set; } = default!;
    public AggregateSummary Summary { get; set; } = default!;
    public List<AggregateTrendPoint> Trend { get; set; } = new();
    public List<AggregateVariabilityPoint>? Variability { get; set; }
}
