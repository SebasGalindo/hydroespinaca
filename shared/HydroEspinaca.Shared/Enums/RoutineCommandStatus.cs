namespace HydroEspinaca.Shared.Enums;

/// <summary>
/// Represents the possible states of a routine command execution
/// </summary>
public enum RoutineCommandStatus
{
    /// <summary>
    /// Command is currently being executed (actuator is active)
    /// </summary>
    RUNNING,

    /// <summary>
    /// Command has finished execution (confirmed by firmware via MQTT)
    /// </summary>
    FINISHED
}