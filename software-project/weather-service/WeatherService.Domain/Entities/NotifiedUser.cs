namespace WeatherService.Domain.Entities;

/// <summary>
/// Record of a user notified about a weather alert.
/// </summary>
public class NotifiedUser
{
    /// Alert ID that was sent
    public required string UserId { get; set; }

    // Channels through which the alert was sent (e.g., ["email", "whatsapp"])
    public List<string> Channels { get; set; } = [];

    // When the alert was sent to the user
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Whether the user has read/acknowledged the alert
    public bool IsRead { get; set; }
}
