namespace SensorService.Domain.Entities;

public class Esp32Alert : AlertBase
{
    public string Esp32Id { get; set; } = default!;

    /// <summary>
    /// Timestamp when the offline alert email was sent.
    /// null = email not sent yet, DateTime = email sent at this time.
    /// Used to prevent duplicate emails.
    /// </summary>
    public DateTime? EmailSentAt { get; set; }
}
