using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces;

/// <summary>
/// Orchestrates sending a notification to a user across multiple channels
/// based on their preferences.
/// </summary>
public interface INotificationDispatcher
{
    /// <summary>
    /// Sends to all channels the user has enabled, based on their preferences.
    /// Returns results per channel attempted.
    /// </summary>
    /// <param name="userId">The ID of the user to send the notification to.</param>
    /// <param name="templateKey">The key identifying the notification template to use (e.g., "daily_summary").</param>
    /// <param name="title">The title of the notification (used in channels that support it, like push/web push).</param>
    /// <param name="body">The body/content of the notification.</param>
    /// <param name="data">Optional dictionary of additional data to include with the notification (e.g., for deep linking in mobile apps).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of NotificationSendResult, one per channel attempted, indicating success/failure and any relevant info.</returns>
    Task<IEnumerable<NotificationSendResult>> DispatchAsync(
        string userId,
        string templateKey,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        List<string>? allowedChannels = null,
        CancellationToken ct = default);
}
