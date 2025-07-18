namespace SensorService.Application.DTOs.Alert;

public class SensorAlertUpdateDto
{
    public string Id { get; set; } = default!;
    public bool Acknowledged { get; set; }
}

