using HydroEspinaca.Shared.Enums;
namespace SensorService.Domain.Entities;

public class Sensor
{
    public string Id { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string PhysicalId { get; set; } = default!;
    public string Location { get; set; } = default!;
    public string Esp32Id { get; set; } = default!;
    public SensorStatus Status { get; set; } = SensorStatus.Active;
    public int SamplingFrequency { get; set; } // en segundos
    public List<string> Variables { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
