using HydroEspinaca.Shared.Abstractions;

namespace ActuatorService.Domain.Entities;

/// <summary>
/// Represents a temporary block (cooldown) on an actuator
/// </summary>
public class ActuatorCooldown : IIdentifiableMutable
{
    public string Id { get; set; } = default!;

    /// <summary>
    /// Actuator ID that is blocked
    /// </summary>
    public string ActuatorId { get; set; } = default!;

    /// <summary>
    /// Reason for the cooldown
    /// </summary>
    public string Reason { get; set; } = default!;

    /// <summary>
    /// When the cooldown started (UTC)
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the cooldown expires (UTC)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether this cooldown is still active
    /// </summary>
    public bool IsActive { get; set; } = true;

    public void SetId(string id) => Id = id;
}
