namespace WeatherService.Domain.Entities;

/// <summary>
/// Individual alert threshold within a WeatherAlertConfig.
/// </summary>
public class AlertThreshold
{
    /// <summary>
    /// Alert type identifier
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Whether this threshold is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Numeric threshold value (null for binary types like thunderstorm)
    /// </summary>
    public double? ThresholdValue { get; set; }

    /// <summary>
    /// Comparison operator: "gt" (greater than) or "lt" (less than). Null for binary types.
    /// </summary>
    public string? Comparison { get; set; }

    /// <summary>
    /// Recommendation text shown to the user when alert triggers
    /// </summary>
    public string Recommendation { get; set; } = string.Empty;
}
