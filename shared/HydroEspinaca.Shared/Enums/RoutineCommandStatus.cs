namespace HydroEspinaca.Shared.Enums;

/// <summary>
/// Represents the possible states of a routine command execution
/// </summary>
public enum RoutineCommandStatus
{
    /// <summary>
    /// Command has been created and scheduled for execution
    /// </summary>
    SCHEDULED,
    
    /// <summary>
    /// Command is currently being executed
    /// </summary>
    IN_PROGRESS,
    
    /// <summary>
    /// Command has been successfully completed
    /// </summary>
    COMPLETED,
    
    /// <summary>
    /// Command execution failed
    /// </summary>
    FAILED,
    
    /// <summary>
    /// Command was cancelled before completion
    /// </summary>
    CANCELLED
}