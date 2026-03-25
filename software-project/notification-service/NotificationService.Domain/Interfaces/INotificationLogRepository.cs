using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces;

/// <summary>
/// Repository interface for managing notification logs, 
/// which record the history of notifications sent to users.
/// </summary>
public interface INotificationLogRepository
{
    /// <summary>
    /// Creates a new notification log entry in the repository. 
    /// This is typically called when a notification is sent,
    /// </summary>
    /// <param name="log">The notification log entry to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created notification log entry.</returns>
    Task<NotificationLog> CreateAsync(NotificationLog log, CancellationToken ct = default);
    
    /// <summary>
    /// Updates an existing notification log entry in the repository.
    /// This can be used to update the status of a notification (e.g., from "sent" to "delivered" or "failed")
    /// </summary>
    /// <param name="log">The notification log entry to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns></returns>
    Task UpdateAsync(NotificationLog log, CancellationToken ct = default);

    /// <summary>
    /// Gets notification history for a user with optional filters.
    /// </summary>
    /// <param name="userId">The ID of the user to retrieve logs for.</param>
    /// <param name="channel">Optional filter by notification channel (e.g., "email", "push").</param>
    /// <param name="from">Optional filter for logs created after this date/time.</param>
    /// <param name="to">Optional filter for logs created before this date/time.</param>
    /// <param name="limit">Maximum number of logs to return (default 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of notification log entries matching the criteria.</returns>
    Task<IEnumerable<NotificationLog>> GetByUserIdAsync(
        string userId,
        string? channel = null,
        DateTime? from = null,
        DateTime? to = null,
        int limit = 100,
        CancellationToken ct = default);
}
