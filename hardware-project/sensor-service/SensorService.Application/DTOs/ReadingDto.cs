namespace SensorService.Application.DTOs;

public class ReadingDto
{
    public string SensorId { get; set; } = default!;
    public string VariableId { get; set; } = default!;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
}
