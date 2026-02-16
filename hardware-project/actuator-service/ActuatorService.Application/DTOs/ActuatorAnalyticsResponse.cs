namespace ActuatorService.Application.DTOs;

/// <summary>
/// Response DTO containing actuator usage analytics (activation counts, total durations, etc.).
/// </summary>
public record ActuatorAnalyticsResponse
{
    public List<TimelineItem> Timeline { get; init; } = new();
    public List<TotalDurationItem> TotalDurationByActuator { get; init; } = new();
    public List<ActiveTimeProportionItem> ActiveTimeProportion { get; init; } = new();
}

public record TimelineItem
{
    public DateTime Timestamp { get; init; }
    public string ActuatorCode { get; init; } = string.Empty;
    public double TotalDurationSeconds { get; init; }
    public int ActivationCount { get; init; }
}

public record TotalDurationItem
{
    public string ActuatorCode { get; init; } = string.Empty;
    public double TotalDurationSeconds { get; init; }
    public int ActivationCount { get; init; }
}

public record ActiveTimeProportionItem
{
    public string ActuatorCode { get; init; } = string.Empty;
    public double Percentage { get; init; }
}
