using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces;

/// <summary>
/// Repository interface for managing push notification subscriptions,
/// which store the details of how to send push notifications to users (e.g., Expo tokens or Web Push subscriptions).
/// </summary>
public interface IPushSubscriptionRepository
{
    /// <summary>
    /// Creates a new push subscription for a user. This is typically called when a user registers a new device or browser for push notifications.
    /// </summary>
    /// <param name="subscription">The push subscription to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created push subscription.</returns>
    Task<PushSubscription> CreateAsync(PushSubscription subscription, CancellationToken ct = default);
    
    /// <summary>
    /// Deletes a push subscription by its ID.
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the subscription was deleted, false otherwise.</returns>
    Task<bool> DeleteAsync(string subscriptionId, CancellationToken ct = default);
    
    /// <summary>
    /// Gets all push subscriptions for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user whose subscriptions are to be retrieved.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of push subscriptions for the user.</returns>
    Task<IEnumerable<PushSubscription>> GetByUserIdAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all active subscriptions for a user, filtered by platform if specified.
    /// </summary>
    /// <param name="userId">The ID of the user whose active subscriptions are to be retrieved.</param>
    /// <param name="platform">The platform to filter by (e.g., "ios", "android"). If null, all platforms are included.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of active push subscriptions for the user, optionally filtered by platform.</returns>
    Task<IEnumerable<PushSubscription>> GetActiveByUserIdAsync(
        string userId, string? platform = null, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing push subscription. This can be used to update the subscription details (e.g., if the Expo token changes) 
    /// or to update the failure count/status after a send attempt.
    /// </summary>
    /// <param name="subscription">The push subscription to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns></returns>
    Task UpdateAsync(PushSubscription subscription, CancellationToken ct = default);

    /// <summary>
    /// Deactivates subscriptions with failure_count >= maxFailures.
    /// Returns the count of deactivated subscriptions.
    /// </summary>
    /// <param name="maxFailures">The maximum number of allowed failures before deactivation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The count of subscriptions that were deactivated.</returns>
    Task<int> DeactivateFailedSubscriptionsAsync(int maxFailures = 3, CancellationToken ct = default);

    /// <summary>
    /// Deletes inactive subscriptions older than the specified number of days.
    /// Returns the count of deleted subscriptions.
    /// </summary>
    /// <param name="olderThanDays">The age in days after which inactive subscriptions should be deleted (default 90 days).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The count of subscriptions that were deleted.</returns>
    Task<int> CleanupInactiveAsync(int olderThanDays = 90, CancellationToken ct = default);
}
