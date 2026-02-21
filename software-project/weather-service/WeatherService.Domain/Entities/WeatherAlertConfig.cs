using HydroEspinaca.Shared.Abstractions;

namespace WeatherService.Domain.Entities;

/// <summary>
/// Weather alert thresholds configuration per fuzzy system.
/// Each active fuzzy system has its own set of thresholds.
/// </summary>
public class WeatherAlertConfig : IIdentifiableMutable
{
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Fuzzy system ID this config belongs to (unique index)
    /// </summary>
    public required string FuzzySystemId { get; set; }

    /// <summary>
    /// Denormalized name for display
    /// </summary>
    public string FuzzySystemName { get; set; } = string.Empty;

    /// <summary>
    /// Alert threshold definitions
    /// </summary>
    public List<AlertThreshold> Alerts { get; set; } = [];

    /// <summary>
    /// Master switch to disable all alerts for this system
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Audit fields (not automatically set, must be managed by application logic)
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// User ID of the last updater (for audit purposes)
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Timestamps for creation and last update (managed by application logic)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public void SetId(string id) => Id = id;

    public void UpdateTimestamp(string userId)
    {
        UpdatedBy = userId;
        UpdatedAt = DateTime.UtcNow;
    }
}
