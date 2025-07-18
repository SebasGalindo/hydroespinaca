namespace SensorService.Application.DTOs.Variable;
public class VariableUpdateDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Unit { get; set; } = default!;
    public string Description { get; set; } = default!;
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public string Type { get; set; } = default!;
}