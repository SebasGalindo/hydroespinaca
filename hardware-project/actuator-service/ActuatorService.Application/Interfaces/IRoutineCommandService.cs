using ActuatorService.Domain.Entities;

namespace ActuatorService.Application.Interfaces;

public interface IRoutineCommandService
{
    Task<List<RoutineCommand>> GetAllRoutineCommandsAsync(string? esp32Id = null);
    Task<RoutineCommand?> GetRoutineCommandByIdAsync(string commandId);
}