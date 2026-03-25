namespace NotificationService.Application.DTOs;

public record SendMultiChannelNotificationResponse(
    string CorrelationId,
    List<ChannelResult> Results
);

public record ChannelResult(
    string Channel,
    bool Success,
    string? Provider,
    string? Error
);
