namespace HydroEspinaca.Shared.DTOs.Sensors;

public class SensorDto
{
    public string Id { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string PhysicalId { get; set; } = default!;
    public string Location { get; set; } = default!;
    public string Esp32Id { get; set; } = default!;
    public int SamplingFrequency { get; set; }
    public List<string> Variables { get; set; } = new();
    public string Status { get; set; } = default!;
    public bool AllowMissing { get; set; } = false;
    public DateTime CreatedAt { get; set; }
}
