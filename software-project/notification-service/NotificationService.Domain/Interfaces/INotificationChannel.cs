namespace NotificationService.Domain.Interfaces;

using NotificationService.Domain.Entities;

/// <summary>
/// Abstraction for a notification delivery channel (email, push, WhatsApp, web push).
/// Each implementation handles sending via its specific transport.
/// </summary>
public interface INotificationChannel
{
    /// <summary>
    /// Channel identifier: "email", "push", "whatsapp", "web_push".
    /// </summary>
    string ChannelType { get; }

    /// <summary>
    /// Sends a notification message through this channel.
    /// </summary>
    /// <param name="message">The notification message to send.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A NotificationSendResult indicating success/failure and any relevant info.</returns>
    Task<NotificationSendResult> SendAsync(NotificationMessage message, CancellationToken ct = default);

    /// <summary>
    /// Checks if this channel is currently available and configured.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
}
