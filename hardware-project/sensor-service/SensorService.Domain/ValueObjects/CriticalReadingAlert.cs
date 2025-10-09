namespace SensorService.Domain.ValueObjects;

public record CriticalReadingAlert
{
    public string Name { get; init; } = default!;
    public double Value { get; init; }
    public string Threshold { get; init; } = default!;
    public bool IsAlert { get; init; }
}

public record CriticalAlertData
{
    public string Esp32Id { get; init; } = default!;
    public DateTime Timestamp { get; init; }
    public IReadOnlyList<CriticalReadingAlert> ManualReadings { get; init; } = Array.Empty<CriticalReadingAlert>();
    
    public bool HasAnyAlert => ManualReadings.Any(r => r.IsAlert);
    public IEnumerable<CriticalReadingAlert> AlertReadings => ManualReadings.Where(r => r.IsAlert);
}