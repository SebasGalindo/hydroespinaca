namespace SensorService.Domain.Entities;

public class Aggregate
{
    public string Id { get; set; } = default!;
    public string SensorId { get; set; } = default!;
    public string VariableId { get; set; } = default!;
    public double Avg { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public int Count { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
