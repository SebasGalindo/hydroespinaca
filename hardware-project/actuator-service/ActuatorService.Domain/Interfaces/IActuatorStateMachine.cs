using ActuatorService.Domain.Models;
using HydroEspinaca.Shared.Enums;

namespace ActuatorService.Domain.Interfaces;

/// <summary>
/// Manages the runtime state of all actuators in memory.
/// This is the single source of truth for actuator states during execution.
/// </summary>
public interface IActuatorStateMachine
{
    /// <summary>
    /// Gets the current state of an actuator.
    /// </summary>
    ActuatorState? GetState(string actuatorId);

    /// <summary>
    /// Gets all actuator states.
    /// </summary>
    IReadOnlyDictionary<string, ActuatorState> GetAllStates();

    /// <summary>
    /// Updates the state of an actuator.
    /// </summary>
    void UpdateState(string actuatorId, PowerState newState, double? duration = null, double? dutyCycle = null, string? commandId = null);

    /// <summary>
    /// Initializes or updates state for an actuator based on its configuration.
    /// </summary>
    void InitializeActuator(string actuatorId, string esp32Id, string pin, ActuatorMode mode);

    /// <summary>
    /// Clears all actuator states (used during startup).
    /// </summary>
    void ResetAll();
}
