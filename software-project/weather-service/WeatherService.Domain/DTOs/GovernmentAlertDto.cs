namespace WeatherService.Domain.DTOs;

/// <summary>
/// Government weather alert from One Call 3.0 (e.g., IDEAM for Colombia)
/// </summary>
public class GovernmentAlertDto
{
    /// <summary>
    /// Alert sender (e.g., "IDEAM")
    /// </summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>
    /// Alert event name (e.g., "Alerta por lluvias")
    /// </summary>
    public string Event { get; set; } = string.Empty;

    /// <summary>
    /// Alert start time (UTC)
    /// </summary>
    public DateTime Start { get; set; }

    /// <summary>
    /// Alert end time (UTC)
    /// </summary>
    public DateTime End { get; set; }

    /// <summary>
    /// Full description of the alert
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Alert tags/categories
    /// </summary>
    public List<string> Tags { get; set; } = new();
}
