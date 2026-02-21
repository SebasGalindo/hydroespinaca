namespace NotificationService.Domain.Interfaces;

/// <summary>
/// Manages the Quartz-based daily summary schedule for individual users.
/// Called by the UpdatePreferencesHandler to reschedule/remove jobs without service restart.
/// </summary>
public interface IDailySummaryScheduler
{
    /// <summary>
    /// Reschedules (or creates) the daily summary trigger for a user.
    /// </summary>
    /// <param name="userId">The ID of the user to schedule for.</param>
    /// <param name="hour">The hour of the day (0-23) to send the summary.</param>
    /// <param name="minute">The minute of the hour (0-59) to send the summary.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RescheduleAsync(string userId, int hour, int minute, CancellationToken ct = default);

    /// <summary>
    /// Removes the daily summary schedule for a user (when they disable it).
    /// </summary>
    /// <param name="userId">The ID of the user to remove the schedule for.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RemoveAsync(string userId, CancellationToken ct = default);
}
