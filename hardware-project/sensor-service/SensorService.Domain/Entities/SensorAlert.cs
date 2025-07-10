namespace SensorService.Domain.Entities;
public class SensorAlert
{
    public string Id { get; set; } = default!;
    public string SensorId { get; set; } = default!;
    public string Type { get; set; } = default!; // threshold-exceeded, disconnected, etc.
    public double Value { get; set; }
    public double Threshold { get; set; }
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = default!;
    public string Severity { get; set; } = "warning"; // info, warning, critical
    public bool Acknowledged { get; set; } = false;
}
