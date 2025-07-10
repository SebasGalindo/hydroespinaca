namespace SensorService.Domain.Entities;
public class SensorReading
{
    public string Id { get; set; } = default!; // MongoDB ObjectId as string
    public string SensorId { get; set; } = default!; // Link to Sensor.Id
    public string Type { get; set; } = default!; // Copied from Sensor.Type for redundancy
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
}