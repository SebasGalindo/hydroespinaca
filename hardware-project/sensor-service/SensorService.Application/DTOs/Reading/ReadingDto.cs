namespace SensorService.Application.DTOs.Reading;

public class ReadingDto
{
    public string SensorCode { get; set; } = default!;
    public string VariableCode { get; set; } = default!;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
}
