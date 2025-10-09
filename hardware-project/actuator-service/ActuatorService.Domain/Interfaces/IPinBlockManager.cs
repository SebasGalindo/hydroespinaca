namespace ActuatorService.Domain.Interfaces;

/// <summary>
/// Manages temporary blocks (cooldowns) on actuators
/// </summary>
public interface IPinBlockManager
{
    /// <summary>
    /// Checks if an actuator is currently blocked
    /// </summary>
    Task<bool> IsBlockedAsync(string actuatorId);

    /// <summary>
    /// Gets the cooldown information for an actuator (if any)
    /// </summary>
    Task<ActuatorCooldownInfo?> GetCooldownInfoAsync(string actuatorId);

    /// <summary>
    /// Blocks an actuator for a specific duration
    /// </summary>
    Task BlockForAsync(string actuatorId, TimeSpan duration, string reason);

    /// <summary>
    /// Removes expired cooldowns
    /// </summary>
    Task CleanupExpiredCooldownsAsync();

    /// <summary>
    /// Gets all active cooldowns
    /// </summary>
    Task<List<ActuatorCooldownInfo>> GetAllActiveCooldownsAsync();
}

public record ActuatorCooldownInfo
{
    public string ActuatorId { get; init; } = default!;
    public string Reason { get; init; } = default!;
    public DateTime StartedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public TimeSpan RemainingTime { get; init; }
}
