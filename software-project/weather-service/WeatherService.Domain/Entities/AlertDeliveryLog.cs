namespace WeatherService.Domain.Entities;

/// <summary>
/// Lightweight record of a delivered alert, used to prevent duplicate notifications
/// when AllowDuplicateAlerts = false on a WeatherAlertConfig.
/// Stored in a Time Series Collection (timeField = SentAt, metaField = FuzzySystemId).
/// </summary>
public class AlertDeliveryLog
{
    public string Id { get; set; } = string.Empty;
    public string FuzzySystemId { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;

    /// <summary>Date only (time stripped) of the forecast day this alert referred to.</summary>
    public DateTime ForecastDate { get; set; }

    public string UserId { get; set; } = string.Empty;

    /// <summary>timeField for the Time Series Collection.</summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
