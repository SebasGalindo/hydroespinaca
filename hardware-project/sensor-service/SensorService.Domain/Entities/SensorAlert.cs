namespace SensorService.Domain.Entities;
public class SensorAlert : AlertBase
{
    public string SensorId { get; set; } = default!;
    public string VariableId { get; set; } = default!;
    public double Value { get; set; }
    public double Threshold { get; set; }
    public int Count { get; set; } = 1;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public double? LatestValue { get; set; }
    public string? ResolutionReason { get; set; }

    /// <summary>
    /// Timestamp when the critical alert email was sent.
    /// null = email not sent yet, DateTime = email sent at this time.
    /// Used to prevent duplicate emails after service restarts.
    /// </summary>
    public DateTime? EmailSentAt { get; set; }
}