namespace SensorService.Application.DTOs.Aggregate;
public class AggregateDto
{
    public string SensorCode { get; set; } = default!;
    public string VariableCode { get; set; } = default!;
    public double Avg { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public int Count { get; set; }
    public DateTime Timestamp { get; set; }
}