namespace SensorService.Application.DTOs;
public class SensorSearchDto
{
    public string? Code { get; set; }
    public string? Location { get; set; }
    public string? Esp32Id { get; set; }
    public string? Status { get; set; }
}