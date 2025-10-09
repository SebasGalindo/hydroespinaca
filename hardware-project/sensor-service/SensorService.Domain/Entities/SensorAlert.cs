namespace SensorService.Domain.Entities;
public class SensorAlert : AlertBase
{
    public string SensorId { get; set; } = default!;
    public string VariableId { get; set; } = default!;
    public double Value { get; set; }
    public double Threshold { get; set; }
    public int Count { get; set; } = 1;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public double? LatestValue { get; set; }
    public string? ResolutionReason { get; set; }
}