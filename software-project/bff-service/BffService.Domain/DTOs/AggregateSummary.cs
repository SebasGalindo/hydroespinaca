namespace BffService.Domain.DTOs;

/// <summary>
/// DTO containing statistical summary of aggregated sensor readings (min, max, avg, etc.).
/// </summary>
public class AggregateSummary
{
    public double Min { get; set; }
    public double Max { get; set; }
    public double Avg { get; set; }
    public int Count { get; set; }
}
