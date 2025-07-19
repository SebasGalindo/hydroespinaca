namespace SensorService.Application.DTOs.Esp32Node;

public record CheckEsp32StatusResult(
    int TotalEsp32Checked,
    int OfflineEsp32Count,
    int AlertsCreated);

public record Esp32StatusInfo(
    string Esp32Id,
    DateTime LastActivity,
    bool IsOffline,
    double MinutesSinceLastActivity);