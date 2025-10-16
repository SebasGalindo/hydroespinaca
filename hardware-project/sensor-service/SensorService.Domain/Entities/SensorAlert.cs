namespace SensorService.Domain.Entities;
public class SensorAlert : AlertBase
{
    public string VariableCode { get; set; } = default!;
    public double Value { get; set; }
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public double? LatestValue { get; set; }

    /// <summary>
    /// Timestamp when the critical alert email was sent.
    /// null = email not sent yet, DateTime = email sent at this time.
    /// Used to prevent duplicate emails.
    /// </summary>
    public DateTime? EmailSentAt { get; set; }
}