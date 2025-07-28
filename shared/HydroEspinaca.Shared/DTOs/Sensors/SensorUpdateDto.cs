namespace HydroEspinaca.Shared.DTOs.Sensors;

public class SensorUpdateDto
{
    public string PhysicalId { get; set; } = default!;
    public string Location { get; set; } = default!;
    public string Esp32Id { get; set; } = default!;
    public int SamplingFrequency { get; set; }
    public List<string> Variables { get; set; } = new();
    public string Status { get; set; } = default!;
}
