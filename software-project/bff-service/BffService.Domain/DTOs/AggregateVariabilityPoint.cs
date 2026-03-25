namespace BffService.Domain.DTOs;

/// <summary>
/// DTO representing a variability measurement point for sensor data analysis.
/// </summary>
public class AggregateVariabilityPoint
{
    public DateTime Timestamp { get; set; }
    public double Min { get; set; }
    public double Q1 { get; set; }
    public double Median { get; set; }
    public double Q3 { get; set; }
    public double Max { get; set; }
    public int Count { get; set; }
}
