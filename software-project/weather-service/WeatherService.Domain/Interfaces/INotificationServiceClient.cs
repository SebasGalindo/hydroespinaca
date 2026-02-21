namespace WeatherService.Domain.Interfaces;

/// <summary>
/// Client for communicating with notification-service (weather → notification).
/// Used by the alert evaluation worker to send alerts to subscribed users.
/// </summary>
public interface INotificationServiceClient
{
    /// <summary>
    /// Gets the list of user IDs subscribed to weather alerts for a fuzzy system,
    /// along with their enabled notification channels.
    /// </summary>
    /// <param name="fuzzySystemId">The ID of the fuzzy system to get subscribers for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of subscribers with their notification channels.</returns>
    Task<List<AlertSubscriberDto>> GetSubscribersForFuzzySystemAsync(
        string fuzzySystemId, CancellationToken ct = default);

    /// <summary>
    /// Sends a multi-channel notification to a user.
    /// notification-service will route to the appropriate channels based on user preferences.
    /// </summary>
    /// <param name="userId">The ID of the user to send the notification to.</param>
    /// <param name="templateKey">The key of the notification template to use (e.g. "weather_alert").</param>
    /// <param name="title">The title of the notification (used in push and email).</param>
    /// <param name="body">The body of the notification (used in push and email).</param>
    /// <param name="data">Optional key-value pairs with additional data for the notification (e.g. alert details).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task SendAlertNotificationAsync(
        string userId,
        string templateKey,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default);
}

/// <summary>
/// Subscriber info returned by notification-service
/// </summary>
public class AlertSubscriberDto
{
    public string UserId { get; set; } = string.Empty;
    public List<string> Channels { get; set; } = [];
}
