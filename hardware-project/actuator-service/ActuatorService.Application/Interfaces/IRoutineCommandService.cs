using ActuatorService.Domain.Entities;

namespace ActuatorService.Application.Interfaces;

/// <summary>
/// Contract for the routine command service handling execution, scheduling, and completion tracking.
/// </summary>
public interface IRoutineCommandService
{
    Task<List<RoutineCommand>> GetAllRoutineCommandsAsync(string? esp32Id = null);
    Task<RoutineCommand?> GetRoutineCommandByIdAsync(string commandId);
}