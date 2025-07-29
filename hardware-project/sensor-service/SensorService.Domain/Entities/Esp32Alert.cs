namespace SensorService.Domain.Entities;

public class Esp32Alert : AlertBase
{
    public string Esp32Id { get; set; } = default!;
}
