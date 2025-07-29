namespace HydroEspinaca.Shared.DTOs.Alerts;

public class Esp32AlertDto
{
    public string Id { get; set; } = default!;
    public string Esp32Id { get; set; } = default!;
    public string Type { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = default!;
    public string Severity { get; set; } = default!;
    public bool Acknowledged { get; set; }
}
