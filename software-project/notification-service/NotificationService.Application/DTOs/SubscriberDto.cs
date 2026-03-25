namespace NotificationService.Application.DTOs;

public record SubscriberDto(
    string UserId,
    List<string> EnabledChannels,
    string? Email,
    string? Phone
);
