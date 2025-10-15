namespace HydroEspinaca.Shared.DTOs.Readings;

public class EnrichedReadingItemDto
{
    public string Name { get; set; } = default!;
    public double Value { get; set; }
    public string Unit { get; set; } = default!;
    public double OptimalMin { get; set; }
    public double? OptimalMax { get; set; }
}
