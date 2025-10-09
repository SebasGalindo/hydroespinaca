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
    public double? RemainingDuration { get; set; }
    public double? DutyCycle { get; set; }
    public string? LastCommandId { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
