using HydroEspinaca.Shared.Abstractions;

namespace WeatherService.Domain.Entities;

/// <summary>
/// Generated weather alert — created when a forecast matches a threshold.
/// </summary>
public class WeatherAlert : IIdentifiableMutable
{
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Fuzzy system this alert applies to
    /// </summary>
    public required string FuzzySystemId { get; set; }

    /// <summary>
    /// Alert type that triggered (matches AlertThreshold.Type)
    /// </summary>
    public required string AlertType { get; set; }

    /// <summary>
    /// Severity: "warning" or "critical"
    /// </summary>
    public string Severity { get; set; } = "warning";

    /// <summary>
    /// Human-readable title (e.g., "⚠️ Calor extremo pronosticado")
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Descriptive message (e.g., "Se pronostica 37°C para el jueves 22 Feb a las 14:00")
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Recommendation from the threshold config
    /// </summary>
    public string Recommendation { get; set; } = string.Empty;

    /// <summary>
    /// Forecast datetime that triggered the alert
    /// </summary>
    public DateTime ForecastDatetime { get; set; }

    /// <summary>
    /// Actual forecast value (e.g., 37.2 for temperature)
    /// </summary>
    public double? ForecastValue { get; set; }

    /// <summary>
    /// Textual condition (e.g., "Tormenta con lluvia")
    /// </summary>
    public string? ForecastCondition { get; set; }

    /// <summary>
    /// True if this came from IDEAM/government alerts
    /// </summary>
    public bool GovernmentAlert { get; set; }

    /// <summary>
    /// Users who were notified about this alert
    /// </summary>
    public List<NotifiedUser> NotifiedUsers { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public void SetId(string id) => Id = id;
}
