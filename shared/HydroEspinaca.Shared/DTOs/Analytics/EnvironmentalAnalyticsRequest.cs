namespace HydroEspinaca.Shared.DTOs.Analytics;

/// <summary>
/// Request for environmental analytics aggregation
/// </summary>
public record EnvironmentalAnalyticsRequest : TimeRangeAnalyticsRequest
{
    public EnvironmentalAnalyticsRequest() : base(default, default) { }

    public EnvironmentalAnalyticsRequest(DateTime startDate, DateTime endDate, string view = "daily")
        : base(startDate, endDate, view) { }
}
