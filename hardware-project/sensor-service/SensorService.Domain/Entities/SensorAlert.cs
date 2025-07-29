namespace SensorService.Domain.Entities;
public class SensorAlert : AlertBase
{
    public string SensorId { get; set; } = default!;
    public double Value { get; set; }
    public double Threshold { get; set; }
}