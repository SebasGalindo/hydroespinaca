using HydroEspinaca.Shared.Enums;

namespace ActuatorService.Domain.Models;

/// <summary>
/// Represents the current runtime state of an actuator.
/// This is kept in-memory and not persisted to the database.
/// </summary>
public class ActuatorState
{
    public string ActuatorId { get; set; } = default!;
    public string Esp32Id { get; set; } = default!;
    public string Pin { get; set; } = default!;
    public ActuatorMode Mode { get; set; }
    public PowerState State { get; set; } = PowerState.OFF;

    /// <summary>
    /// Remaining duration in seconds for time-limited commands.
    /// Null when command has no time constraint or is permanent (OFF/ON without duration).
    /// Used to track temporary actuator activations and scheduled tasks.
    /// </summary>
    public double? RemainingDuration { get; set; }

    /// <summary>
    /// PWM duty cycle percentage (0-100) for actuators in PWM mode.
    /// Null for digital mode actuators.
    /// </summary>
    public double? DutyCycle { get; set; }

    public string? LastCommandId { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
