using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces;

/// <summary>
/// Repository interface for managing user notification preferences,
/// which determine how and when users receive notifications.
/// </summary>
public interface INotificationPreferenceRepository
{
    /// <summary>
    /// Gets the notification preferences for a specific user by their ID.
    /// </summary>
    /// <param name="userId">The ID of the user whose preferences are to be retrieved.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The notification preferences for the user, or null if not found.</returns>
    Task<NotificationPreference?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    
    /// <summary>
    /// Creates new notification preferences for a user. This is typically called when a new user is onboarded and default preferences are set up for them.
    /// </summary>
    /// <param name="preference">The notification preference to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created notification preference.</returns>
    Task<NotificationPreference> CreateAsync(NotificationPreference preference, CancellationToken ct = default);
    
    /// <summary>
    /// Updates existing notification preferences for a user. 
    /// This is called when a user updates their preferences through the UI, 
    /// allowing changes to their notification settings to be saved in the repository.
    /// </summary>
    /// <param name="preference">The notification preference to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns></returns>
    Task UpdateAsync(NotificationPreference preference, CancellationToken ct = default);

    /// <summary>
    /// Gets all users subscribed to weather alerts for a specific fuzzy system.
    /// Returns preferences where weather_alerts_subscription.enabled=true
    /// and fuzzy_system_id matches (or is null, meaning "auto-detect active").
    /// </summary>
    /// <param name="fuzzySystemId">The ID of the fuzzy system to find subscribers for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of notification preferences for users subscribed to weather alerts for the specified fuzzy system.</returns>
    Task<IEnumerable<NotificationPreference>> GetSubscribersForFuzzySystemAsync(
        string fuzzySystemId, CancellationToken ct = default);

    /// <summary>
    /// Gets all preferences with daily_summary.enabled=true.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of notification preferences for users subscribed to daily summaries.</returns>
    Task<IEnumerable<NotificationPreference>> GetDailySummarySubscribersAsync(CancellationToken ct = default);
}
