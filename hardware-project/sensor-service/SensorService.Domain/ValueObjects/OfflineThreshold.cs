namespace SensorService.Domain.ValueObjects;

public record OfflineThreshold
{
    public TimeSpan Duration { get; }

    private OfflineThreshold(TimeSpan duration)
    {
        Duration = duration;
    }

    public static OfflineThreshold Create(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            throw new ArgumentException("Offline threshold must be positive", nameof(duration));

        return new OfflineThreshold(duration);
    }

    public static OfflineThreshold FromMinutes(int minutes) => Create(TimeSpan.FromMinutes(minutes));

    public bool IsOffline(DateTime lastActivity, DateTime currentTime)
    {
        return (currentTime - lastActivity) > Duration;
    }

    public static implicit operator TimeSpan(OfflineThreshold threshold) => threshold.Duration;
}