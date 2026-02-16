namespace BffService.Domain.DTOs;

/// <summary>
/// DTO representing a single data point in an aggregated sensor time series.
/// </summary>
public class AggregateDataPoint
{
    public DateTime Timestamp { get; set; }
    public double Avg { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public int Count { get; set; }
}
