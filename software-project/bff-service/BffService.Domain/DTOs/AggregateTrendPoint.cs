namespace BffService.Domain.DTOs;

/// <summary>
/// DTO representing a single point in a sensor data trend line.
/// </summary>
public class AggregateTrendPoint
{
    public DateTime Timestamp { get; set; }
    public double Avg { get; set; }
}
