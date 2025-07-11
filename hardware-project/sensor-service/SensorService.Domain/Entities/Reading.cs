namespace SensorService.Domain.Entities;
public class Reading
{
    public string Id { get; set; } = default!;
    public string SensorId { get; set; } = default!;
    public string VariableId { get; set; } = default!;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}