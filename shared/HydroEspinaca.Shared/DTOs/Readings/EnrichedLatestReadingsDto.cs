namespace HydroEspinaca.Shared.DTOs.Readings;

public class EnrichedLatestReadingsDto
{
    public DateTime Timestamp { get; set; }
    public List<EnrichedReadingItemDto> Readings { get; set; } = new();
}
