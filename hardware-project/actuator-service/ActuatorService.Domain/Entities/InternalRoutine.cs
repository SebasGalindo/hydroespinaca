using HydroEspinaca.Shared.Abstractions;

namespace ActuatorService.Domain.Entities;

/// <summary>
/// Represents a system-internal recurring routine that executes at fixed intervals.
/// These routines are independent of the fuzzy-service and are scheduled based on time.
/// Execution is deterministic, calculated from 00:00 local time without persisted state.
/// </summary>
public class InternalRoutine : IIdentifiableMutable
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;

    /// <summary>
    /// ESP32 device ID where this routine should execute
    /// </summary>
    public string Esp32Id { get; set; } = default!;

    /// <summary>
    /// Interval between executions (e.g., 02:00:00 for 2 hours, 00:30:00 for 30 minutes)
    /// Execution times are calculated from 00:00 local time (America/Bogota)
    /// </summary>
    public TimeSpan Interval { get; set; }

    /// <summary>
    /// Steps to execute in this routine (using outputVariable like fuzzy routines)
    /// </summary>
    public List<InternalRoutineStep> Steps { get; set; } = new();

    /// <summary>
    /// Whether this routine is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public void SetId(string id) => Id = id;
}

/// <summary>
/// Step in an internal routine
/// </summary>
public class InternalRoutineStep
{
    /// <summary>
    /// Actuator code (e.g., "BombaRiego", "Ventiladores")
    /// Previously was OutputVariable ID from control_outputs collection
    /// Now directly references Actuator.Code
    /// </summary>
    public string OutputVariable { get; set; } = default!;

    /// <summary>
    /// Power state (ON/OFF) - Used for DIGITAL mode. Null for PWM mode.
    /// </summary>
    public string? Power { get; set; }

    /// <summary>
    /// Duration in seconds
    /// </summary>
    public double Duration { get; set; }

    /// <summary>
    /// Duty cycle for PWM mode (0-100%)
    /// </summary>
    public double? DutyCycle { get; set; }

    /// <summary>
    /// Actuator mode (DIGITAL, PWM, etc.)
    /// </summary>
    public string? Mode { get; set; }
}
