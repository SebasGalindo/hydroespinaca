namespace SensorService.Application.DTOs.Aggregate;

public class EnvironmentalAggregateResponse
{
    public string VariableCode { get; set; } = default!;
    public AggregateSummary Summary { get; set; } = default!;
    public List<AggregateTrendPoint> Trend { get; set; } = new();
    public List<AggregateVariabilityPoint>? Variability { get; set; }
}
