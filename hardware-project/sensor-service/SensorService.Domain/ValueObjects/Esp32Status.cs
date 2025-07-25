namespace SensorService.Domain.ValueObjects;

public record Esp32StatusRecord
{
    public Esp32Id Esp32Id { get; }
    public DateTime LastActivity { get; }
    public bool IsOffline { get; }
    public TimeSpan TimeSinceLastActivity { get; }

    private Esp32StatusRecord(Esp32Id esp32Id, DateTime lastActivity, bool isOffline, TimeSpan timeSinceLastActivity)
    {
        Esp32Id = esp32Id;
        LastActivity = lastActivity;
        IsOffline = isOffline;
        TimeSinceLastActivity = timeSinceLastActivity;
    }

    public static Esp32StatusRecord Create(
        Esp32Id esp32Id,
        DateTime lastActivity,
        DateTime currentTime,
        OfflineThreshold threshold)
    {
        var timeSinceLastActivity = currentTime - lastActivity;
        var isOffline = threshold.IsOffline(lastActivity, currentTime);

        return new Esp32StatusRecord(esp32Id, lastActivity, isOffline, timeSinceLastActivity);
    }
}