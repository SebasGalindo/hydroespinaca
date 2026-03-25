namespace NotificationService.Application.DTOs;

public record ChannelPreferenceDto(
    string Channel,
    bool Enabled,
    string? Target
);

public record DailySummaryConfigDto(
    bool Enabled,
    int Hour,
    int Minute,
    List<string> Channels,
    bool IncludeFuzzyRules,
    bool IncludeSensorAverages,
    bool IncludeActuatorRuntime,
    bool IncludeWeatherForecast
);

public record WeatherAlertSubscriptionDto(
    bool Enabled,
    string? FuzzySystemId,
    List<string> AlertTypes
);

public record QuietHoursConfigDto(
    bool Enabled,
    int StartHour,
    int EndHour
);
