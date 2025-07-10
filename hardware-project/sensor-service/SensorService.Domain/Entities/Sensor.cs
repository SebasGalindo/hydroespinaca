namespace SensorService.Domain.Entities;
public class Sensor
{
    public string Id { get; set; } = default!; 
    public string Code { get; set; } = default!; // e.g., snh0016-temp
    public string Type { get; set; } = default!; // temperature, humidity, light, etc.
    public string Unit { get; set; } = default!; // °C, %, lux, etc.
    public string PhysicalId { get; set; } = default!; // SNH0016, etc.
    public string? Location { get; set; } // Optional, e.g., "Módulo 1"
    public int SamplingFrequency { get; set; } // In seconds
    public string Status { get; set; } = "active"; // active, inactive
}
