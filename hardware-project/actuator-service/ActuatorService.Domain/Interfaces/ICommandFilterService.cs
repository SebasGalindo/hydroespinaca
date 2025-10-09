using HydroEspinaca.Shared.DTOs.Actuator;

namespace ActuatorService.Domain.Interfaces;

/// <summary>
/// Service for intelligent filtering of redundant actuator commands.
/// Prevents unnecessary command noise from FuzzyService.
/// </summary>
public interface ICommandFilterService
{
    /// <summary>
    /// Filters a list of routine commands, removing redundant ones.
    /// </summary>
    /// <param name="routines">Incoming routine commands from FuzzyService</param>
    /// <returns>Filtered list of routines that should be processed</returns>
    Task<List<RoutineCommandDto>> FilterCommandsAsync(List<RoutineCommandDto> routines);
}
