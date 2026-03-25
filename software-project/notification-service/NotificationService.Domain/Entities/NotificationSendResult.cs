namespace NotificationService.Domain.Entities;

/// <summary>
/// Result of sending a notification through a specific channel.
/// </summary>
public record NotificationSendResult(
    bool Success,
    string Channel,
    string? Provider,
    string? ProviderMessageId,
    string? Error
);
