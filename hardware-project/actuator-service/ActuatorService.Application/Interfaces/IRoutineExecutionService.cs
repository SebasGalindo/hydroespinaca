using ActuatorService.Application.DTOs;
using HydroEspinaca.Shared.DTOs.Actuator;

namespace ActuatorService.Application.Interfaces;

/// <summary>
/// Manages the execution lifecycle of routines with pin-based locking.
/// Handles concurrent execution with max 1 running + 1 pending per pin.
/// </summary>
public interface IRoutineExecutionService
{
    /// <summary>
    /// Schedules routines for execution, activating those with available pins.
    /// </summary>
    Task<List<string>> ScheduleRoutinesAsync(List<ResolvedRoutineDto> resolvedRoutines, string esp32Id);

    /// <summary>
    /// Marks a routine as completed, releases its pins, and activates pending routines.
    /// </summary>
    Task OnRoutineCompletedAsync(string commandId);

    /// <summary>
    /// Gets the current status of all routines (active + pending).
    /// </summary>
    Task<JobStatusDto> GetStatusAsync(string? esp32Id = null);

    /// <summary>
    /// Gets statistics about active and pending routines.
    /// </summary>
    Task<RoutineExecutionStats> GetStatsAsync();

    /// <summary>
    /// Clears all routines for a specific ESP32 or all ESP32s.
    /// </summary>
    Task ClearAsync(string? esp32Id = null);

    /// <summary>
    /// Resets all actuators to OFF state, clears cache, and publishes reset commands to firmware.
    /// </summary>
    Task ResetAllActuatorsAsync(string? esp32Id = null);
}

public record RoutineExecutionStats
{
    public int ActiveCount { get; init; }
    public int PendingCount { get; init; }
    public int TotalLockedPins { get; init; }
    public List<string> Esp32Ids { get; init; } = new();
}
