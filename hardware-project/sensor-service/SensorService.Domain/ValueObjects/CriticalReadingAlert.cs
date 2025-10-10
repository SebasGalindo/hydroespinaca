namespace SensorService.Domain.ValueObjects;

public record CriticalReadingAlert
{
    public string Name { get; init; } = default!;
    public double Value { get; init; }
    public string Threshold { get; init; } = default!;
    public bool IsAlert { get; init; }
}

/// <summary>
/// Represents a contextual (non-alert) reading for informational purposes
/// </summary>
public record ContextualReading
{
    public string Name { get; init; } = default!;
    public double Value { get; init; }
    public string Unit { get; init; } = default!;
}

public record CriticalAlertData
{
    public string Esp32Id { get; init; } = default!;
    public DateTime Timestamp { get; init; }
    public IReadOnlyList<CriticalReadingAlert> ManualReadings { get; init; } = Array.Empty<CriticalReadingAlert>();

    /// <summary>
    /// Contextual readings from automatic sensors (temperature, humidity, etc.)
    /// These are NOT alerts, just informational context
    /// </summary>
    public IReadOnlyList<ContextualReading> ContextualReadings { get; init; } = Array.Empty<ContextualReading>();

    public bool HasAnyAlert => ManualReadings.Any(r => r.IsAlert);
    public IEnumerable<CriticalReadingAlert> AlertReadings => ManualReadings.Where(r => r.IsAlert);
}