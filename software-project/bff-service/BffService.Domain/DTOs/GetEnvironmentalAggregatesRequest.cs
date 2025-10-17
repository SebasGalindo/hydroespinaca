namespace BffService.Domain.DTOs;

public class GetEnvironmentalAggregatesRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string View { get; set; } = default!; // "hourly", "daily", "weekly"
}
