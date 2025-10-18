namespace HydroEspinaca.Shared.DTOs.Analytics;

/// <summary>
/// Base request for analytics aggregation with time range and granularity
/// </summary>
public record TimeRangeAnalyticsRequest(
    DateTime StartDate,
    DateTime EndDate,
    string View = "daily" // hourly | daily | weekly | monthly
);
