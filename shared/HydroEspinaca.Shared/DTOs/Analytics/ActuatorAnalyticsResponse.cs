namespace HydroEspinaca.Shared.DTOs.Analytics;

/// <summary>
/// Response containing actuator analytics data
/// </summary>
public record ActuatorAnalyticsResponse
{
    public List<ActuatorTimelineItem> Timeline { get; init; } = new();
    public List<ActuatorTotalDurationItem> TotalDurationByActuator { get; init; } = new();
    public List<ActuatorActiveTimeProportionItem> ActiveTimeProportion { get; init; } = new();
}

/// <summary>
/// Timeline item for actuator activations by time period
/// </summary>
public record ActuatorTimelineItem
{
    public DateTime Timestamp { get; init; }
    public string ActuatorCode { get; init; } = string.Empty;
    public double TotalDurationSeconds { get; init; }
    public int ActivationCount { get; init; }
}

/// <summary>
/// Total duration aggregated by actuator
/// </summary>
public record ActuatorTotalDurationItem
{
    public string ActuatorCode { get; init; } = string.Empty;
    public double TotalDurationSeconds { get; init; }
    public int ActivationCount { get; init; }
}

/// <summary>
/// Active time proportion (percentage) by actuator
/// </summary>
public record ActuatorActiveTimeProportionItem
{
    public string ActuatorCode { get; init; } = string.Empty;
    public double Percentage { get; init; }
}
