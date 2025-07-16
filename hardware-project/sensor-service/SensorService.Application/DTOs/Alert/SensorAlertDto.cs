namespace SensorService.Application.DTOs.Alert;
public class SensorAlertDto
{
    public string Id { get; set; } = default!;
    public string SensorId { get; set; } = default!;
    public string Type { get; set; } = default!;
    public double Value { get; set; }
    public double Threshold { get; set; }
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = default!;
    public string Severity { get; set; } = default!;
    public bool Acknowledged { get; set; }
}