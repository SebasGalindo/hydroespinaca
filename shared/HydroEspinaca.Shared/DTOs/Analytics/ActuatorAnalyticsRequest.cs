namespace HydroEspinaca.Shared.DTOs.Analytics;

/// <summary>
/// Request for actuator analytics aggregation
/// </summary>
public record ActuatorAnalyticsRequest : TimeRangeAnalyticsRequest
{
    public ActuatorAnalyticsRequest() : base(default, default) { }

    public ActuatorAnalyticsRequest(DateTime startDate, DateTime endDate, string view = "daily")
        : base(startDate, endDate, view) { }
}
